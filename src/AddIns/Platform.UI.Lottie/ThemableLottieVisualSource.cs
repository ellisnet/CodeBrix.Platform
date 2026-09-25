#if true
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Json;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
using CodeBrix.Platform.UI.Lottie;
using CodeBrix.Platform.UI.Lottie.Engine;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Disposables;
using Windows.UI;

#if HAS_CODEBRIX_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	[Bindable]
	public partial class ThemableLottieVisualSource : LottieVisualSourceBase, IThemableAnimatedVisualSource
	{
		//The colour-theming engine (WPE1 C9): the parsed document, its colour bindings and the rewriting - moved verbatim
		//into Engine/LottieColorTheme; this source keeps the stream handling and the update callback.
		private readonly LottieColorTheme _theme = new LottieColorTheme();

		private UpdatedAnimation? _updateCallback;
		private string? _sourceCacheKey;

		protected override bool IsPayloadNeedsToBeUpdated => true;

#if IS_UNIT_TESTS
		public void LoadForTests(
			IInputStream sourceJson,
			string sourceCacheKey,
			UpdatedAnimation updateCallback)
		{
			_updateCallback = updateCallback;
			LoadAndUpdate(default, sourceCacheKey, sourceJson);
		}

		public string? GetJson()
		{
			return _theme.GetJson();
		}
#endif

		protected override IDisposable? LoadAndObserveAnimationData(
			IInputStream sourceJson,
			string sourceCacheKey,
			UpdatedAnimation updateCallback)
		{
			var cts = new CancellationTokenSource();

			_updateCallback = updateCallback;

			LoadAndUpdate(cts.Token, sourceCacheKey, sourceJson);

			return Disposable.Create(() =>
			{
				cts.Cancel();
				cts.Dispose();
			});
		}

		private void LoadAndUpdate(
			CancellationToken ct,
			string sourceCacheKey,
			IInputStream sourceJson)
		{
			_sourceCacheKey = sourceCacheKey;

			// The theming below mutates the parsed document in place, which requires the
			// mutable System.Json mini-DOM vendored in this project.

			// LOAD & PARSE JSON
			LoadAndParseDocument(sourceJson);

			if (!_theme.HasDocument)
			{
				return;
			}

			// APPLY PROPERTIES
			_theme.ApplyProperties();

			// NOTIFY
			NotifyCallback();
		}

		private void LoadAndParseDocument(IInputStream sourceJson)
		{
			using (var stream = sourceJson.AsStreamForRead(0))
			{
				_theme.Load(stream);
			}
		}

		private void NotifyCallback()
		{
			if (_updateCallback is { } callback)
			{
				var json = _theme.GetJson();
				if (json is { })
				{
					callback(json, _theme.GetCacheKey(_sourceCacheKey));
				}
			}
		}
	}
}
#endif
