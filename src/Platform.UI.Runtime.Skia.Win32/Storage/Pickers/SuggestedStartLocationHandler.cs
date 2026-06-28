using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Helpers;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using static CodeBrix.Platform.WinRTFeatureConfiguration.Storage;

namespace CodeBrix.Platform.UI.Runtime.Skia.Win32.Storage.Pickers; //Was previously: Uno.UI.Runtime.Skia.Win32.Storage.Pickers

internal static class SuggestedStartLocationHandler
{
	internal static unsafe ComScope<IShellItem> GetStartLocationShellItem(PickerLocationId startLocation)
	{
		if (startLocation == PickerLocationId.Unspecified)
		{
			return default;
		}

		Guid? folderGuid = startLocation is PickerLocationId.ComputerFolder ?
			PickerHelpers.WindowsComputerFolderGUID :
			null;

		var folderPath = folderGuid is null ?
			PickerHelpers.GetInitialDirectory(startLocation) :
			null;

		if (folderGuid is not null || !string.IsNullOrEmpty(folderPath))
		{
			void* defaultFolderItemRaw;
			var iid = IShellItem.IID_Guid;
			HRESULT hResult;
			if (folderGuid is not null)
			{
				var folderId = folderGuid.Value;
				hResult = PInvoke.SHCreateItemInKnownFolder(&folderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, &iid, &defaultFolderItemRaw);
			}
			else
			{
				fixed (char* folderPathPtr = folderPath)
				{
					hResult = PInvoke.SHCreateItemFromParsingName(new PCWSTR(folderPathPtr), null, &iid, &defaultFolderItemRaw);
				}
			}

			if (hResult.Failed)
			{
				var methodName = folderGuid is not null ?
					nameof(PInvoke.SHCreateItemInKnownFolder) :
					nameof(PInvoke.SHCreateItemFromParsingName);
				typeof(SuggestedStartLocationHandler).LogError()?.Error($"{methodName} failed: {Win32Helper.GetErrorMessage(hResult)}");
			}

			return new((IShellItem*)defaultFolderItemRaw);
		}

		return default;
	}
}
