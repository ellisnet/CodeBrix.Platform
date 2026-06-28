#nullable enable

using System.Windows;
using CodeBrix.Platform.ApplicationModel.Core;

namespace CodeBrix.Platform.Extensions.ApplicationModel.Core
{
	internal class CoreApplicationExtension : ICoreApplicationExtension
	{
		public CoreApplicationExtension(object? owner)
		{
		}

		public bool CanExit => true;

		public void Exit() => Application.Current.Shutdown();
	}
}
