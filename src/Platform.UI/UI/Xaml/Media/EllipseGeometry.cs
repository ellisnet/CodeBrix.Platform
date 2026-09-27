using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media
{
	/// <summary>
	/// Represents the geometry of a circle or ellipse.
	/// </summary>
	/// <remarks>
	/// The platform path is built by the geometry platform contract (<c>IGeometryPlatform.CreateGeometrySource</c>); a
	/// change of <see cref="Center"/>, <see cref="RadiusX"/> or <see cref="RadiusY"/> re-measures the element that uses the
	/// geometry (for example a Path) and raises the geometry's change notification, as <see cref="RectangleGeometry"/> does.
	/// </remarks>
	public partial class EllipseGeometry : Geometry
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EllipseGeometry"/> class.
		/// </summary>
		public EllipseGeometry()
		{
		}

		/// <summary>
		/// Gets or sets the center point of the ellipse. The default is (0,0).
		/// </summary>
		public Point Center
		{
			get => (Point)this.GetValue(CenterProperty);
			set => this.SetValue(CenterProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="Center"/> dependency property.
		/// </summary>
		public static DependencyProperty CenterProperty { get; } =
			DependencyProperty.Register(
				nameof(Center), typeof(Point),
				typeof(EllipseGeometry),
				new FrameworkPropertyMetadata(
					default(Point),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: OnGeometryPropertyChanged));

		/// <summary>
		/// Gets or sets the x-radius of the ellipse. The default is 0.
		/// </summary>
		public double RadiusX
		{
			get => (double)this.GetValue(RadiusXProperty);
			set => this.SetValue(RadiusXProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="RadiusX"/> dependency property.
		/// </summary>
		public static DependencyProperty RadiusXProperty { get; } =
			DependencyProperty.Register(
				nameof(RadiusX), typeof(double),
				typeof(EllipseGeometry),
				new FrameworkPropertyMetadata(
					default(double),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: OnGeometryPropertyChanged));

		/// <summary>
		/// Gets or sets the y-radius of the ellipse. The default is 0.
		/// </summary>
		public double RadiusY
		{
			get => (double)this.GetValue(RadiusYProperty);
			set => this.SetValue(RadiusYProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="RadiusY"/> dependency property.
		/// </summary>
		public static DependencyProperty RadiusYProperty { get; } =
			DependencyProperty.Register(
				nameof(RadiusY), typeof(double),
				typeof(EllipseGeometry),
				new FrameworkPropertyMetadata(
					default(double),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: OnGeometryPropertyChanged));

		private static void OnGeometryPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
			=> ((EllipseGeometry)dependencyObject).RaiseGeometryChanged();

		/// <summary>
		/// The box the ellipse fills: Center +/- (RadiusX, RadiusY), a negative radius counting as its magnitude, then the
		/// geometry's <see cref="Geometry.Transform"/> (as <see cref="RectangleGeometry"/> does).
		/// </summary>
		private protected override Rect ComputeBounds()
		{
			var center = Center;
			var radiusX = Math.Abs(RadiusX);
			var radiusY = Math.Abs(RadiusY);
			var bounds = new Rect(center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY);

			return Transform is { } transform ? transform.TransformBounds(bounds) : bounds;
		}
	}
}
