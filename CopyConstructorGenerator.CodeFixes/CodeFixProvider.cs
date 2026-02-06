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

			// 基底クラスのコピーコンストラクターの存在をチェック
			var hasBaseClassCopyConstructor = HasBaseClassCopyConstructor( model, classDeclaration );

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


				var newRegionConst = CreateCopyConstructor( className, values, hasBaseClassCopyConstructor );

				ClassDeclarationSyntax newClassDeclaration;
				if( classDeclaration.Members.Count == 0 ) {
					newClassDeclaration = 
						classDeclaration.AddMembers( newRegionConst.ToArray() )
							.ReplaceNode( r => r.Members.First(), r => r.WithAdditionalAnnotations( Formatter.Annotation ) );
				} else {
					newClassDeclaration =
						classDeclaration
							.ReplaceNode( r => r.Members.First(), f => f.WithLeadingTrivia( f.GetLeadingTrivia().InsertRange( 0, Enumerable.Range( 0, 1 ).Select( x => SyntaxFactory.CarriageReturnLineFeed ) ) ) )
							.InsertNodesBefore( r => r.Members.First(), newRegionConst )
							.ReplaceNode( r => r.Members.First(), r => r.WithAdditionalAnnotations( Formatter.Annotation ) );
				}

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

		/// <summary>
		/// 基底クラスにコピーコンストラクターが存在するかチェック
		/// </summary>
		static bool HasBaseClassCopyConstructor( SemanticModel model, ClassDeclarationSyntax classDeclaration ) {
			var classSymbol = model.GetDeclaredSymbol( classDeclaration ) as INamedTypeSymbol;
			if( classSymbol?.BaseType == null || classSymbol.BaseType.SpecialType == SpecialType.System_Object ) {
				return false;
			}

			var baseType = classSymbol.BaseType;

			// 基底クラスのコンストラクターをチェック
			var copyConstructor = baseType.Constructors.FirstOrDefault( ctor =>
				!ctor.IsStatic && ctor.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals( ctor.Parameters[0].Type, baseType )
			);

			return copyConstructor != null;
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

			if( type is QualifiedNameSyntax qualifiedNameSyntax ) {
				type = qualifiedNameSyntax.Right;
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
	/// コピーコンストラクターを生成
	/// </summary>
	static IEnumerable<MemberDeclarationSyntax> CreateCopyConstructor( string className, IEnumerable<string> values, bool hasBaseClassCopyConstructor ) {
		var baseInitializer = hasBaseClassCopyConstructor ? " : base(value)" : "";

			// ParseText で生成すると コンストラクター として認識されないため、ダミークラスに埋め込んでから取り出す
			var regionSource = $$"""
			class Dummy {
				{{CodeFixResources.summary}}
				public {{className}}({{className}} value){{baseInitializer}}
				{
					{{string.Join( "\r\n", values.ToArray() )}}
				}
			}
			""";
		var tree = CSharpSyntaxTree.ParseText( regionSource );
		var root = tree.GetRoot();

		// ダミークラスからコンストラクターメンバーを取り出す
		return root.DescendantNodes()
				   .OfType<ClassDeclarationSyntax>()
				   .First()
				   .Members;
	}

	}
}