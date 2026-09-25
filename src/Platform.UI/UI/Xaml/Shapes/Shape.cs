using System;
using System.Collections.Generic;
using CodeBrix.Platform.Extensions.Disposables;
using System.Text;
using Windows.Foundation;
using CodeBrix.Platform.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform;
using CodeBrix.Platform.UI.Helpers;

namespace Microsoft.UI.Xaml.Shapes
{
	public abstract partial class Shape : FrameworkElement
	{
		private const double DefaultStrokeThicknessWhenNoStrokeDefined = 0.0;

#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
		private Action _brushChanged;
		private Action _strokeBrushChanged;
		private IDisposable _brushChangedSubscription;
		private IDisposable _strokeBrushChangedSubscription;
#endif

		/// <summary>
		/// Returns 0.0 if Stroke is <c>null</c>, otherwise, StrokeThickness
		/// </summary>
		/// <remarks>Path does not need to define a stroke, in that case StrokeThickness should just return 0.
		/// Other shapes like Ellipse and Polygon will not draw if Stroke is null so returning 0 will have no effect
		///</remarks>
		private protected double ActualStrokeThickness => Stroke == null
			? DefaultStrokeThicknessWhenNoStrokeDefined
			: LayoutRound(StrokeThickness);

		#region Fill Dependency Property
		//This field is never accessed. It just exists to create a reference, because the DP causes issues with ImageBrush of the backing bitmap being prematurely garbage-collected. (Bug with ConditionalWeakTable? https://bugzilla.xamarin.com/show_bug.cgi?id=21620)
		private Brush _fillStrongref;
		public Brush Fill
		{
			get => (Brush)this.GetValue(FillProperty);
			set
			{
				this.SetValue(FillProperty, value);
				_fillStrongref = value;
			}
		}

		public static DependencyProperty FillProperty { get; } = DependencyProperty.Register(
			"Fill",
			typeof(Brush),
			typeof(Shape),
			new FrameworkPropertyMetadata(
				defaultValue: SolidColorBrushHelper.Transparent,
				options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext | FrameworkPropertyMetadataOptions.LogicalChild,
				propertyChangedCallback: (s, e) => ((Shape)s).OnFillChanged((Brush)e.OldValue, (Brush)e.NewValue)
			)
		);

		private void OnFillChanged(Brush oldValue, Brush newValue)
		{
#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			// On Skia, OnFillBrushChanged will call GetOrCreateCompositionBrush and assign this to _shape.FillBrush
			// In this case, we don't really want to listen to brush changes as the Brush is responsible for synchronizing its internal composition brush
			OnFillBrushChanged();
#else
			_brushChangedSubscription?.Dispose();
			_brushChangedSubscription = Brush.SetupBrushChanged(newValue, ref _brushChanged, () => OnFillBrushChanged());
#endif
		}

		#endregion

		#region Stroke Dependency Property
		public Brush Stroke
		{
			get => (Brush)this.GetValue(StrokeProperty);
			set => this.SetValue(StrokeProperty, value);
		}

		public static DependencyProperty StrokeProperty { get; } = DependencyProperty.Register(
			nameof(Stroke),
			typeof(Brush),
			typeof(Shape),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				propertyChangedCallback: (s, e) => ((Shape)s).OnStrokeChanged((Brush)e.OldValue, (Brush)e.NewValue)
			) // Perf: WinUI uses AffectsMeasure, we optimize this and only invalidate measure if needed
		);

		private void OnStrokeChanged(Brush oldValue, Brush newValue)
		{
			if ((oldValue is null) ^ (newValue is null))
			{
				// Moving from null to non-null or vice-versa affects measure.
				InvalidateMeasure();
			}

#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			// On Skia, OnStrokeBrushChanged will call GetOrCreateCompositionBrush and assign this to _shape.StrokeBrush
			// In this case, we don't really want to listen to brush changes as the Brush is responsible for synchronizing its internal composition brush
			OnStrokeBrushChanged();
#else
			_strokeBrushChangedSubscription?.Dispose();
			_strokeBrushChangedSubscription = Brush.SetupBrushChanged(newValue, ref _strokeBrushChanged, () => OnStrokeBrushChanged());
#endif
		}

		#endregion

		#region StrokeMiterLimit Dependency Property
		// Declared here (not in Generated/) for its WinUI default of 10: the generated stub registered default(double) = 0,
		// which a renderer that honours the limit (CodeBrix.Android's shape views) turns into a bevel at every corner. The
		// Skia renderer does not read it yet, so it stays marked NotImplemented for the Skia/Core flavors, as before.
		[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__", "__CODEBRIX_CORE__")]
		public double StrokeMiterLimit
		{
			get => (double)this.GetValue(StrokeMiterLimitProperty);
			set => this.SetValue(StrokeMiterLimitProperty, value);
		}

		[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__", "__CODEBRIX_CORE__")]
		public static DependencyProperty StrokeMiterLimitProperty { get; } = DependencyProperty.Register(
			nameof(StrokeMiterLimit),
			typeof(double),
			typeof(Shape),
			new FrameworkPropertyMetadata(10.0d));
		#endregion

		#region StrokeThickness Dependency Property
		public double StrokeThickness
		{
			get => (double)this.GetValue(StrokeThicknessProperty);
			set => this.SetValue(StrokeThicknessProperty, value);
		}

		public static DependencyProperty StrokeThicknessProperty { get; } = DependencyProperty.Register(
			nameof(StrokeThickness),
			typeof(double),
			typeof(Shape),
			new FrameworkPropertyMetadata(
				defaultValue: 1.0d,
				propertyChangedCallback: (s, e) => ((Shape)s).OnStrokeThicknessChanged()
			) // Perf: WinUI uses AffectsMeasure, we optimize this and only invalidate measure if Stroke is not null
		);

		private void OnStrokeThicknessChanged()
		{
			if (Stroke is not null)
			{
				// Changing stroke thickness will only have effect if Stroke is not null.
				InvalidateMeasure();
			}
		}
		#endregion

		#region Stretch Dependency Property
		public Stretch Stretch
		{
			get => (Stretch)this.GetValue(StretchProperty);
			set => this.SetValue(StretchProperty, value);
		}

		public static DependencyProperty StretchProperty { get; } = DependencyProperty.Register(
			"Stretch",
			typeof(Stretch),
			typeof(Shape),
			new FrameworkPropertyMetadata(
				defaultValue: Stretch.None, // Note: this is overriden in ctor for Rectangle and Ellipse
				options: FrameworkPropertyMetadataOptions.AffectsMeasure
			)
		);
		#endregion

		#region StrokeDashArray Dependency Property
		public DoubleCollection StrokeDashArray
		{
			get => (DoubleCollection)this.GetValue(StrokeDashArrayProperty);
			set => this.SetValue(StrokeDashArrayProperty, value);
		}

		public static DependencyProperty StrokeDashArrayProperty { get; } = DependencyProperty.Register(
			"StrokeDashArray",
			typeof(DoubleCollection),
			typeof(Shape),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				options: FrameworkPropertyMetadataOptions.AffectsArrange
			)
		);
		#endregion

		// Do not invoke base.IsViewHit(): We don't have to have de FrameworkElement.Background to be hit testable!
		internal override bool IsViewHit()
			=> Fill != null
#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__ // we only add this condition for Skia and Wasm because these are the only platforms with proper hit-testing support for shapes. If we add it for other platforms, we get a different but still inaccurate behaviour, so we prefer to keep the behaviour as is.
				// TODO: Verify if this should also consider StrokeThickness (likely it should)
				|| Stroke != null
#endif
				;

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// Don't call base, we need to keep UIView.BackgroundColor set to transparent
		}
	}
}
