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

using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Triggers;

namespace Deveel.Data.Sql.Statements {
	public sealed class CreateProcedureTriggerStatement : SqlStatement {
		public CreateProcedureTriggerStatement(ObjectName triggerName, ObjectName tableName, ObjectName procedureName, TriggerEventType eventType) 
			: this(triggerName, tableName, procedureName, new SqlExpression[0], eventType) {
		}

		public CreateProcedureTriggerStatement(ObjectName triggerName, ObjectName tableName, ObjectName procedureName, SqlExpression[] args, TriggerEventType eventType) {
			if (triggerName == null)
				throw new ArgumentNullException("triggerName");
			if (tableName == null)
				throw new ArgumentNullException("tableName");
			if (procedureName == null)
				throw new ArgumentNullException("procedureName");

			TriggerName = triggerName;
			TableName = tableName;
			ProcedureName = procedureName;
			ProcedureArguments = args;
			EventType = eventType;
		}

		public ObjectName TriggerName { get; private set; }

		public ObjectName TableName { get; private set; }

		public TriggerEventType EventType { get; private set; }

		public ObjectName ProcedureName { get; private set; }

		public SqlExpression[] ProcedureArguments { get; private set; }
	}
}
