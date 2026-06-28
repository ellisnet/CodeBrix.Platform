using System;
using Windows.ApplicationModel.DataTransfer;

namespace CodeBrix.Platform.ApplicationModel.DataTransfer //Was previously: Uno.ApplicationModel.DataTransfer
{
	internal interface IClipboardExtension
	{
		event EventHandler<object> ContentChanged;
		void StartContentChanged();
		void StopContentChanged();
		void Clear();
		void Flush();
		DataPackageView GetContent();
		void SetContent(DataPackage content);
	}
}
