using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media
{
	/// <summary>
	/// Represents the geometry of a line.
	/// </summary>
	/// <remarks>
	/// The platform path is built by the geometry platform contract (<c>IGeometryPlatform.CreateGeometrySource</c>); a
	/// change of <see cref="StartPoint"/> or <see cref="EndPoint"/> re-measures the element that uses the geometry (for
	/// example a Path) and raises the geometry's change notification, as <see cref="RectangleGeometry"/> does. A line
	/// encloses no area, so a Path shows it through its Stroke only.
	/// </remarks>
	public partial class LineGeometry : Geometry
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LineGeometry"/> class.
		/// </summary>
		public LineGeometry()
		{
		}

		/// <summary>
		/// Gets or sets the start point of the line. The default is (0,0).
		/// </summary>
		public Point StartPoint
		{
			get => (Point)this.GetValue(StartPointProperty);
			set => this.SetValue(StartPointProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="StartPoint"/> dependency property.
		/// </summary>
		public static DependencyProperty StartPointProperty { get; } =
			DependencyProperty.Register(
				nameof(StartPoint), typeof(Point),
				typeof(LineGeometry),
				new FrameworkPropertyMetadata(
					default(Point),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: OnGeometryPropertyChanged));

		/// <summary>
		/// Gets or sets the end point of the line. The default is (0,0).
		/// </summary>
		public Point EndPoint
		{
			get => (Point)this.GetValue(EndPointProperty);
			set => this.SetValue(EndPointProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="EndPoint"/> dependency property.
		/// </summary>
		public static DependencyProperty EndPointProperty { get; } =
			DependencyProperty.Register(
				nameof(EndPoint), typeof(Point),
				typeof(LineGeometry),
				new FrameworkPropertyMetadata(
					default(Point),
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: OnGeometryPropertyChanged));

		private static void OnGeometryPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
			=> ((LineGeometry)dependencyObject).RaiseGeometryChanged();

		/// <summary>
		/// The smallest box that holds both end points, then the geometry's <see cref="Geometry.Transform"/> (as
		/// <see cref="RectangleGeometry"/> does).
		/// </summary>
		private protected override Rect ComputeBounds()
		{
			var bounds = new Rect(StartPoint, EndPoint);

			return Transform is { } transform ? transform.TransformBounds(bounds) : bounds;
		}
	}
}
