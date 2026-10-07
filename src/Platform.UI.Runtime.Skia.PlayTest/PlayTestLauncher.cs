using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeBrix.Platform.Extensions.System;
using CodeBrix.Platform.Foundation.Extensibility;
using Windows.System;

namespace CodeBrix.Platform.PlayTest;

/// <summary>The head's launcher: it records every <c>Windows.System.Launcher</c> request the application
/// makes (a link click, an "open file" or "open folder" through a <c>file:</c> URI) and never starts a
/// browser, viewer or other process. Obtain it from <see cref="PlayTestApplication.Launcher"/>.</summary>
public sealed class PlayTestLauncher
{
    private readonly object _lock = new();
    private readonly List<Uri> _launched = new();
    private readonly List<Uri> _queried = new();
    private bool _launchResult = true;
    private LaunchQuerySupportStatus _queryResult = LaunchQuerySupportStatus.Available;

    /// <summary>The URIs passed to <c>Launcher.LaunchUriAsync</c> since the last <see cref="Clear"/>, in order.</summary>
    public IReadOnlyList<Uri> LaunchedUris { get { lock (_lock) return _launched.ToArray(); } }

    /// <summary>The URIs passed to <c>Launcher.QueryUriSupportAsync</c> since the last <see cref="Clear"/>, in order.</summary>
    public IReadOnlyList<Uri> QueriedUris { get { lock (_lock) return _queried.ToArray(); } }

    /// <summary>What <c>Launcher.LaunchUriAsync</c> returns; true (launched) by default. Set false to reach
    /// the application's "could not open" branch.</summary>
    public bool LaunchResult { get { lock (_lock) return _launchResult; } set { lock (_lock) _launchResult = value; } }

    /// <summary>What <c>Launcher.QueryUriSupportAsync</c> returns; <c>Available</c> by default.</summary>
    public LaunchQuerySupportStatus QueryUriSupportResult { get { lock (_lock) return _queryResult; } set { lock (_lock) _queryResult = value; } }

    /// <summary>Clears the request history and restores the default results, between serialized tests.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _launched.Clear();
            _queried.Clear();
            _launchResult = true;
            _queryResult = LaunchQuerySupportStatus.Available;
        }
    }

    internal void Register() => ApiExtensibility.Register(typeof(ILauncherExtension), _ => new Extension(this));

    internal bool RecordLaunch(Uri uri)
    {
        lock (_lock)
        {
            _launched.Add(uri);
            return _launchResult;
        }
    }

    internal LaunchQuerySupportStatus RecordQuery(Uri uri)
    {
        lock (_lock)
        {
            _queried.Add(uri);
            return _queryResult;
        }
    }

    // Created by the framework's Launcher through ApiExtensibility; never starts a process.
    internal sealed class Extension(PlayTestLauncher owner) : ILauncherExtension
    {
        public Task<bool> LaunchUriAsync(Uri uri) => Task.FromResult(owner.RecordLaunch(uri));

        public Task<LaunchQuerySupportStatus> QueryUriSupportAsync(Uri uri, LaunchQuerySupportType launchQuerySupportType) =>
            Task.FromResult(owner.RecordQuery(uri));
    }
}
