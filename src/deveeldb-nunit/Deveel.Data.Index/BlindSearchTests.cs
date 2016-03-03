﻿// 
//  Copyright 2010-2014 Deveel
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

using System;
using System.Collections.Generic;
using System.Linq;

using Deveel.Data;
using Deveel.Data.Sql;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Types;

using NUnit.Framework;

namespace Deveel.Data.Index {
	[TestFixture]
	public sealed class BlindSearchTests {
		private ITable table;

		private DateTimeOffset cornerTime;

		[SetUp]
		public void TestSetUp() {
			var tableName = ObjectName.Parse("APP.test_table");
			var tableInfo = new TableInfo(tableName);
			tableInfo.AddColumn("id", PrimitiveTypes.Numeric(), true);
			tableInfo.AddColumn("name", PrimitiveTypes.String(SqlTypeCode.VarChar));
			tableInfo.AddColumn("date", PrimitiveTypes.DateTime());

			cornerTime = DateTimeOffset.UtcNow;

			var tmpTable = new TemporaryTable(tableInfo);

			AddRow(tmpTable, 1, "test1", cornerTime);
			AddRow(tmpTable, 2, "test2", cornerTime.AddSeconds(2));
			AddRow(tmpTable, 3, "test3", cornerTime.AddSeconds(5));

			tmpTable.BuildIndexes(DefaultIndexTypes.BlindSearch);

			table = tmpTable;
		}

		private void AddRow(TemporaryTable tmpTable, long id, string name, DateTimeOffset date) {
			var row = new Field[3];
			row[0] = Field.BigInt(id);
			row[1] = Field.String(name);
			row[2] = Field.Date(date);
			tmpTable.NewRow(row);
		}

		[Test]
		public void SelectEqualOneColumn() {
			var name = Field.String("test1");
			var result = table.SelectRowsEqual(1, name);

			Assert.IsNotNull(result);
			Assert.IsNotEmpty(result);

			var index = result.First();
			Assert.AreEqual(0, index);
		}

		[Test]
		public void SelectEqualTwoColumns() {
			var name = Field.String("test1");
			var id = Field.BigInt(1);

			var result = table.SelectRowsEqual(1, name, 0, id);
			Assert.IsNotNull(result);
			Assert.IsNotEmpty(result);

			var index = result.First();
			Assert.AreEqual(0, index);
		}

		[Test]
		public void SelectGreater() {
			var id = Field.BigInt(1);

			var result = table.SelectRowsGreater(0, id);

			Assert.IsNotNull(result);
			Assert.IsNotEmpty(result);
			Assert.AreEqual(2, result.Count());

			Assert.AreEqual(1, result.First());
		}
	}
}
