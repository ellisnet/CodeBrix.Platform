#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeBrix.Platform.Extensions.Threading;

#if HAS_CODEBRIX_WINUI
using CommunityToolkit.WinUI.Lottie;
#else
using Microsoft.Toolkit.Uwp.UI.Lottie;
#endif

namespace CodeBrix.Platform.UI.Tests.Lottie //Was previously: Uno.UI.Tests.Lottie
{
	[TestClass]
	public class Given_ThemableLottieVisualSource
	{
		[TestMethod]
		public void ThemableLottieVisualSource_SimpleLoading()
		{
			var sut = new ThemableLottieVisualSource();

			var results = new List<string>();

			sut.LoadForTests(GetStream(), "cache-key", Callback);

			void Callback(string animationjson, string cachekey)
			{
				results.Add(animationjson);
			}

			results.Should().HaveCount(1);
		}

		[TestMethod]
		public void ThemableLottieVisualSource_ValueSet_BeforeLoading()
		{
			var reference = new ThemableLottieVisualSource();

			reference.LoadForTests(GetStream(), "cache-key", (x, y) => { });

			var sut = new ThemableLottieVisualSource();

			var color = Color.FromArgb(1, 2, 3, 4);
			sut.SetColorThemeProperty("Foreground", color);
			sut.GetColorThemeProperty("Foreground").Should().Be(color);

			var results = new List<string>();

			sut.LoadForTests(GetStream(), "cache-key", Callback);

			void Callback(string animationjson, string cachekey)
			{
				results.Add(animationjson);
			}

			results.Should().HaveCount(1);

			var jsonReference = reference.GetJson();
			sut.GetJson().Should().NotBe(jsonReference);
			sut.GetColorThemeProperty("Foreground").Should().Be(color);
		}

		[TestMethod]
		public void ThemableLottieVisualSource_ValueSet_AfterLoading()
		{
			var reference = new ThemableLottieVisualSource();

			reference.LoadForTests(GetStream(), "cache-key", (x, y) => { });

			var sut = new ThemableLottieVisualSource();

			var results = new List<string>();

			sut.LoadForTests(GetStream(), "cache-key", Callback);

			void Callback(string animationjson, string cachekey)
			{
				results.Add(animationjson);
			}

			results.Should().HaveCount(1);

			var color = Color.FromArgb(1, 2, 3, 4);
			sut.SetColorThemeProperty("Foreground", color);
			sut.GetColorThemeProperty("Foreground").Should().Be(color);

			results.Should().HaveCount(2);

			var jsonReference = reference.GetJson();
			sut.GetJson().Should().NotBe(jsonReference);
			sut.GetColorThemeProperty("Foreground").Should().Be(color);
		}

		private IInputStream GetStream(string name = "animation.json")
		{
			var type = GetType();
			var assembly = type.Assembly;
			var resourceName = "CodeBrix.Platform.UI.Tests.Lottie." + name;
			var stream = assembly.GetManifestResourceStream(resourceName);

			if (stream == null)
			{
				throw new InvalidOperationException("Unable to find embedded resource named " + resourceName);
			}

			return stream.AsInputStream();
		}
	}
}
