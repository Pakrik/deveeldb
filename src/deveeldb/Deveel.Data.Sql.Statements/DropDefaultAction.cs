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

namespace Deveel.Data.Sql.Statements {
	[Serializable]
	public sealed class DropDefaultAction : IAlterTableAction {
		public DropDefaultAction(string columnName) {
			ColumnName = columnName;
		}

		private DropDefaultAction(ObjectData data) {
			ColumnName = data.GetString("Column");
		}

		public string ColumnName { get; private set; }

		AlterTableActionType IAlterTableAction.ActionType {
			get { return AlterTableActionType.DropDefault; }
		}

		void ISerializable.GetData(SerializeData data) {
			data.SetValue("Column", ColumnName);
		}
	}
}
