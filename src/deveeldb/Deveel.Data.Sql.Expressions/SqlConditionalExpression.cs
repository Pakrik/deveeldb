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
using System.Runtime.Serialization;

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

		private SqlConditionalExpression(SerializationInfo info, StreamingContext context)
			: base(info, context) {
			TestExpression = (SqlExpression)info.GetValue("Test", typeof(SqlExpression));
			TrueExpression = (SqlExpression)info.GetValue("True", typeof(SqlExpression));
			FalseExpression = (SqlExpression)info.GetValue("False", typeof(SqlExpression));
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

		protected override void GetData(SerializationInfo info, StreamingContext context) {
			info.AddValue("Test", TestExpression, typeof(SqlExpression));
			info.AddValue("True", TrueExpression, typeof(SqlExpression));
			info.AddValue("False", FalseExpression, typeof(SqlExpression));
		}

		internal override void AppendTo(SqlStringBuilder builder) {
			builder.Append("CASE WHEN ");
			TestExpression.AppendTo(builder);
			builder.Append(" THEN ");
			TrueExpression.AppendTo(builder);

			if (FalseExpression != null) {
				builder.Append(" ELSE ");
				FalseExpression.AppendTo(builder);
			}
		}
	}
}