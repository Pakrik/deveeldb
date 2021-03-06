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

using Deveel.Data.Sql;
using Deveel.Data.Sql.Tables;

namespace Deveel.Data.Caching {
	public sealed class CachedCell {
		internal CachedCell(CellKey key, Field value) {
			Key = key;
			Value = value;
		}

		public CellKey Key { get; private set; }

		public RowId RowId {
			get { return Key.RowId; }
		}

		public int TableId {
			get { return RowId.TableId; }
		}

		public long RowNumber {
			get { return RowId.RowNumber; }
		}

		public int ColumnOffset {
			get { return Key.ColumnOffset; }
		}

		public Field Value { get; private set; }

		public string Database {
			get { return Key.Database; }
		}
	}
}
