using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.DataBinding;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using CodeBrix.Platform.Extensions.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ScrollContentPresenter : ContentPresenter
{
	public bool CanHorizontallyScroll { get; set; }

	public bool CanVerticallyScroll { get; set; }

	public double ExtentHeight { get; internal set; }

	public double ExtentWidth { get; internal set; }

	internal ScrollBarVisibility VerticalScrollBarVisibility { get; set; }

	internal ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }

	private object RealContent { get; set; }

	protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);

	protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
}
