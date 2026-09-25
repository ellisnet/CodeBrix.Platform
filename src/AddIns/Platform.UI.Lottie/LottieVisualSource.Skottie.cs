#nullable enable

#if HAS_SKOTTIE

using System;
using System.Threading;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using CodeBrix.Platform.Extensions.Disposables;
using SkiaSharp;
using CodeBrix.Platform.Foundation.Logging;
using Microsoft.UI.Xaml;
using Windows.System;
using System.Diagnostics;
using SkiaSharp.SceneGraph;
using Microsoft.UI.Xaml.Media;
using System.Text;
using System.IO;
using CodeBrix.Platform.UI.Lottie.Contracts;
using CodeBrix.Platform.UI.Lottie.Engine;
using CodeBrix.Platform.UI.Lottie.Internal;

#if HAS_CODEBRIX_WINUI
using SkiaSharp.Views.Windows;
using Windows.UI.Core;
#else
using SkiaSharp.Views.UWP;
using Microsoft.UI.Xaml.Controls;
#endif

#if HAS_CODEBRIX_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	//was previously: the render surface was chosen per platform at compile time - a Graphics2DSK SKCanvasElement
	//subclass on Skia (__SKIA__), an SKSwapChainPanel (USE_HARDWARE_ACCELERATION, defined when not __SKIA__) or an
	//SKXamlCanvas elsewhere. Since the Core/Skia split the platform supplies it through ILottieCanvasPlatform (on Skia:
	//Skia/LottieCanvasSkiaPlatform.skia.cs, the same SKCanvasElement subclass), and the frame is drawn here through the
	//SKCanvas-typed callback of the Skia-canvas host seam (SKCanvasHost.ToPlatformCallback).
	//WPE1 C9: the play state, the frame clock, the frame timer and the rendering moved into the WinUI-free engine
	//(Engine/LottiePlayer, driven by an Engine/ITickSource - here Internal/DispatcherQueueTickSource); this source
	//wraps it and keeps the XAML half (the player, its dependency properties, the render surface, the JSON loading).
	partial class LottieVisualSourceBase
	{
		//The platform's canvas supply, resolved once for the process.
		private static ILottieCanvasPlatform? _canvasPlatform;

		private static ILottieCanvasPlatform CanvasPlatform => _canvasPlatform ??= PlatformContract.Resolve<ILottieCanvasPlatform>();

		private UIElement? _renderSurface;
		private Action<object, Size>? _renderCallback;

		private bool _wasPlaying;

		private Uri? _lastSource;

		//The engine (created on first use: a source can exist before any player uses it).
		private LottiePlayer? _engine;

		private LottiePlayer Engine => _engine ??= CreateEngine();

		private SkiaSharp.Skottie.Animation? CurrentAnimation => _engine?.Animation;

		private readonly SerialDisposable _animationDataSubscription = new SerialDisposable();

		private LottiePlayer CreateEngine()
		{
			var engine = new LottiePlayer(
				DispatcherQueueTickSource.ForCurrentThread,
				action =>
				{
					if (Dispatcher.HasThreadAccess)
					{
						action();
					}
					else
					{
						_ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => action());
					}
				});
			engine.InvalidateRequested += Invalidate;
			engine.IsPlayingChanged += SetIsPlaying;
			return engine;
		}

		async Task InnerUpdate(CancellationToken ct)
		{
			var player = _player;

			if (player == null)
			{
				return;
			}

			await SetProperties();

			async Task SetProperties()
			{
				try
				{
					var sourceUri = UriSource;
					if (_lastSource == null || !_lastSource.Equals(sourceUri))
					{
						_lastSource = sourceUri;
						if ((await TryLoadDownloadJson(sourceUri, ct)) is { } jsonStream)
						{
							var cacheKey = sourceUri.OriginalString;
							_animationDataSubscription.Disposable = null;
							_animationDataSubscription.Disposable =
								LoadAndObserveAnimationData(jsonStream, cacheKey, OnJsonChanged);

							void OnJsonChanged(string updatedJson, string updatedCacheKey)
							{
								try
								{
									//Decoded (and sought to its start) by the engine; throws when Skottie cannot load it
									var animation = LottiePlayer.CreateAnimation(updatedJson);

									if (this.Log().IsEnabled(LogLevel.Debug))
									{
										this.Log().Debug($"Version: {animation.Version} Duration: {animation.Duration} Fps:{animation.Fps} InPoint: {animation.InPoint} OutPoint: {animation.OutPoint}");
									}

									SetAnimation(animation);

									if (Engine.PlayState is { } playState)
									{
										var (fromProgress, toProgress, looped) = playState;
										Play(fromProgress, toProgress, looped);
									}
								}
								catch (Exception ex)
								{
									throw new InvalidOperationException("Failed load the animation", ex);
								}
							}
						}
						else
						{
							throw new NotSupportedException($"Failed to load animation: {sourceUri}");
						}

						// Force layout to recalculate
						player.InvalidateMeasure();
						player.InvalidateArrange();

						if (Engine.PlayState is { } playState)
						{
							var (fromProgress, toProgress, looped) = playState;
							Play(fromProgress, toProgress, looped);
						}
						else if (player.AutoPlay)
						{
							Play(0, 1, true);
						}

					}

					if (CurrentAnimation == null)
					{
						return;
					}

					PublishAnimationState();

					Invalidate();
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Failed to update lottie player for [{UriSource}]", e);
					}
				}
			}
		}

		private void SetAnimation(SkiaSharp.Skottie.Animation animation)
		{
			if (!ReferenceEquals(CurrentAnimation, animation))
			{
				_player?.RemoveChild(_renderSurface);
			}

			_renderSurface = BuildRenderSurface();

			_player?.AddChild(_renderSurface);

			Engine.Animation = animation;

			// The player learns that it has something to play HERE, where the decoded animation
			// actually arrives - not at the end of the update pass that asked for it. That pass
			// may have run to its end long before: a plain source reads its JSON asynchronously,
			// so the animation is handed over from a continuation, and the update pass had no
			// animation to report when it looked. Nothing runs a second pass unless something
			// else changes a property of the player, which is why the state used to appear only
			// when the animation happened to start playing by itself.
			PublishAnimationState();
		}

		/// <summary>
		/// Publishes the decoded animation's duration, and the fact that there IS one, on the
		/// player. Both are read-only dependency properties an application binds to; setting the
		/// same values again is a no-op, so this may be called from every path that could be the
		/// first to hold a decoded animation.
		/// </summary>
		private void PublishAnimationState()
		{
			if (_player is not { } player || CurrentAnimation is not { } animation)
			{
				return;
			}

			var duration = animation.Duration;
			player.SetValue(AnimatedVisualPlayer.DurationProperty, duration);
			player.SetValue(AnimatedVisualPlayer.IsAnimatedVisualLoadedProperty, duration > TimeSpan.Zero);
		}

		private UIElement BuildRenderSurface()
		{
			ClearRenderSurface();

			return CanvasPlatform.CreateRenderSurface(_renderCallback ??= SKCanvasHost.ToPlatformCallback(OnRenderOverride));
		}

		private void ClearRenderSurface()
		{
			//Nothing to release: the platform's surface holds no subscription of this source (the render callback is
			//owned by the surface, which the player drops in SetAnimation).
		}

		private void OnSoftwareCanvas_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
		{
			Render(e.Surface.Canvas, e.Surface.Canvas.LocalClipBounds.Size, saveRestoreAndCleanCanvas: true);
		}

		private void OnHardwareCanvas_PaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
		{
			Render(e.Surface.Canvas, e.Surface.Canvas.LocalClipBounds.Size, saveRestoreAndCleanCanvas: true);
		}

		private void OnRenderOverride(SKCanvas canvas, Size area)
		{
			Render(canvas, area.ToSKSize(), saveRestoreAndCleanCanvas: false);
		}

		private void Render(SKCanvas canvas, SKSize localSize, bool saveRestoreAndCleanCanvas)
		{
			if (_player is not { } player || _engine is not { } engine)
			{
				return;
			}

			//The engine renders the frame; the player supplies the stretch, the speed and (for a surface of its own)
			//the background it clears to.
			engine.Render(
				canvas,
				localSize,
				(LottieStretch)(int)player.Stretch,
				player.PlaybackRate,
				saveRestoreAndCleanCanvas ? GetBackgroundColor() : null);
		}

		private SKColor GetBackgroundColor()
		{
			if (_player?.Background is SolidColorBrush sb)
			{
				return new SKColor(alpha: sb.ColorWithOpacity.A, red: sb.ColorWithOpacity.R, green: sb.ColorWithOpacity.G, blue: sb.ColorWithOpacity.B);
			}

			return SKColors.Transparent;
		}

		public void Play(double fromProgress, double toProgress, bool looped) => Engine.Play(fromProgress, toProgress, looped);

		private void Invalidate()
		{
			if (_renderSurface is { } renderSurface)
			{
				CanvasPlatform.Invalidate(renderSurface);
			}
		}

		public void Stop() => Engine.Stop();

		public void Pause() => Engine.Pause();

		public void Resume() => Engine.Resume();

		public void SetProgress(double progress) => Engine.SetProgress(progress);

		public void Load()
		{
			if (_wasPlaying)
			{
				_wasPlaying = false;
				Resume();
			}
		}

		public void Unload()
		{
			if (_player?.IsPlaying ?? false)
			{
				_wasPlaying = true;
				Pause();
			}
		}

		private Size CompositionSize
			=> CurrentAnimation?.Size is { } size
				? new Size(size.Width, size.Height)
				: default;
	}
}
#endif
