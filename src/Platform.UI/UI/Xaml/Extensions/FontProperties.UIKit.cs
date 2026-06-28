using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;

namespace CodeBrix.Platform.UI.Xaml; //Was previously: Uno.UI.Xaml

internal record struct FontProperties(
	nfloat Size,
	FontWeight Weight,
	FontStyle Style,
	FontStretch Stretch
);
