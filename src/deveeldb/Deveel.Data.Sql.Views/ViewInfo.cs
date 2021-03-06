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
using System.IO;
using System.Runtime.Serialization;

using Deveel.Data.Serialization;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Objects;
using Deveel.Data.Sql.Query;
using Deveel.Data.Sql.Tables;

namespace Deveel.Data.Sql.Views {
	[Serializable]
	public sealed class ViewInfo : IObjectInfo, ISerializable {
		public ViewInfo(TableInfo tableInfo, SqlQueryExpression queryExpression, IQueryPlanNode queryPlan) {
			if (tableInfo == null)
				throw new ArgumentNullException("tableInfo");
			if (queryExpression == null)
				throw new ArgumentNullException("queryExpression");

			TableInfo = tableInfo;
			QueryExpression = queryExpression;
			QueryPlan = queryPlan;
		}

		private ViewInfo(SerializationInfo info, StreamingContext context) {
			TableInfo = (TableInfo) info.GetValue("TableInfo", typeof (TableInfo));
			QueryExpression = (SqlQueryExpression) info.GetValue("QueryExpression", typeof (SqlQueryExpression));
			QueryPlan = (IQueryPlanNode) info.GetValue("QueryPlan", typeof (IQueryPlanNode));
		}

		public TableInfo TableInfo { get; private set; }

		public ObjectName ViewName {
			get { return TableInfo.TableName; }
		}

		public SqlQueryExpression QueryExpression { get; private set; }

		public IQueryPlanNode QueryPlan { get; private set; }

		public string Owner { get; set; }

		DbObjectType IObjectInfo.ObjectType {
			get { return DbObjectType.View; }
		}

		ObjectName IObjectInfo.FullName {
			get { return ViewName; }
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
			info.AddValue("TableInfo", TableInfo);
			info.AddValue("QueryPlan", QueryPlan);
			info.AddValue("QueryExpression", QueryExpression);
		}

		public static ViewInfo FromBinary(ISqlBinary binary) {
			using (var stream = binary.GetInput()) {
				var serializer = new BinarySerializer();
				return (ViewInfo) serializer.Deserialize(stream);
			}
		}

		public SqlBinary AsBinary() {
			using (var stream = new MemoryStream()) {
				var serializer = new BinarySerializer();
				serializer.Serialize(stream, this);
				stream.Flush();

				var data = stream.ToArray();
				return new SqlBinary(data);
			}
		}
	}
}