using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using CodeBrix.Platform.Analyzers.Tests.Verifiers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CodeBrix.Platform.Analyzers.Tests //Was previously: Uno.Analyzers.Tests
{
	using Verify = CSharpCodeFixVerifier<CodeBrixNotImplementedAnalyzer, EmptyCodeFixProvider>;

	[TestClass]
	public class CodeBrixNotImplementedTests
	{
		private static string CodeBrixNotImplementedAtribute = @"
		#nullable enable
		namespace CodeBrix.Platform
		{
				[System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
				public sealed class NotImplementedAttribute : Attribute
				{
					public NotImplementedAttribute() { }

					public NotImplementedAttribute(params string[] platforms)
					{
						Platforms = platforms;
					}

					public string[]? Platforms { get; }
				}
		}";

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
		public async Task When_EmptyNotImplemented()
		{
			var test = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Threading.Tasks;
                using System.Diagnostics;

				namespace CodeBrix.Platform
				{
					[NotImplemented]
					public class TestClass { }
				}

                namespace ConsoleApplication1
                {
                    class TypeName
                    {
                        public TypeName()
                        {
                           var a = [|new CodeBrix.Platform.TestClass()|];
                        }
                    }
                }

			" + CodeBrixNotImplementedAtribute;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_EventAddNotImplemented()
		{
			var test =
				"""
					using System;
					using System.Collections.Generic;
					using System.Linq;
					using System.Text;
					using System.Threading.Tasks;
					using System.Diagnostics;

					namespace CodeBrix.Platform
					{
						public class TestClass 
						{ 
							[NotImplemented]
							public event Action MyEvent;
						}
					}

					namespace ConsoleApplication1
					{
					    class TypeName
					    {
					        public TypeName()
					        {
					           var a = new CodeBrix.Platform.TestClass();
							   [|a.MyEvent|] += delegate { };
					        }
					    }
					}
				""" + CodeBrixNotImplementedAtribute;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_EventRemoveNotImplemented()
		{
			var test =
				"""
					using System;
					using System.Collections.Generic;
					using System.Linq;
					using System.Text;
					using System.Threading.Tasks;
					using System.Diagnostics;

					namespace CodeBrix.Platform
					{
						public class TestClass 
						{ 
							[NotImplemented]
							public event Action MyEvent;
						}
					}

					namespace ConsoleApplication1
					{
					    class TypeName
					    {
					        public TypeName()
					        {
					           var a = new CodeBrix.Platform.TestClass();
							   [|a.MyEvent|] -= delegate { };
					        }
					    }
					}
				""" + CodeBrixNotImplementedAtribute;

			await Verify.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task When_SinglePlatform_Included()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					[NotImplemented("__WASM__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = [|new CodeBrix.Platform.TestClass()|];
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;
			await TestWithPreprocessorDirective(test, new[] { "__WASM__" });
		}

		[TestMethod]
		public async Task When_SinglePlatform_Excluded()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					[NotImplemented("__SKIA__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = new CodeBrix.Platform.TestClass();
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "__WASM__" });
		}

		[TestMethod]
		public async Task When_TwoPlatforms_Excluded()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					[NotImplemented("__SKIA__", "__IOS__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = new CodeBrix.Platform.TestClass();
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "__WASM__" });
		}

		[TestMethod]
		public async Task When_Generic_Excluded()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					[NotImplemented("__IOS__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = new CodeBrix.Platform.TestClass();
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "CODEBRIX_REFERENCE_API" });
		}

		[TestMethod]
		public async Task When_Generic_Partial_Excluded()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					[NotImplemented("__SKIA__", "__IOS__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = new CodeBrix.Platform.TestClass();
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "CODEBRIX_REFERENCE_API" });
		}

		[TestMethod]
		public async Task When_Generic_Included()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					[NotImplemented("__SKIA__", "__IOS__", "__WASM__")]
					public class TestClass { }
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = [|new CodeBrix.Platform.TestClass()|];
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "CODEBRIX_REFERENCE_API" });
		}


		[TestMethod]
		public async Task When_Generic_Member_Included()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					public class TestClass {
						[NotImplemented("__SKIA__", "__IOS__", "__WASM__")]
						public int Test { get; }
					}
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = [|new CodeBrix.Platform.TestClass().Test|];
							var b = new CodeBrix.Platform.TestClass()?[|.Test|];
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "CODEBRIX_REFERENCE_API" });
		}

		[TestMethod]
		public async Task When_Generic_Member_Partial_Excluded()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					public class TestClass {
						[NotImplemented("__IOS__", "__WASM__")]
						public int Test { get; }
					}
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var a = new CodeBrix.Platform.TestClass().Test;
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "CODEBRIX_REFERENCE_API" });
		}

		[TestMethod]
		public async Task When_Using_Object_Initializer_Syntax_Included()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					public class TestClass {
						[NotImplemented("__SKIA__", "__IOS__", "__WASM__")]
						public int Test { get; set; }
					}
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var x = new CodeBrix.Platform.TestClass { [|Test|] = 0 };
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "CODEBRIX_REFERENCE_API" });
		}

		[TestMethod]
		public async Task When_TypeOf_Included()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					[NotImplemented("__ANDROID__")]
					public class TestClass
					{
					}
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							_ = [|typeof(CodeBrix.Platform.TestClass)|];
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "__ANDROID__" });
		}

		[TestMethod]
		public async Task When_MethodReference_Included()
		{
			var test = """
				using System;

				namespace CodeBrix.Platform
				{
					public class TestClass
					{
						[NotImplemented("__ANDROID__")]
						public void M()
						{
						}
					}
				}

				namespace ConsoleApplication1
				{
					class TypeName
					{
						public TypeName()
						{
							var x = new CodeBrix.Platform.TestClass();
							Action action = [|x.M|];
							action();
						}
					}
				}
				""" + CodeBrixNotImplementedAtribute;

			await TestWithPreprocessorDirective(test, new[] { "__ANDROID__" });
		}
	}
}
