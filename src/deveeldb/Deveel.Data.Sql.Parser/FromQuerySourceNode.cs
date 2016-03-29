﻿// 
//  Copyright 2010-2016 Deveel
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//


using System;
using System.Linq;

namespace Deveel.Data.Sql.Parser {
	/// <summary>
	/// A node in the grammar tree that defines a sub-query in a
	/// <c>FROM</c> clause.
	/// </summary>
	/// <seealso cref="IFromSourceNode"/>
	class FromQuerySourceNode : SqlNode, IFromSourceNode {
		internal FromQuerySourceNode() {	
		}

		/// <summary>
		/// Gets the <see cref="SqlQueryExpressionNode">node</see> that represents
		/// the sub-qury that is the source of a query.
		/// </summary>
		public SqlQueryExpressionNode Query { get; private set; }

		/// <inheritdoc/>
		public IdentifierNode Alias { get; private set; }

		/// <inheritdoc/>
		protected override ISqlNode OnChildNode(ISqlNode node) {
			if (node is SqlQueryExpressionNode) {
				Query = (SqlQueryExpressionNode)node;
			} else if (node.NodeName == "select_as_opt") {
				Alias = (IdentifierNode)node.ChildNodes.FirstOrDefault();
			}

			return base.OnChildNode(node);
		}
	}
}