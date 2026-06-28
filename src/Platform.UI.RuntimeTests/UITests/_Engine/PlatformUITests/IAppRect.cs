#nullable enable

using System;
using System.Linq;

namespace CodeBrix.Platform.UITest; //Was previously: Uno.UITest

public interface IAppRect
{
	float Width { get; }
	float Height { get; }
	float X { get; }
	float Y { get; }
	float CenterX { get; }
	float CenterY { get; }
	float Right { get; }
	float Bottom { get; }
}
