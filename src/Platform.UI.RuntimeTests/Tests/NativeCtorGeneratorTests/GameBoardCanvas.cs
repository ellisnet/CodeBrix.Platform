#if false
using Android.Content;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.NativeCtorGeneratorTests //Was previously: Uno.UI.RuntimeTests.Tests.NativeCtorGeneratorTests
{
	public partial class MyCustomView : Android.Views.View
	{
		public MyCustomView(Context context) : base(context) { }
		public MyCustomView(Context context, Android.Util.IAttributeSet attributeSet) : base(context, attributeSet) { }
	}

	public partial class GameBoardCanvas : MyCustomView
	{
		public GameBoardCanvas(Context context)
			: base(context)
		{
		}

		public GameBoardCanvas(Context context, Android.Util.IAttributeSet attributeSet)
			: base(context, attributeSet)
		{
		}
	}
}
#endif
