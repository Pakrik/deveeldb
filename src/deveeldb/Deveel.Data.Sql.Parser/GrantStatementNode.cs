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

using Deveel.Data.Security;
using Deveel.Data.Sql.Statements;

namespace Deveel.Data.Sql.Parser {
	class GrantStatementNode : SqlStatementNode {
		public string ObjectName { get; private set; }

		public IEnumerable<PrivilegeNode> Privileges { get; private set; }

		public IEnumerable<string> Grantees { get; private set; }

		public bool WithGrant { get; private set; }

		protected override ISqlNode OnChildNode(ISqlNode node) {
			if (node is ObjectNameNode) {
				ObjectName = ((ObjectNameNode) node).Name;
			} else if (node.NodeName.Equals("object_priv")) {
				GetPrivileges(node);
			} else if (node.NodeName.Equals("distribution_list")) {
				GetGrantees(node);
			} else if (node.NodeName.Equals("with_grant_opt")) {
				GetWithGrant(node);
			}

			return base.OnChildNode(node);
		}

		private void GetWithGrant(ISqlNode node) {
			if (node.ChildNodes.Any())
				WithGrant = true;
		}

		private void GetGrantees(ISqlNode node) {
			Grantees = node.ChildNodes.OfType<IdentifierNode>().Select(x => x.Text);
		}

		private void GetPrivileges(ISqlNode node) {
			bool isAll = false;

			foreach (var childNode in node.ChildNodes) {
				if (childNode is SqlKeyNode) {
					if (((SqlKeyNode) childNode).Text.Equals("ALL", StringComparison.OrdinalIgnoreCase)) {
						isAll = true;
					} else if (!((SqlKeyNode) childNode).Text.Equals("PRIVILEGES", StringComparison.OrdinalIgnoreCase)) {
						throw new InvalidOperationException();
					}
				} else if (childNode.NodeName.Equals("priv_list")) {
					Privileges = childNode.ChildNodes.OfType<PrivilegeNode>();
				}
			}

			if (isAll)
				Privileges = new[] {PrivilegeNode.All};
		}

		protected override void BuildStatement(StatementBuilder builder) {
			var objName = Sql.ObjectName.Parse(ObjectName);
			foreach (var grantee in Grantees) {
				foreach (var privilegeNode in Privileges) {
					var privilege = ParsePrivilege(privilegeNode.Privilege);
					builder.Statements.Add(new GrantPrivilegesStatement(grantee, privilege, WithGrant, objName, privilegeNode.Columns));
				}
			}
		}

		private static Privileges ParsePrivilege(string privName) {
			try {
				return (Privileges) Enum.Parse(typeof (Privileges), privName, true);
			} catch (Exception) {
				throw new InvalidOperationException(String.Format("Invalid privilege name '{0}' specified.", privName));
			}
		}
	}
}