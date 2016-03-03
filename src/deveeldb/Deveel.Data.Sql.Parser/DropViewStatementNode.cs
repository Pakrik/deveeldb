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

using Deveel.Data.Sql.Statements;

namespace Deveel.Data.Sql.Parser {
	class DropViewStatementNode : SqlStatementNode {
		public IEnumerable<string> ViewNames { get; private set; }

		public bool IfExists { get; private set; }

		protected override void OnNodeInit() {
			ViewNames = this.FindNodes<ObjectNameNode>().Select(x => x.Name);
			var ifExistsOpt = this.FindByName("if_exists_opt");
			if (ifExistsOpt != null && ifExistsOpt.ChildNodes.Any())
				IfExists = true;

			base.OnNodeInit();
		}

		protected override void BuildStatement(SqlCodeObjectBuilder builder) {
			foreach (var viewName in ViewNames) {
				var name = ObjectName.Parse(viewName);
				builder.AddObject(new DropViewStatement(name, IfExists));
			}
		}
	}
}
