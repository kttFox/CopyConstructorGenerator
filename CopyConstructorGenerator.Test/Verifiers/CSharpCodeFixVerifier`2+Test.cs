using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace CopyConstructorGenerator.Test {
	public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
		where TAnalyzer : DiagnosticAnalyzer, new()
		where TCodeFix : CodeFixProvider, new() {
		public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> {
			public Test() {
				SolutionTransforms.Add( ( solution, projectId ) => {
					var compilationOptions = (CSharpCompilationOptions)solution.GetProject( projectId )!.CompilationOptions!;
					compilationOptions = compilationOptions
					//.WithSpecificDiagnosticOptions( compilationOptions.SpecificDiagnosticOptions.SetItems( CSharpVerifierHelper.NullableWarnings ) )
					// Nullable参照型のコンテキストを有効化
					.WithNullableContextOptions( NullableContextOptions.Enable );

					solution = solution.WithProjectCompilationOptions( projectId, compilationOptions );
					return solution;
				} );

				// コードフィックスのテストでは、コンパイラー診断は無視する。
				this.CompilerDiagnostics = CompilerDiagnostics.None;

				// 単一のコードフィックスのみをテストし、Fix All チェックはスキップする。
				this.CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;



			}

		}
	}
}
