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
using System.Runtime.Serialization;

using Deveel.Data.Sql.Tables;

namespace Deveel.Data.Sql.Query {
	[Serializable]
	class DistinctNode : SingleQueryPlanNode {
		public DistinctNode(IQueryPlanNode child, ObjectName[] columnNames) 
			: base(child) {
			ColumnNames = columnNames;
		}

		private DistinctNode(SerializationInfo info, StreamingContext context)
			: base(info, context) {
			ColumnNames = (ObjectName[]) info.GetValue("Columns", typeof(ObjectName[]));
		}

		public ObjectName[] ColumnNames { get; private set; }

		public override ITable Evaluate(IRequest context) {
			var table = Child.Evaluate(context);
			return table.DistinctBy(ColumnNames);
		}

		protected override void GetData(SerializationInfo info, StreamingContext context) {
			info.AddValue("Columns", ColumnNames);
		}
	}
}
