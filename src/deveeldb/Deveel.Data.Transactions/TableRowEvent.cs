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
using System.Collections.Generic;

using Deveel.Data.Diagnostics;

namespace Deveel.Data.Transactions {
	public class TableRowEvent : Event, ITableEvent {
		private ITableEvent tableEventImplementation;

		public TableRowEvent(int tableId, int rowNumber, TableRowEventType eventType) {
			TableId = tableId;
			RowNumber = rowNumber;
			EventType = eventType;
		}

		public int TableId { get; private set; }

		public int RowNumber { get; private set; }

		public TableRowEventType EventType { get; private set; }

		protected override void GetEventData(Dictionary<string, object> data) {
			data["table.id"] = TableId;
			data["table.row"] = RowNumber;
			data["eventType"] = EventType.ToString().ToLowerInvariant();
		}
	}
}
