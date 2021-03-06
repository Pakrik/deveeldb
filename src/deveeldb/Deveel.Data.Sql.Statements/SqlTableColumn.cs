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

using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Types;

namespace Deveel.Data.Sql.Statements {
	[Serializable]
	public sealed class SqlTableColumn : IPreparable, IStatementPreparable, ISerializable {
		public SqlTableColumn(string columnName, SqlType columnType) {
			if (String.IsNullOrEmpty(columnName))
				throw new ArgumentNullException("columnName");
			if (columnType == null)
				throw new ArgumentNullException("columnType");
			
			ColumnName = columnName;
			ColumnType = columnType;
		}

		private SqlTableColumn(SerializationInfo info, StreamingContext context) {
			ColumnName = info.GetString("ColumnName");
			ColumnType = (SqlType) info.GetValue("ColumnType", typeof(SqlType));
			IsIdentity = info.GetBoolean("IsIdentity");
			IsNotNull = info.GetBoolean("IsNotNull");
			DefaultExpression = (SqlExpression) info.GetValue("Default", typeof(SqlExpression));
			IndexType = info.GetString("IndexType");
		}

		public string ColumnName { get; private set; }

		public SqlType ColumnType { get; private set; }

		public bool IsIdentity { get; set; }

		public SqlExpression DefaultExpression { get; set; }

		public string IndexType { get; set; }

		public bool HasDefaultExpression {
			get { return DefaultExpression != null; }
		}

		public bool IsNotNull { get; set; }

		object IPreparable.Prepare(IExpressionPreparer preparer) {
			var column = new SqlTableColumn(ColumnName, ColumnType);
			if (DefaultExpression != null)
				column.DefaultExpression = DefaultExpression.Prepare(preparer);

			column.IsNotNull = IsNotNull;
			column.IsIdentity = IsIdentity;
			column.IndexType = IndexType;
			return column;
		}

		object IStatementPreparable.Prepare(IRequest context) {
			var columnType = ColumnType.Resolve(context);
			return new SqlTableColumn(ColumnName, columnType) {
				IsIdentity = IsIdentity,
				IsNotNull = IsNotNull,
				IndexType = IndexType,
				DefaultExpression = DefaultExpression
			};
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
			info.AddValue("ColumnName", ColumnName);
			info.AddValue("ColumnType", ColumnType);
			info.AddValue("IsNotNull", IsNotNull);
			info.AddValue("IsIdentity", IsIdentity);
			info.AddValue("Default", DefaultExpression);
			info.AddValue("IndexType", IndexType);
		}
	}
}
