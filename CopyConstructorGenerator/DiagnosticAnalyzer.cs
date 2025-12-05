using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CopyConstructorGenerator {
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class CopyConstructorGeneratorAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "CopyConstructorGenerator";

		// You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
		// See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
		private static readonly LocalizableString Title = new LocalizableResourceString( nameof( Resources.AnalyzerTitle ), Resources.ResourceManager, typeof( Resources ) );
		private static readonly LocalizableString MessageFormat = new LocalizableResourceString( nameof( Resources.AnalyzerMessageFormat ), Resources.ResourceManager, typeof( Resources ) );
		private static readonly LocalizableString Description = new LocalizableResourceString( nameof( Resources.AnalyzerDescription ), Resources.ResourceManager, typeof( Resources ) );
		private const string Category = "Constructor";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor( DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description );

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create( Rule ); } }

		public override void Initialize( AnalysisContext context ) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );

			context.RegisterSyntaxNodeAction( AnalyzeSymbol, SyntaxKind.ClassDeclaration );
		}

		private static void AnalyzeSymbol( SyntaxNodeAnalysisContext context ) {
			if( context.Node is ClassDeclarationSyntax classDeclaration ) {

				var diagnostic = Diagnostic.Create( Rule, classDeclaration.Identifier.GetLocation() );
				context.ReportDiagnostic( diagnostic );
			}
		}


	}
}
