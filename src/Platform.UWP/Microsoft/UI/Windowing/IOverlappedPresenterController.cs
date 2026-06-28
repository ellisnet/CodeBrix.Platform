using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBrix.Platform.UI.Windowing; //Was previously: Uno.UI.Windowing

internal interface IOverlappedPresenterController
{
	void Restore();

	void Maximize();

	void Minimize(bool activateWindow);
}
