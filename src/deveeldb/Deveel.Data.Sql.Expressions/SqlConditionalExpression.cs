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

namespace Deveel.Data.Sql.Expressions {
	[Serializable]
	public sealed class SqlConditionalExpression : SqlExpression {
		internal SqlConditionalExpression(SqlExpression testExpression, SqlExpression trueExpression, SqlExpression falsExpression) {
			if (testExpression == null) 
				throw new ArgumentNullException("testExpression");
			if (trueExpression == null) 
				throw new ArgumentNullException("trueExpression");

			TrueExpression = trueExpression;
			TestExpression = testExpression;
			FalseExpression = falsExpression;
		}

		private SqlConditionalExpression(ObjectData data)
			: base(data) {
			TestExpression = data.GetValue<SqlExpression>("Test");
			TrueExpression = data.GetValue<SqlExpression>("True");
			FalseExpression = data.GetValue<SqlExpression>("False");
		}

		public SqlExpression TestExpression { get; private set; }

		public SqlExpression TrueExpression { get; private set; }

		public SqlExpression FalseExpression { get; set; }

		public override SqlExpressionType ExpressionType {
			get { return SqlExpressionType.Conditional; }
		}

		public override bool CanEvaluate {
			get { return true; }
		}

		protected override void GetData(SerializeData data) {
			data.SetValue("Test", TestExpression);
			data.SetValue("True", TrueExpression);
			data.SetValue("False", FalseExpression);
		}
	}
}