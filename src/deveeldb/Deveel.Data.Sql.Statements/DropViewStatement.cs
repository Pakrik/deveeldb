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
using System.IO;
using System.Linq;

using Deveel.Data.Serialization;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Sql.Views;

namespace Deveel.Data.Sql.Statements {
	public sealed class DropViewStatement : SqlStatement, IPreparableStatement {
		public DropViewStatement(string[] viewNames) 
			: this(viewNames, false) {
		}

		public DropViewStatement(string[] viewNames, bool ifExists) {
			ViewNames = viewNames;
			IfExists = ifExists;
		}

		public string[] ViewNames { get; private set; }

		public bool IfExists { get; set; }

		IStatement IPreparableStatement.Prepare(IRequest context) {
			var viewNameList = ViewNames.ToList();
			var dropViews = new List<string>();

			foreach (var tableName in viewNameList) {
				if (dropViews.Contains(tableName, StringComparer.OrdinalIgnoreCase))
					throw new StatementPrepareException(String.Format("Duplicated table name '{0}' in the list of tables to drop.",
						tableName));

				dropViews.Add(tableName);
			}

			var resolvedNames = dropViews.Select(context.Query.ResolveObjectName);

			return new Prepared(resolvedNames.ToArray(), IfExists);
		}

		#region Prepared

		[Serializable]
		class Prepared : SqlStatement {
			public ObjectName[] ViewNames { get; set; }

			public bool IfExists { get; set; }

			public Prepared(ObjectName[] viewNames, bool ifExists) {
				ViewNames = viewNames;
				IfExists = ifExists;
			}

			private Prepared(ObjectData data) {
				ViewNames = data.GetValue<ObjectName[]>("ViewNames");
				IfExists = data.GetBoolean("IfExists");
			}

			protected override void GetData(SerializeData data) {
				data.SetValue("ViewNames", ViewNames);
				data.SetValue("IfExists", IfExists);
			}

			protected override void ExecuteStatement(ExecutionContext context) {
				context.Request.Query.DropViews(ViewNames, IfExists);
			}
		}

		#endregion
	}
}
