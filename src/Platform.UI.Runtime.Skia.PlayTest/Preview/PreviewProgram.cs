using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest.Hosting;
using SDL;
using static SDL.SDL3;

namespace CodeBrix.Platform.PlayTest.Preview;

internal static class PreviewProgram
{
    private static VirtualFrame? _latest;
    private static volatile bool _finished;

    private static async Task ReadFramesAsync()
    {
        try
        {
            using var input = Console.OpenStandardInput();
            while (await PreviewFrames.ReadAsync(input).ConfigureAwait(false) is { } frame)
                Interlocked.Exchange(ref _latest, frame);
        }
        finally { _finished = true; }
    }

    [STAThread]
    private static unsafe int Main(string[] args)
    {
        if (args.Length != 3 || args[0] != "--preview" || !int.TryParse(args[1], out var width) ||
            !int.TryParse(args[2], out var height) || !((width == 1920 && height == 1080) || (width == 1080 && height == 1920)))
        {
            Console.Error.WriteLine("This executable is the view-only PlayTest preview. Run the consuming test project with dotnet test.");
            return 2;
        }
        SDL_Window* window = null;
        SDL_Renderer* renderer = null;
        SDL_Texture* texture = null;
        try
        {
            if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO)) throw new InvalidOperationException(SDL_GetError());
            window = SDL_CreateWindow("CodeBrix PlayTest - view only", width / 2, height / 2, SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            if (window == null) throw new InvalidOperationException(SDL_GetError());
            // These proportions are the fixture preference and remain unchanged for the entire run.
            // Individual test orientations affect only the frame and its letterboxed presentation.
            // Aspect constraints are advisory on some desktops; letterboxing remains correct if denied.
            SDL_SetWindowAspectRatio(window, (float)width / height, (float)width / height);
            renderer = SDL_CreateRenderer(window, (byte*)null);
            if (renderer == null) throw new InvalidOperationException(SDL_GetError());
            if (!SDL_SetRenderLogicalPresentation(renderer, width, height, SDL_RendererLogicalPresentation.SDL_LOGICAL_PRESENTATION_LETTERBOX))
                throw new InvalidOperationException(SDL_GetError());
            texture = SDL_CreateTexture(renderer, SDL_PIXELFORMAT_BGRA32, SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);
            if (texture == null) throw new InvalidOperationException(SDL_GetError());
            SDL_SetTextureBlendMode(texture, SDL_BlendMode.SDL_BLENDMODE_NONE);
            var reader = Task.Run(ReadFramesAsync);
            Console.WriteLine("READY");
            Console.Out.Flush();
            var hasFrame = false;
            while (!_finished)
            {
                SDL_Event e;
                while (SDL_PollEvent(&e))
                {
                    if (e.type == (uint)SDL_EventType.SDL_EVENT_QUIT || e.type == (uint)SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED) return 0;
                    // All physical keyboard, pointer, wheel and touch events are discarded.
                }
                var frame = Interlocked.Exchange(ref _latest, null);
                if (frame != null)
                {
                    if (frame.Width != width || frame.Height != height)
                    {
                        SDL_DestroyTexture(texture);
                        texture = null;
                        width = frame.Width;
                        height = frame.Height;
                        if (!SDL_SetRenderLogicalPresentation(renderer, width, height, SDL_RendererLogicalPresentation.SDL_LOGICAL_PRESENTATION_LETTERBOX))
                            throw new InvalidOperationException(SDL_GetError());
                        texture = SDL_CreateTexture(renderer, SDL_PIXELFORMAT_BGRA32, SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);
                        if (texture == null) throw new InvalidOperationException(SDL_GetError());
                        SDL_SetTextureBlendMode(texture, SDL_BlendMode.SDL_BLENDMODE_NONE);
                    }
                    fixed (byte* p = frame.Pixels)
                        if (!SDL_UpdateTexture(texture, null, (IntPtr)p, width * 4)) throw new InvalidOperationException(SDL_GetError());
                    hasFrame = true;
                }
                SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
                SDL_RenderClear(renderer);
                if (hasFrame) SDL_RenderTexture(renderer, texture, null, null);
                SDL_RenderPresent(renderer);
                Thread.Sleep(16);
            }
            reader.GetAwaiter().GetResult(); // A truncated or malformed frame must fail the headed run.
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        finally
        {
            if (texture != null) SDL_DestroyTexture(texture);
            if (renderer != null) SDL_DestroyRenderer(renderer);
            if (window != null) SDL_DestroyWindow(window);
            SDL_Quit();
        }
    }
}
