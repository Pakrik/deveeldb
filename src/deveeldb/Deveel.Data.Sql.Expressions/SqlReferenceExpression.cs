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

namespace Deveel.Data.Sql.Expressions {
	/// <summary>
	/// An expression that references an object within a context.
	/// </summary>
	[Serializable]
	public sealed class SqlReferenceExpression : SqlExpression {
		internal SqlReferenceExpression(ObjectName name) {
			ReferenceName = name;
		}

		private SqlReferenceExpression(SerializationInfo info, StreamingContext context)
			: base(info, context) {
			ReferenceName = (ObjectName)info.GetValue("Reference", typeof(ObjectName));
		}

		public override bool CanEvaluate {
			get { return true; }
		}

		/// <summary>
		/// Gets the name of the object referenced by the expression.
		/// </summary>
		public ObjectName ReferenceName { get; private set; }

		/// <inheritdoc/>
		public override SqlExpressionType ExpressionType {
			get { return SqlExpressionType.Reference; }
		}

		protected override void GetData(SerializationInfo info, StreamingContext context) {
			info.AddValue("Reference", ReferenceName, typeof(ObjectName));
		}
	}
}