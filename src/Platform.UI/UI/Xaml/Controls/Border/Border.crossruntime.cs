using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform.Extensions;
using System.Linq;
using System.Drawing;
using CodeBrix.Platform.Extensions.Disposables;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.UI;

using View = Microsoft.UI.Xaml.UIElement;
using Color = System.Drawing.Color;
using Microsoft.UI.Composition;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Xaml.Shapes;
using CodeBrix.Platform.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class Border
{
	partial void OnBackgroundChangedPartial() => UpdateHitTest();
}
