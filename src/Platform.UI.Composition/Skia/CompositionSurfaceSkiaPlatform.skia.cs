#nullable enable

using SkiaSharp;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Composition.Contracts;
using CodeBrix.Platform.UI.Dispatching;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="ICompositionSurfacePlatform"/>: the image of a
/// <see cref="PlatformCompositionSurface"/> is an <see cref="SKImage"/> supplied by a frame provider (one frame, or the
/// frames of an animated image that a timer advances).
/// </summary>
/// <remarks>This is the image code that lived in <c>PlatformCompositionSurface.skia.cs</c>, moved verbatim.</remarks>
internal sealed class CompositionSurfaceSkiaPlatform : ICompositionSurfacePlatform
{
	private readonly PlatformCompositionSurface _owner;

	// Don't set this field directly. Use SetFrameProviderAndOnFrameChanged instead.
	private IFrameProvider? _frameProvider;

	// Unused: But intentionally kept!
	// This is here to keep the Action lifetime the same as PlatformCompositionSurface.
	// i.e, only cause the Action to be GC'ed if PlatformCompositionSurface is GC'ed.
	private Action? _onFrameChanged;

	/// <summary>
	/// Creates the image store of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The surface this object backs.</param>
	internal CompositionSurfaceSkiaPlatform(PlatformCompositionSurface owner)
	{
		_owner = owner;
	}

	/// <summary>
	/// Returns the Skia image store of <paramref name="surface"/> (creating it on first use).
	/// </summary>
	/// <param name="surface">The surface.</param>
	/// <returns>The surface's image store.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CompositionSurfaceSkiaPlatform Of(PlatformCompositionSurface surface)
	{
		var platform = surface.Platform;
		Debug.Assert(platform is CompositionSurfaceSkiaPlatform);
		return Unsafe.As<CompositionSurfaceSkiaPlatform>(platform);
	}

	// Don't set directly. Use SetFrameProviderAndOnFrameChanged instead
	private IFrameProvider? FrameProvider
	{
		get => _frameProvider;
		set
		{
			_frameProvider?.Dispose();
			_frameProvider = value;
			_owner.RaiseSurfacePropertyChanged(nameof(FrameProvider));
		}
	}

	/// <summary>
	/// Gets the current image of the surface.
	/// </summary>
	internal SKImage? Image => FrameProvider?.CurrentImage;

	/// <summary>
	/// Replaces the surface's image with <paramref name="image"/>.
	/// </summary>
	/// <param name="image">The image.</param>
	internal void SetImage(SKImage image)
	{
		FrameProvider = FrameProviderFactory.Create(image);
	}

	private void SetFrameProviderAndOnFrameChanged(IFrameProvider? provider, Action? onFrameChanged)
	{
		FrameProvider = provider;
		_onFrameChanged = onFrameChanged;
	}

	/// <inheritdoc />
	public (bool success, object nativeResult) LoadFromStream(int? targetWidth, int? targetHeight, Stream imageStream)
	{
		using var stream = new SKManagedStream(imageStream);

		if (targetWidth is int actualTargetWidth && targetHeight is int actualTargetHeight)
		{
			using var codec = SKCodec.Create(stream);

			var bitmap = new SKBitmap(actualTargetWidth, actualTargetHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

			var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());

			if (_owner.Log().IsEnabled(LogLevel.Debug))
			{
				_owner.Log().Debug($"Image load result {result}");
			}

			if (result == SKCodecResult.Success)
			{
				SetFrameProviderAndOnFrameChanged(FrameProviderFactory.Create(SKImage.FromBitmap(bitmap)), null);
			}

			return (result == SKCodecResult.Success || result == SKCodecResult.IncompleteInput, result);
		}
		else
		{
			try
			{
				var onFrameChanged = () => NativeDispatcher.Main.Enqueue(() => _owner.RaiseSurfacePropertyChanged(nameof(Image)), NativeDispatcherPriority.High);
				if (!FrameProviderFactory.TryCreate(stream, onFrameChanged, out var provider))
				{
					SetFrameProviderAndOnFrameChanged(null, null);
					return (false, "Failed to decode image");
				}

				SetFrameProviderAndOnFrameChanged(provider, onFrameChanged);
				GC.KeepAlive(onFrameChanged);
				return (true, "Success");
			}
			catch (Exception e)
			{
				SetFrameProviderAndOnFrameChanged(null, null);
				return (false, e.Message);
			}
		}
	}

	/// <inheritdoc />
	public unsafe void CopyPixels(int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data)
	{
		var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

		using (var pData = data.Pin())
		{
			SetFrameProviderAndOnFrameChanged(FrameProviderFactory.Create(SKImage.FromPixelCopy(info, (IntPtr)pData.Pointer, pixelWidth * 4)), null);
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		SetFrameProviderAndOnFrameChanged(null, null);
	}
}
