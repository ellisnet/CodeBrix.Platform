#if !CODEBRIX_REFERENCE_API

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;
using CodeBrix.Platform;
using CodeBrix.Platform.Extensions;

namespace Microsoft.UI.Xaml.Controls;

partial class TimePicker
{
	#region FlyoutPlacement DependencyProperty

	[CodeBrixOnly]
	public FlyoutPlacementMode FlyoutPlacement
	{
		get => (FlyoutPlacementMode)this.GetValue(FlyoutPlacementProperty);
		set => this.SetValue(FlyoutPlacementProperty, value);
	}

	[CodeBrixOnly]
	public static DependencyProperty FlyoutPlacementProperty { get; } =
		DependencyProperty.Register(
			nameof(FlyoutPlacement),
			typeof(FlyoutPlacementMode),
			typeof(TimePicker),
			new FrameworkPropertyMetadata(FlyoutPlacementMode.Full));

	#endregion
}

#endif
