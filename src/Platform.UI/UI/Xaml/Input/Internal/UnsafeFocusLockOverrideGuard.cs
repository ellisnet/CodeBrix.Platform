using System;
using Microsoft.UI.Xaml.Input;

namespace CodeBrix.Platform.UI.Xaml.Input //Was previously: Uno.UI.Xaml.Input
{
	internal class UnsafeFocusLockOverrideGuard : IDisposable
	{
		private readonly FocusManager _focusManager;

		public UnsafeFocusLockOverrideGuard(FocusManager focusManager)
		{
			_focusManager = focusManager ?? throw new ArgumentNullException(nameof(focusManager));
			_focusManager.SetIgnoreFocusLock(true);
		}

		public void Dispose() => _focusManager.SetIgnoreFocusLock(false);
	}
}
