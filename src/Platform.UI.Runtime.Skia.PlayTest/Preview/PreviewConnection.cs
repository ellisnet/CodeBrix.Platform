using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest.Hosting;

namespace CodeBrix.Platform.PlayTest.Preview;

internal sealed class PreviewConnection : IAsyncDisposable
{
    private readonly Process _process;
    private readonly Task<string> _errors;
    private readonly Channel<VirtualFrame> _frames = Channel.CreateBounded<VirtualFrame>(new BoundedChannelOptions(1)
    { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = true });
    private Task _writer;

    private PreviewConnection(Process process)
    {
        _process = process;
        _errors = process.StandardError.ReadToEndAsync();
    }

    internal static async Task<PreviewConnection> StartAsync(int width, int height)
    {
        // Use the consumer's deps/runtimeconfig so native NuGet assets resolve on all
        // RIDs. The separate process gives SDL the main thread, including on macOS.
        var entry = Assembly.GetEntryAssembly()?.Location;
        var candidates = new[] { string.IsNullOrEmpty(entry) ? "" : Path.ChangeExtension(entry, ".deps.json") }
            .Concat(((string)AppContext.GetData("APP_CONTEXT_DEPS_FILES") ?? "")
                .Split(new[] { ';', Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries));
        var deps = candidates.FirstOrDefault(p => p.EndsWith(".deps.json", StringComparison.Ordinal) && File.Exists(p)
            && File.Exists(p[..^".deps.json".Length] + ".runtimeconfig.json"));
        if (deps == null) throw new PlayTestException("Headed PlayTest requires an executable test project with a deps.json and runtimeconfig.json.");
        var runtimeconfig = deps[..^".deps.json".Length] + ".runtimeconfig.json";
        var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true,
        };
        foreach (var argument in new[] { "exec", "--depsfile", deps, "--runtimeconfig", runtimeconfig,
            typeof(PreviewConnection).Assembly.Location, "--preview", width.ToString(System.Globalization.CultureInfo.InvariantCulture),
            height.ToString(System.Globalization.CultureInfo.InvariantCulture) }) info.ArgumentList.Add(argument);
        var result = new PreviewConnection(Process.Start(info));
        try
        {
            var ready = await result._process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            if (ready != "READY") throw new PlayTestException("Could not start PlayTest preview: " + await result._errors.ConfigureAwait(false));
            result._writer = Task.Run(result.WriteAsync);
            return result;
        }
        catch { await result.DisposeAsync().ConfigureAwait(false); throw; }
    }

    internal void Present(VirtualFrame frame)
    {
        if (!_process.HasExited) _frames.Writer.TryWrite(frame);
        else if (_process.ExitCode != 0) throw new PlayTestException($"PlayTest preview exited with code {_process.ExitCode}.");
    }

    private async Task WriteAsync()
    {
        try
        {
            await foreach (var frame in _frames.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await PreviewFrames.WriteAsync(_process.StandardInput.BaseStream, frame).ConfigureAwait(false);
            }
        }
        catch (IOException) when (_process.HasExited) { }
    }

    public async ValueTask DisposeAsync()
    {
        _frames.Writer.TryComplete();
        if (_writer != null)
        {
            try { await _writer.WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false); }
            catch (TimeoutException) { }
            catch (IOException) { }
        }
        _process.StandardInput.Close();
        var killed = false;
        if (!_process.HasExited)
        {
            try { await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false); }
            catch (TimeoutException) { killed = true; _process.Kill(entireProcessTree: true); await _process.WaitForExitAsync().ConfigureAwait(false); }
        }
        var errors = await _errors.ConfigureAwait(false);
        var exitCode = _process.ExitCode;
        _process.Dispose();
        if (!killed && exitCode != 0) throw new PlayTestException($"PlayTest preview exited with code {exitCode}: {errors}");
    }
}
