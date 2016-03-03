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

namespace Deveel.Data.Sql.Parser {
	class CreateTriggerNode : SqlStatementNode {
		internal CreateTriggerNode() {
		}

		public ObjectName TriggerName { get; private set; }

		public bool IfNotExists { get; private set; }

		public bool Callback { get; private set; }

		public ObjectName ProcedureName { get; private set; }

		public IExpressionNode[] ProcedureArguments { get; private set; }

		public bool IsBefore { get; private set; }

		public bool IsAfter { get; private set; }

		public IEnumerable<string> Events { get; private set; }

		protected override void BuildStatement(SqlCodeObjectBuilder builder) {
			throw new NotImplementedException();
		}
	}
}
