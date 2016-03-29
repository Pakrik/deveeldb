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

namespace Deveel.Data.Sql.Parser {
	/// <summary>
	/// The node that represents a switch in a <c>CASE</c> expression
	/// </summary>
	/// <seealso cref="SqlCaseExpressionNode"/>
	class CaseSwitchNode : SqlNode {
		internal CaseSwitchNode() {
		}

		/// <summary>
		/// Gets the conditional expression in the node.
		/// </summary>
		public IExpressionNode WhenExpression { get; private set; }

		/// <summary>
		/// Gets an optional fallback expression of the node.
		/// </summary>
		public IExpressionNode ThenExpression { get; private set; }

		private bool whenFound;

		/// <inheritdoc/>
		protected override ISqlNode OnChildNode(ISqlNode node) {
			if (node is SqlKeyNode && ((SqlKeyNode) node).Text == "WHEN") {
				whenFound = true;
			} else if (node is IExpressionNode) {
				if (whenFound) {
					WhenExpression = (IExpressionNode) node;
				} else {
					ThenExpression = (IExpressionNode) node;
				}
			}
			return base.OnChildNode(node);
		}
	}
}