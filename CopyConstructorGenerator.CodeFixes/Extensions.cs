using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CopyConstructorGenerator {
	internal static class Extensions {

		public static TRoot InsertNodesBefore<TRoot>( this TRoot root, Func<TRoot, SyntaxNode> nodeInList, IEnumerable<SyntaxNode> newNodes ) where TRoot : SyntaxNode {
			return root.InsertNodesBefore( nodeInList( root ), newNodes );
		}
		//public static TRoot InsertNodesBefore<TRoot>( this TRoot root, SyntaxNode nodeInList, Func<SyntaxNode,SyntaxNode> newNode ) where TRoot : SyntaxNode {
		//	return root.InsertNodesBefore( nodeInList, new[] { newNode( nodeInList )  } );
		//}


		public static TRoot ReplaceNode<TRoot>( this TRoot root, Func<TRoot, SyntaxNode> oldFunc, Func<SyntaxNode, SyntaxNode> newFunc ) where TRoot : SyntaxNode {
			var oldNode = oldFunc( root );
			return root.ReplaceNode( oldNode, newFunc( oldNode ) );
		}


		//public static TRoot ReplaceNode<TRoot>( this TRoot root, Func<SyntaxNode, TRoot> oldFunc, Func<SyntaxNode, TRoot> newFunc ) where TRoot : SyntaxNode {
		//	var oldNode = oldFunc( root );
		//	return root.ReplaceNode( oldNode, newFunc( oldNode ) );
		//}

		//public static TRoot ReplaceNode<TRoot>( this TRoot root, SyntaxNode oldNode, Func<SyntaxNode, TRoot> newFunc ) where TRoot : SyntaxNode {
		//	return root.ReplaceNode( oldNode, newFunc( oldNode ) );
		//}


		
	}
}
