#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using System.Numerics;
using CodeBrix.Platform.UI.Composition.Contracts;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class CompositionShape
{
	private Matrix3x2 _combinedTransformMatrix = Matrix3x2.Identity;
	private ICompositionShapePlatform? _platform;

	/// <summary>
	/// Gets the platform state of this shape (what draws it), created the first time it is needed.
	/// </summary>
	internal ICompositionShapePlatform Platform => _platform ??= CompositionPlatformServices.Composition.CreateShapePlatform(this);

	internal Matrix3x2 CombinedTransformMatrix
	{
		get => _combinedTransformMatrix;
		private set => SetProperty(ref _combinedTransformMatrix, value);
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

		switch (propertyName)
		{
			case nameof(TransformMatrix) or nameof(Scale) or nameof(RotationAngle) or nameof(CenterPoint):
				var transform = TransformMatrix;

				if (Scale != Vector2.One)
				{
					transform *= Matrix3x2.CreateScale(Scale, CenterPoint);
				}

				if (RotationAngle is not 0)
				{
					transform *= Matrix3x2.CreateRotation(RotationAngle, CenterPoint);
				}

				CombinedTransformMatrix = transform;
				break;
		}
	}

	internal bool CanPaint() => Platform.CanPaint();

	internal bool HitTest(Point point) => Platform.HitTest(point);
}
#endif
