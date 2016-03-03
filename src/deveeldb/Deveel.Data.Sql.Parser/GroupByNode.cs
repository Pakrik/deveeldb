﻿// 
//  Copyright 2010-2015 Deveel
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
using System.Collections.Generic;
using System.Linq;

namespace Deveel.Data.Sql.Parser {
	/// <summary>
	/// A node that describes the <c>GROUP BY</c> clause in a SQL query.
	/// </summary>
	class GroupByNode : SqlNode {
		internal GroupByNode() {
		}

		/// <summary>
		/// Gets the expression node to group the results.
		/// </summary>
		public IEnumerable<IExpressionNode> GroupExpressions { get; private set; }

		/// <summary>
		/// Gets the <c>HAVING</c> expression used to filter the grouped results.
		/// </summary>
		public IExpressionNode HavingExpression { get; private set; }

		/// <inheritdoc/>
		protected override ISqlNode OnChildNode(ISqlNode node) {
			if (node.NodeName == "sql_expression_list") {
				GetGroupExpressions(node);
			} else if (node.NodeName == "having_clause_opt") {
				HavingExpression = node.ChildNodes.FirstOrDefault() as IExpressionNode;
			}

			return base.OnChildNode(node);
		}

		private void GetGroupExpressions(ISqlNode node) {
			var exps = new List<IExpressionNode>();
			foreach (var childNode in node.ChildNodes) {
				if (childNode is IExpressionNode)
					exps.Add((IExpressionNode) childNode);
			}

			GroupExpressions = exps.ToArray();
		}
	}
}