using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;

namespace CopyConstructorGenerator.Test;

using Verify = CSharpCodeFixVerifier<CopyConstructorGeneratorAnalyzer, CopyConstructorGeneratorCodeFixProvider>;

[TestClass]
public class UnitTest_EN {
	[TestInitialize]
	public void TestInitialize() {
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
	}

	//No diagnostics expected to show up
	[TestMethod]
	public async Task Nothing() {
		var test = @"";

		await Verify.VerifyAnalyzerAsync( test );
	}


	[TestMethod]
	public async Task TestMethod1() {
		var testCode = """
		               class A {
		               }

		               """;

		var fixTest = """
		              class A {
		                  /// <summary>
		                  /// Copy Constructor
		                  /// </summary>
		                  public A(A value)
		                  {
		              
		                  }
		              }

		              """;

		var expected = Verify.Diagnostic( "CopyConstructorGenerator" ).WithLocation( 1, 7 );

		var test = new Verify.Test() {
			TestCode = testCode,
			FixedCode = fixTest,
			ExpectedDiagnostics = { expected },
			FixedState = { ExpectedDiagnostics = { expected } },
		};

		await test.RunAsync( CancellationToken.None );
	}

	[TestMethod]
	public async Task TestMethod2() {
		var testCode = """
			class A {
			    public int X { get; set; }
			}

			""";

		var fixTest = """
			class A {
			    /// <summary>
			    /// Copy Constructor
			    /// </summary>
			    public A(A value)
			    {
			        this.X = value.X;
			    }

			    public int X { get; set; }
			}

			""";

		var expected = Verify.Diagnostic( "CopyConstructorGenerator" ).WithLocation( 1, 7 );

		var test = new Verify.Test() {
			TestCode = testCode,
			FixedCode = fixTest,
			ExpectedDiagnostics = { expected },
			FixedState = { ExpectedDiagnostics = { expected } },
		};

		await test.RunAsync( CancellationToken.None );
	}

	[TestMethod]
	public async Task TestMethod3() {
		var testCode = """
				class A {
					public A()
					{
						
					}

					public int X { get; set; }
				}
				""";

		var fixTest = """
				class A {
					/// <summary>
					/// Copy Constructor
					/// </summary>
					public A(A value)
					{
						this.X = value.X;
					}

					public A()
					{
						
					}

					public int X { get; set; }
				}
				""";

		var expected = Verify.Diagnostic( "CopyConstructorGenerator" ).WithLocation( 1, 7 );

		var test = new Verify.Test() {
			TestCode = testCode,
			FixedCode = fixTest,
			ExpectedDiagnostics = { expected },
			FixedState = { ExpectedDiagnostics = { expected } },
			TestState = {
				AnalyzerConfigFiles = {
					("/.editorconfig", """
						root = true

						[*.cs]
						indent_style = tab
						indent_size = 4
						""")
				}
			}
		};

		await test.RunAsync( CancellationToken.None );
	}

	/// <summary>
	/// 既存のコピーコンストラクターがある場合でも、先頭に追加される
	/// 複数のコピーコンストラクターが存在することになる
	/// </summary>
	/// <returns></returns>
	[TestMethod]
	public async Task TestMethod3_ExistingCopyConstructor() {
		var testCode = """
		class A {
		    public A(A value) {
		        // 既存のコピーコンストラクター
		    }
		    
		    public int X { get; set; }
		}

		""";

		var fixTest = """
		class A {
		    /// <summary>
		    /// Copy Constructor
		    /// </summary>
		    public A(A value)
		    {
		        this.X = value.X;
		    }

		    public A(A value) {
		        // 既存のコピーコンストラクター
		    }
		    
		    public int X { get; set; }
		}

		""";

		var expected = Verify.Diagnostic( "CopyConstructorGenerator" ).WithLocation( 1, 7 );

		var test = new Verify.Test() {
			TestCode = testCode,
			FixedCode = fixTest,
			ExpectedDiagnostics = { expected },
			FixedState = { ExpectedDiagnostics = { expected, DiagnosticResult.CompilerError("CS0111").WithSpan(10, 12, 10, 13).WithArguments("A", "A"), } },
			CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,

		};

		await test.RunAsync( CancellationToken.None );
	}

	[TestMethod]
	public async Task TestMethod4_PropertiesOnlyCodeFix() {
		var testCode = """
			class A {
			    public int X { get; set; }
			    private int num = 10;
			}

			""";

		var fixTest = """
			class A {
			    /// <summary>
			    /// Copy Constructor
			    /// </summary>
			    public A(A value)
			    {
			        this.X = value.X;
			    }

			    public int X { get; set; }
			    private int num = 10;
			}

			""";

		var expected = Verify.Diagnostic( "CopyConstructorGenerator" ).WithLocation( 1, 7 );

		var test = new Verify.Test() {
			TestCode = testCode,
			FixedCode = fixTest,
			ExpectedDiagnostics = { expected },
			FixedState = { ExpectedDiagnostics = { expected } },
			CodeActionIndex = 1,  // プロパティのみのコードフィックス（2番目）を選択
			CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck,
		};

		await test.RunAsync( CancellationToken.None );
	}
}
