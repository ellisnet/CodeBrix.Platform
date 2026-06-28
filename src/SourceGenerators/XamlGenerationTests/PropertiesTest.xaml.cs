using System;
using System.Collections.Generic;
using System.Text;

namespace XamlGenerationTests.Shared
{
	public partial class PropertiesTest
	{
		public PropertiesTest()
		{
#if PREVIOUS_IOS_SUPPORT
			iOSUILabel.ToString();
#endif

#if PREVIOUS_ANDROID_SUPPORT
			AndroidTextView.ToString();
#endif

			GradientStopEffect.ToString();
			testRun.ToString();
			rtbRun.ToString();
		}
	}
}
