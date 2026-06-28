using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using CodeBrix.Platform.Analyzers.Tests.Verifiers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CodeBrix.Platform.Analyzers.Tests //Was previously: Uno.Analyzers.Tests
{
	using Verify = CSharpCodeFixVerifier<CodeBrixDoNotDisposeNativeViews, EmptyCodeFixProvider>;

	[TestClass]
	public class CodeBrixDoNotDisposeNativeViewsTests
	{
		private static string CodeBrixUIViewClass =
			"""
			namespace Foundation
			{
					public class NSObject : System.IDisposable
					{ 
						public virtual void Dispose() { }
						public virtual void Dispose(bool disposing) { }
					}
			}

			namespace UIKit
			{
					public class UIView : Foundation.NSObject
					{ 
						public override void Dispose(bool disposing) { }
					}
			}
			""";

		private static async Task TestWithPreprocessorDirective(string testCode, IEnumerable<string> preprocessorSymbols)
		{
			await new Verify.Test
			{
				TestCode = testCode,
				FixedCode = testCode,
				PreprocessorSymbols = preprocessorSymbols,
			}.RunAsync();
		}

		[TestMethod]
		public async Task Nothing()
		{
			var test = @"";

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_InheritedCallsDispose()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class MyInherited : UIKit.UIView { }

					public class TypeName
					{
						public static void Test()
						{
							var b = new MyInherited();
							[|b.Dispose()|];
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_InheritedCallsDisposeBool()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class MyInherited : UIKit.UIView { }

					public class TypeName
					{
						public static void Test()
						{
							var b = new MyInherited();
							[|b.Dispose(true)|];
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_InheritedCallsUsingBlock()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class MyInherited : UIKit.UIView { }

					public class TypeName
					{
						public static void Test()
						{
							using(var [|b|] = new MyInherited())
							{
							}
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}


		[TestMethod]
		public async Task When_InheritedCallsUsingBlockMultipleDeclarators()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class MyInherited : UIKit.UIView { }

					public class TypeName
					{
						public static void Test()
						{
							using(MyInherited [|b|] = new MyInherited(), [|b2|] =  new MyInherited())
							{
							}
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_InheritedDisposeCallsFromOutside()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class MyInherited : UIKit.UIView 
					{ 
						public override void Dispose()
						{
							base.Dispose();
						}
					}

					public class Test
					{
						public void Run()
						{
							var b = new MyInherited();
							[|b.Dispose()|];
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_InheritedCallsBaseFromDispose()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class MyInherited : UIKit.UIView 
					{ 
						public override void Dispose()
						{
							base.Dispose();
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}


		[TestMethod]
		public async Task When_InheritedCallsBaseNotInDispose()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1	
				{
					public class MyInherited : UIKit.UIView 
					{ 
						public void Test()
						{
							[|base.Dispose()|];
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_InheritedCallsUsingStatement()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class MyInherited : UIKit.UIView { }

					public class TypeName
					{
						public static void Test()
						{
							using var [|b = new MyInherited()|];
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_InheritedCallsUsingStatementMultipleDeclarators()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class MyInherited : UIKit.UIView { }

					public class TypeName
					{
						public static void Test()
						{
							using MyInherited [|b = new MyInherited()|], [|b2 = new MyInherited()|];
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_InheritedCallsUsingStatementDiscard()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class MyInherited : UIKit.UIView { }

					public class TypeName
					{
						public static void Test()
						{
							using var [|_ = new MyInherited()|];
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_DirectCallsDispose()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class TypeName
					{
						public static void Test()
						{
							[|new UIKit.UIView().Dispose()|];
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_DirectCallsDisposeBool()
		{
			var test =
				"""
				using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using System.Threading.Tasks;
				using System.Diagnostics;

				namespace ConsoleApplication1
				{
					public class TypeName
					{
						public static void Test()
						{
							[|new UIKit.UIView().Dispose(true)|];
						}
					}
				}
				""" + CodeBrixUIViewClass;

			await Verify.VerifyAnalyzerAsync(test);
		}
	}
}
