using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CopyConstructorGenerator.Test;

using Verify = CSharpCodeFixVerifier<CopyConstructorGeneratorAnalyzer, CopyConstructorGeneratorCodeFixProvider>;

[TestClass]
public class UnitTest_JP {
	[TestInitialize]
	public void TestInitialize() {
		Thread.CurrentThread.CurrentUICulture = new CultureInfo( "ja-JP" );
		Thread.CurrentThread.CurrentCulture = new CultureInfo( "ja-JP" );
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
		                  /// コピーコンストラクター
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
				public int? Y { get; set; }
				public int Z;
			}
			""";

		var fixTest = """
			class A {
			    /// <summary>
			    /// コピーコンストラクター
			    /// </summary>
			    public A(A value)
			    {
			        this.X = value.X;
			        this.Y = value.Y;
			        this.Z = value.Z;
			    }

			    public int X { get; set; }
				public int? Y { get; set; }
				public int Z;
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
					/// コピーコンストラクター
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
		    /// コピーコンストラクター
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
			FixedState = { ExpectedDiagnostics = { expected } },
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
			    /// コピーコンストラクター
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
		};

		await test.RunAsync( CancellationToken.None );
	}

	[TestMethod]
	public async Task CodeFix_Class() {
		var testCode = """
		               class A {
		                   public A A1 { get; set; }
		                   public A? A2 { get; set; }
		               }
		               """;

		var fixTest = """
		              class A {
		                  /// <summary>
		                  /// コピーコンストラクター
		                  /// </summary>
		                  public A(A value)
		                  {
		                      this.A1 = new A(value.A1);
		                      this.A2 = value.A2 is not null ? new A(value.A2) : null;
		                  }

		                  public A A1 { get; set; }
		                  public A? A2 { get; set; }
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
	public async Task CodeFix_ListProperty() {
		var testCode = """
			using System.Collections.Generic;
			class A {
			    public List<string> List1 { get; set; }
				public List<string>? List2 { get; set; }
			}
			""";

		var fixTest = """
			using System.Collections.Generic;
			class A {
			    /// <summary>
			    /// コピーコンストラクター
			    /// </summary>
			    public A(A value)
			    {
			        this.List1 = value.List1.ToList();
			        this.List2 = value.List2?.ToList();
			    }

			    public List<string> List1 { get; set; }
				public List<string>? List2 { get; set; }
			}
			""";

		var expected = Verify.Diagnostic( "CopyConstructorGenerator" ).WithLocation( 2, 7 );

		var test = new Verify.Test() {
			TestCode = testCode,
			FixedCode = fixTest,
			ExpectedDiagnostics = { expected },
			FixedState = { ExpectedDiagnostics = { expected } },
			CodeActionIndex = 1,  // プロパティのみのコードフィックス（2番目）を選択
		};

		await test.RunAsync( CancellationToken.None );
	}

	[TestMethod]
	public async Task CodeFix_DictionaryProperty() {
		var testCode = """
		               using System.Collections.Generic;
		               class A {
		                   public Dictionary<string, string> Dic1 { get; set; }
		                   public Dictionary<string, string>? Dic2 { get; set; }
		               }
		               """;

		var fixTest = """
		              using System.Collections.Generic;
		              class A {
		                  /// <summary>
		                  /// コピーコンストラクター
		                  /// </summary>
		                  public A(A value)
		                  {
		                      this.Dic1 = value.Dic1.ToDictionary(k => k.Key, v => v.Value);
		                      this.Dic2 = value.Dic2?.ToDictionary(k => k.Key, v => v.Value);
		                  }

		                  public Dictionary<string, string> Dic1 { get; set; }
		                  public Dictionary<string, string>? Dic2 { get; set; }
		              }
		              """;

		var expected = Verify.Diagnostic( "CopyConstructorGenerator" ).WithLocation( 2, 7 );

		var test = new Verify.Test() {
			TestCode = testCode,
			FixedCode = fixTest,
			ExpectedDiagnostics = { expected },
			FixedState = { ExpectedDiagnostics = { expected } },
			CodeActionIndex = 1,  // プロパティのみのコードフィックス（2番目）を選択
		};

		await test.RunAsync( CancellationToken.None );
	}
}
