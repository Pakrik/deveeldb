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

using Deveel.Data.Serialization;
using Deveel.Data.Sql.Query;

namespace Deveel.Data.Sql.Expressions {
	[Serializable]
	class QueryReferenceExpression : SqlExpression {
		public QueryReferenceExpression(QueryReference reference) {
			QueryReference = reference;
		}

		private QueryReferenceExpression(ObjectData data) {
			QueryReference = data.GetValue<QueryReference>("Reference");
		}

		public QueryReference QueryReference { get; private set; }

		public override SqlExpressionType ExpressionType {
			get { return SqlExpressionType.Reference; }
		}

		protected override void GetData(SerializeData data) {
			data.SetValue("Reference", QueryReference);
		}
	}
}