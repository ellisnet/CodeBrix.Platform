#if !__NETSTD_REFERENCE__
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;

#nullable enable

namespace Microsoft.UI.Composition
{
	/// <summary>
	/// The composition visual of a <see cref="TextBlock"/>. Its text is painted by the platform state that the platform
	/// registers for this visual type (on Skia, <c>CodeBrix.Platform.UI.Skia.TextVisualSkiaPlatform</c>).
	/// </summary>
	internal class TextVisual : ContainerVisual
	{
		private readonly WeakReference<TextBlock> _owner;

		public TextVisual(Compositor compositor, TextBlock owner) : base(compositor)
		{
			_owner = new WeakReference<TextBlock>(owner);
		}

		/// <summary>
		/// Gets the text block this visual belongs to, unless it was collected.
		/// </summary>
		/// <param name="owner">The text block.</param>
		/// <returns><see langword="true"/> when the text block is still alive.</returns>
		internal bool TryGetOwner([NotNullWhen(true)] out TextBlock? owner) => _owner.TryGetTarget(out owner);
	}
}
#endif
