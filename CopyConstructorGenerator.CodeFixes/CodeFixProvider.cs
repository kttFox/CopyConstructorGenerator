using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;


namespace CopyConstructorGenerator {
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( CopyConstructorGeneratorCodeFixProvider ) ), Shared]
	public class CopyConstructorGeneratorCodeFixProvider : CodeFixProvider {

		public sealed override ImmutableArray<string> FixableDiagnosticIds {
			get { return ImmutableArray.Create( CopyConstructorGeneratorAnalyzer.DiagnosticId ); }
		}

		public sealed override FixAllProvider GetFixAllProvider() {
			return WellKnownFixAllProviders.BatchFixer;
		}


		public sealed override async Task RegisterCodeFixesAsync( CodeFixContext context ) {

			var model = await context.Document.GetSemanticModelAsync( context.CancellationToken );

			var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false ) as CompilationUnitSyntax;

			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;
			var classDeclaration = root.FindToken( diagnosticSpan.Start ).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

			var className = classDeclaration.Identifier.Text;

			var members = classDeclaration.Members
							.Where( x => !x.Modifiers.Any( SyntaxKind.StaticKeyword ) )
							.Where( x => !x.Modifiers.Any( SyntaxKind.ConstKeyword ) )
							.Where( x => x switch {
								PropertyDeclarationSyntax property => property.AccessorList?.Accessors.Any( SyntaxKind.GetAccessorDeclaration ) == true,// Getアクセスがあるプロパティのみを対象とする。
								FieldDeclarationSyntax => true,
								_ => false,
							} );

			if( members.Any() ) {
				Task<Document> CreateChangedDocument( IEnumerable<MemberDeclarationSyntax> members ) {
					var values = members.Select( x => {
						switch( x ) {
							case PropertyDeclarationSyntax prop: {
								var name = prop.Identifier.Text;

								return CreateCopyValue( model, prop.Type, name );
							}
							case FieldDeclarationSyntax field: {
								var f = field.Declaration;
								var name = f.Variables.First().Identifier.Text;

								return CreateCopyValue( model, f.Type, name );
							}
							default: {
								throw new Exception();
							}
						}
					} );

					var newRegionConst = CreateCopyConstructor( className, values );

					var newClassDeclaration = classDeclaration
												.ReplaceNode( r => r.Members.First(), f => f.WithLeadingTrivia( f.GetLeadingTrivia().AddRange( Enumerable.Range( 0, 2 ).Select( x => SyntaxFactory.ElasticCarriageReturnLineFeed ) ) ) )
												.InsertNodesBefore( r => r.Members.First(), newRegionConst )
												.ReplaceNode( r => r.Members.First(), r => r.WithAdditionalAnnotations( Formatter.Annotation ) );

					var newRoot = root.ReplaceNode( classDeclaration, newClassDeclaration );

					var newDocument = context.Document.WithSyntaxRoot( newRoot );

					return Task.FromResult( newDocument );
				}

				// コード編集を登録します。
				context.RegisterCodeFix(
					CodeAction.Create( CodeFixResources.CodeFixTitle, _ => CreateChangedDocument( members ) ), diagnostic );

				context.RegisterCodeFix(
					CodeAction.Create( CodeFixResources.CodeFixTitleProperyOnly, _ => CreateChangedDocument( members.OfType<PropertyDeclarationSyntax>() ) ), diagnostic );
			}
		}

		static string CreateCopyValue( SemanticModel model, TypeSyntax type, string valueName ) {
			return $"this.{valueName} = {GetDeepInstance( model, type, $"value.{valueName}" )};";
		}

		static readonly string[] genericArgs = ["x", "z", "k"];

		static string GetDeepInstance( SemanticModel model, TypeSyntax type, string value, int count = 0 ) {
			if( type is GenericNameSyntax generic ) {

				// List と　Dictionary
				switch( generic.Identifier.Text ) {
					case "List": {
						var arg = generic.TypeArgumentList.Arguments.First();
						if( arg is PredefinedTypeSyntax ) {
							return $"{value}.ToList()";
						} else {
							var T = ( count < genericArgs.Length ) ? genericArgs[count] : "x" + count;

							return $"{value}.Select({T}=> {GetDeepInstance( model, arg, $"{T}", count + 1 )} ).ToList()";
						}
					}

					case "Dictionary": {
						if( generic.TypeArgumentList.Arguments.Any( x => x is not PredefinedTypeSyntax ) ) {
							var keyType = generic.TypeArgumentList.Arguments[0];
							var valueType = generic.TypeArgumentList.Arguments[1];

							var k = ( count == 0 ) ? "k" : "k" + count;
							var v = ( count == 0 ) ? "v" : "v" + count;

							return $"{value}.ToDictionary({k}=>{GetDeepInstance( model, keyType, $"{k}.Key", count + 1 )}, {v}=> {GetDeepInstance( model, valueType, $"{v}.Value", count + 1 )} )";
						} else {
							return $"{value}.ToDictionary(k=>k.Key, v=>v.Value )";
						}
					}

					default:
						break;
				}

				// Generic型の何か
				return $"new {type}({value}. )";
			}

			// struct or class

			switch( type ) {
				case IdentifierNameSyntax: {
					switch( ( model.GetSymbolInfo( type ).Symbol as ITypeSymbol )?.TypeKind ) {
						case TypeKind.Enum:
						case TypeKind.Struct:
							return value;
					}

					break;
				}
				case PredefinedTypeSyntax:
				case NullableTypeSyntax:
					return value;
			}

			return $"new {type}({value})";
		}

		/// <summary>

		/// </summary>
		static IEnumerable<MemberDeclarationSyntax> CreateCopyConstructor( string className, IEnumerable<string> values ) {
			var regionSource =
					$$"""
						{{CodeFixResources.summary}}
						public {{className}} ( {{className}} value ) {
							{{string.Join( "\r\n", values.ToArray() )}}
						}
					""";
			return CSharpSyntaxTree.ParseText( regionSource ).GetRoot()
									.ChildNodes()
									.OfType<MemberDeclarationSyntax>();
		}

	}
}