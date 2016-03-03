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
using System.Linq;

using Deveel.Data.Sql.Objects;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Transactions;
using Deveel.Data.Sql.Types;

namespace Deveel.Data.Sql.Sequences {
	/// <summary>
	/// A default implementation of a sequence manager that is backed by 
	/// a given transaction.
	/// </summary>
	/// <seealso cref="ISequenceManager"/>
	public class SequenceManager : ISequenceManager {
		/// <summary>
		/// A static TObject that represents numeric 1.
		/// </summary>
		private static readonly Field OneValue = Field.Integer(1);

		private Dictionary<ObjectName, Sequence> sequenceKeyMap; 

		/// <summary>
		/// Construct a new instance of <see cref="SequenceManager"/> that is backed by
		/// the given transaction factory.
		/// </summary>
		/// <param name="transaction"></param>
		public SequenceManager(ITransaction transaction) {
			Transaction = transaction;
			sequenceKeyMap = new Dictionary<ObjectName, Sequence>();
		}

		~SequenceManager() {
			Dispose(false);
		}

		DbObjectType IObjectManager.ObjectType {
			get { return DbObjectType.Sequence; }
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				if (sequenceKeyMap !=null)
					sequenceKeyMap.Clear();

				sequenceKeyMap = null;
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets the transaction where the manager is operating.
		/// </summary>
		private ITransaction Transaction { get; set; }

		public ITableContainer TableContainer {
			get { return new SequenceTableContainer(this); }
		}

		#region SequenceTableContainer

		class SequenceTableContainer : ITableContainer {
			private readonly ITransaction transaction;
			private readonly SequenceManager manager;

			public SequenceTableContainer(SequenceManager manager) {
				transaction = manager.Transaction;
				this.manager = manager;
			}

			public int TableCount {
				get {
					var table = transaction.GetTable(SystemSchema.SequenceTableName);
					return table != null ? table.RowCount : 0;
				}
			}

			private static TableInfo CreateTableInfo(ObjectName schema, string name) {
				var info = new TableInfo(new ObjectName(schema, name));
				info.AddColumn("last_value", PrimitiveTypes.Numeric());
				info.AddColumn("current_value", PrimitiveTypes.Numeric());
				info.AddColumn("top_value", PrimitiveTypes.Numeric());
				info.AddColumn("increment_by", PrimitiveTypes.Numeric());
				info.AddColumn("min_value", PrimitiveTypes.Numeric());
				info.AddColumn("max_value", PrimitiveTypes.Numeric());
				info.AddColumn("start", PrimitiveTypes.Numeric());
				info.AddColumn("cache", PrimitiveTypes.Numeric());
				info.AddColumn("cycle", PrimitiveTypes.Boolean());
				info = info.AsReadOnly();
				return info;
			}

			public int FindByName(ObjectName tableName) {
				if (tableName == null)
					throw new ArgumentNullException("tableName");

				if (tableName.Parent == null)
					return -1;

				var seqInfo = SystemSchema.SequenceInfoTableName;
				if (transaction.RealTableExists(seqInfo)) {
					// Search the table.
					var table = transaction.GetTable(seqInfo);
					var name = Field.VarChar(tableName.Name);
					var schema = Field.VarChar(tableName.Parent.FullName);

					int p = 0;
					foreach (var row in table) {
						var seqType = row.GetValue(3);
						if (!seqType.IsEqualTo(OneValue)) {
							var obName = row.GetValue(2);
							if (obName.IsEqualTo(name)) {
								var obSchema = row.GetValue(1);
								if (obSchema.IsEqualTo(schema)) {
									// Match so return this
									return p;
								}
							}
							++p;
						}
					}
				}

				return -1;
			}

			public ObjectName GetTableName(int offset) {
				var seqInfo = SystemSchema.SequenceInfoTableName;
				if (transaction.RealTableExists(seqInfo)) {
					var table = transaction.GetTable(seqInfo);
					int p = 0;
					foreach (var row in table) {
						var seqType = row.GetValue(3);
						if (!seqType.IsEqualTo(OneValue)) {
							if (offset == p) {
								var obSchema = row.GetValue(1);
								var obName = row.GetValue(2);
								return new ObjectName(ObjectName.Parse(obSchema.Value.ToString()), obName.Value.ToString());
							}
							++p;
						}
					}
				}

				throw new ArgumentOutOfRangeException("offset");
			}

			public TableInfo GetTableInfo(int offset) {
				var tableName = GetTableName(offset);
				return CreateTableInfo(tableName.Parent, tableName.Name);
			}

			public string GetTableType(int offset) {
				return TableTypes.Sequence;
			}

			public bool ContainsTable(ObjectName name) {
				var seqInfo = SystemSchema.SequenceInfoTableName;

				// This set can not contain the table that is backing it, so we always
				// return false for that.  This check stops an annoying recursive
				// situation for table name resolution.
				if (name.Equals(seqInfo))
					return false;

				return FindByName(name) != -1;
			}

			public ITable GetTable(int offset) {
				var table = transaction.GetTable(SystemSchema.SequenceInfoTableName);
				int p = 0;
				int rowIndex = -1;

				foreach (var row in table) {
					var seqType = row.GetValue(3);
					if (seqType.IsEqualTo(OneValue)) {
						if (p == offset) {
							rowIndex = row.RowId.RowNumber;
							break;
						}

						p++;
					}
				}

				if (rowIndex == -1)
					throw new ArgumentOutOfRangeException("offset");

				var seqId = table.GetValue(rowIndex, 0);
				var schema = ObjectName.Parse(table.GetValue(rowIndex, 1).Value.ToString());
				var name = table.GetValue(rowIndex, 2).Value.ToString();

				var tableName = new ObjectName(schema, name);

				// Find this id in the 'sequence' table
				var seqTable = transaction.GetTable(SystemSchema.SequenceTableName);

				var index = seqTable.GetIndex(0);
				var list = index.SelectEqual(seqId);

				if (!list.Any())
					throw new Exception("No SEQUENCE table entry for sequence.");

				int seqRowI = list.First();

				// Generate the DataTableInfo
				var tableInfo = CreateTableInfo(schema, name);

				// Last value for this sequence generated by the transaction
				Field lastValue;
				try {
					var sequence = manager.GetSequence(tableName);
					if (sequence == null)
						throw new ObjectNotFoundException(tableName);

					lastValue = Field.Number(sequence.GetCurrentValue());
				} catch (Exception) {
					lastValue = Field.BigInt(-1);
				}

				// The current value of the sequence generator
				var currentValue = Field.Number(manager.GetCurrentValue(tableName));

				// Read the rest of the values from the SEQUENCE table.
				var topValue = seqTable.GetValue(seqRowI, 1);
				var incrementBy = seqTable.GetValue(seqRowI, 2);
				var minValue = seqTable.GetValue(seqRowI, 3);
				var maxValue = seqTable.GetValue(seqRowI, 4);
				var start = seqTable.GetValue(seqRowI, 5);
				var cache = seqTable.GetValue(seqRowI, 6);
				var cycle = seqTable.GetValue(seqRowI, 7);


				return new SequenceTable(transaction.Database.Context, tableInfo) {
					TopValue = topValue,
					LastValue = lastValue,
					CurrentValue = currentValue,
					Increment = incrementBy,
					MinValue = minValue,
					MaxValue = maxValue,
					Start = start,
					Cache = cache,
					Cycle = cycle
				};
			}
		}

		#endregion

		#region SequenceTable

		class SequenceTable : GeneratedTable {
			private readonly TableInfo tableInfo;

			public SequenceTable(IDatabaseContext dbContext, TableInfo tableInfo) 
				: base(dbContext) {
				this.tableInfo = tableInfo;
			}

			public override TableInfo TableInfo {
				get { return tableInfo; }
			}

			public override int RowCount {
				get { return 1; }
			}

			public Field TopValue { get; set; }

			public Field LastValue { get; set; }

			public Field CurrentValue { get; set; }

			public Field Increment { get; set; }

			public Field MinValue { get; set; }

			public Field MaxValue { get; set; }

			public Field Start { get; set; }

			public Field Cache { get; set; }

			public Field Cycle { get; set; }

			public override Field GetValue(long rowNumber, int columnOffset) {
				if (rowNumber != 0)
					throw new ArgumentOutOfRangeException("rowNumber");

				switch (columnOffset) {
					case 0:
						return LastValue;
					case 1:
						return CurrentValue;
					case 2:
						return TopValue;
					case 3:
						return Increment;
					case 4:
						return MinValue;
					case 5:
						return MaxValue;
					case 6:
						return Start;
					case 7:
						return Cache;
					case 8:
						return Cycle;
					default:
						throw new ArgumentOutOfRangeException("columnOffset");
				}
			}
		}

		#endregion

		/// <summary>
		/// Updates the state of the sequence key in the sequence tables in the
		/// database.
		/// </summary>
		/// <param name="sequence"></param>
		/// <remarks>
		/// The update occurs on an independent transaction.
		/// </remarks>
		private void UpdateSequenceState(Sequence sequence) {
			// We need to update the sequence key state.

			// The sequence table
			var seq = Transaction.GetMutableTable(SystemSchema.SequenceTableName);

			// Find the row with the id for this sequence.
			var list = seq.SelectRowsEqual(0, Field.Number(sequence.Id)).ToList();

			// Checks
			var count = list.Count();
			if (count == 0)
				throw new ObjectNotFoundException(sequence.FullName);

			if (count > 1)
				throw new Exception("Assert failed: multiple id for sequence.");

			// Create the DataRow
			var dataRow = seq.GetRow(list.First());

			// Set the content of the row data
			dataRow.SetValue(0, Field.Number(sequence.Id));
			dataRow.SetValue(1, Field.Number(sequence.LastValue));
			dataRow.SetValue(2, Field.Number(sequence.SequenceInfo.Increment));
			dataRow.SetValue(3, Field.Number(sequence.SequenceInfo.MinValue));
			dataRow.SetValue(4, Field.Number(sequence.SequenceInfo.MaxValue));
			dataRow.SetValue(5, Field.Number(sequence.SequenceInfo.StartValue));
			dataRow.SetValue(6, Field.BigInt(sequence.SequenceInfo.Cache));
			dataRow.SetValue(7, Field.Boolean(sequence.SequenceInfo.Cycle));

			// Update the row
			seq.UpdateRow(dataRow);
		}

		public void Create() {
			// SYSTEM.SEQUENCE_INFO
			var tableInfo = new TableInfo(SystemSchema.SequenceInfoTableName);
			tableInfo.AddColumn("id", PrimitiveTypes.Numeric());
			tableInfo.AddColumn("schema", PrimitiveTypes.String());
			tableInfo.AddColumn("name", PrimitiveTypes.String());
			tableInfo.AddColumn("type", PrimitiveTypes.Numeric());
			tableInfo = tableInfo.AsReadOnly();
			Transaction.CreateTable(tableInfo);

			// SYSTEM.SEQUENCE
			tableInfo = new TableInfo(SystemSchema.SequenceTableName);
			tableInfo.AddColumn("seq_id", PrimitiveTypes.Numeric());
			tableInfo.AddColumn("last_value", PrimitiveTypes.Numeric());
			tableInfo.AddColumn("increment", PrimitiveTypes.Numeric());
			tableInfo.AddColumn("minvalue", PrimitiveTypes.Numeric());
			tableInfo.AddColumn("maxvalue", PrimitiveTypes.Numeric());
			tableInfo.AddColumn("start", PrimitiveTypes.Numeric());
			tableInfo.AddColumn("cache", PrimitiveTypes.Numeric());
			tableInfo.AddColumn("cycle", PrimitiveTypes.Boolean());
			tableInfo = tableInfo.AsReadOnly();
			Transaction.CreateTable(tableInfo);
		}

		void IObjectManager.CreateObject(IObjectInfo objInfo) {
			var seqInfo = objInfo as SequenceInfo;
			if (seqInfo == null)
				throw new ArgumentException();

			CreateSequence(seqInfo);
		}

		public ISequence CreateSequence(SequenceInfo sequenceInfo) {
			if (sequenceInfo == null)
				throw new ArgumentNullException("sequenceInfo");

			var sequenceName = sequenceInfo.SequenceName;
			
			// If the Sequence or SequenceInfo tables don't exist then 
			// We can't add or remove native tables
			if (sequenceName.Equals(SystemSchema.SequenceTableName) ||
				sequenceName.Equals(SystemSchema.SequenceInfoTableName) ||
				!Transaction.TableExists(SystemSchema.SequenceTableName) ||
				!Transaction.TableExists(SystemSchema.SequenceInfoTableName)) {
				return null;
			}

			if (sequenceInfo.Type == SequenceType.Native)
				return CreateNativeTableSequence(sequenceName);

			return CreateCustomSequence(sequenceName, sequenceInfo);
		}

		private Sequence CreateCustomSequence(ObjectName sequenceName, SequenceInfo sequenceInfo) {
			// The SEQUENCE and SEQUENCE_INFO table
			var seq = Transaction.GetMutableTable(SystemSchema.SequenceTableName);
			var seqi = Transaction.GetMutableTable(SystemSchema.SequenceInfoTableName);

			var list = seqi.SelectRowsEqual(2, Field.VarChar(sequenceName.Name), 1, Field.VarChar(sequenceName.Parent.FullName));
			if (list.Any())
				throw new Exception(String.Format("Sequence generator with name '{0}' already exists.", sequenceName));

			// Generate a unique id for the sequence info table
			var uniqueId = Transaction.NextTableId(SystemSchema.SequenceInfoTableName);

			// Insert the new row
			var dataRow = seqi.NewRow();
			dataRow.SetValue(0, Field.Number(uniqueId));
			dataRow.SetValue(1, Field.VarChar(sequenceName.Parent.FullName));
			dataRow.SetValue(2, Field.VarChar(sequenceName.Name));
			dataRow.SetValue(3, Field.BigInt(2));
			seqi.AddRow(dataRow);

			// Insert into the SEQUENCE table.
			dataRow = seq.NewRow();
			dataRow.SetValue(0, Field.Number(uniqueId));
			dataRow.SetValue(1, Field.Number(sequenceInfo.StartValue));
			dataRow.SetValue(2, Field.Number(sequenceInfo.Increment));
			dataRow.SetValue(3, Field.Number(sequenceInfo.MinValue));
			dataRow.SetValue(4, Field.Number(sequenceInfo.MaxValue));
			dataRow.SetValue(5, Field.Number(sequenceInfo.StartValue));
			dataRow.SetValue(6, Field.BigInt(sequenceInfo.Cache));
			dataRow.SetValue(7, Field.Boolean(sequenceInfo.Cycle));
			seq.AddRow(dataRow);

			return new Sequence(this, uniqueId, sequenceInfo);
		}

		private ISequence CreateNativeTableSequence(ObjectName tableName) {
			var table = Transaction.GetMutableTable(SystemSchema.SequenceInfoTableName);
			var uniqueId = Transaction.NextTableId(SystemSchema.SequenceInfoTableName);

			var dataRow = table.NewRow();
			dataRow.SetValue(0, Field.Number(uniqueId));
			dataRow.SetValue(1, Field.VarChar(tableName.Parent.FullName));
			dataRow.SetValue(2, Field.VarChar(tableName.Name));
			dataRow.SetValue(3, Field.BigInt(1));
			table.AddRow(dataRow);

			return new Sequence(this, uniqueId, SequenceInfo.Native(tableName));
		}

		bool IObjectManager.AlterObject(IObjectInfo objInfo) {
			throw new NotImplementedException();
		}

		bool IObjectManager.DropObject(ObjectName objName) {
			return DropSequence(objName);
		}

		public ObjectName ResolveName(ObjectName objName, bool ignoreCase) {
			throw new NotImplementedException();
		}

		public bool DropSequence(ObjectName sequenceName) {
			// If the Sequence or SequenceInfo tables don't exist then 
			// we can't create the sequence sequence
			if (!Transaction.ObjectExists(SystemSchema.SequenceTableName) ||
				!Transaction.ObjectExists(SystemSchema.SequenceInfoTableName)) {
				throw new Exception("System sequence tables do not exist.");
			}

			// Remove the table sequence (delete SEQUENCE_INFO and SEQUENCE entry)
			return RemoveNativeTableSequence(sequenceName);
		}

		private bool RemoveNativeTableSequence(ObjectName tableName) {
			// If the Sequence or SequenceInfo tables don't exist then 
			// We can't add or remove native tables
			if (tableName.Equals(SystemSchema.SequenceTableName) ||
				tableName.Equals(SystemSchema.SequenceInfoTableName) ||
				!Transaction.ObjectExists(SystemSchema.SequenceTableName) ||
				!Transaction.ObjectExists(SystemSchema.SequenceInfoTableName)) {
				return false;
			}

			// The SEQUENCE and SEQUENCE_INFO table
			var seq = Transaction.GetMutableTable(SystemSchema.SequenceTableName);
			var seqi = Transaction.GetMutableTable(SystemSchema.SequenceInfoTableName);

			var list = seqi.SelectRowsEqual(2, Field.VarChar(tableName.Name), 1, Field.VarChar(tableName.Parent.FullName));

			// Remove the corresponding entry in the SEQUENCE table
			foreach (var rowIndex in list) {
				var sid = seqi.GetValue(rowIndex, 0);
				var list2 = seq.SelectRowsEqual(0, sid);
				foreach (int rowIndex2 in list2) {
					// Remove entry from the sequence table.
					seq.RemoveRow(rowIndex2);
				}

				// Remove entry from the sequence info table
				seqi.RemoveRow(rowIndex);
			}

			return true;
		}

		bool IObjectManager.ObjectExists(ObjectName objName) {
			return SequenceExists(objName);
		}

		bool IObjectManager.RealObjectExists(ObjectName objName) {
			return SequenceExists(objName);
		}

		public bool SequenceExists(ObjectName sequenceName) {
			// If the Sequence or SequenceInfo tables don't exist then 
			// we can't create the sequence generator
			if (!Transaction.TableExists(SystemSchema.SequenceTableName) ||
				!Transaction.TableExists(SystemSchema.SequenceInfoTableName)) {
				throw new Exception("System sequence tables do not exist.");
			}

			// The SEQUENCE and SEQUENCE_INFO table
			var seq = Transaction.GetMutableTable(SystemSchema.SequenceTableName);
			var seqi = Transaction.GetMutableTable(SystemSchema.SequenceInfoTableName);

			return seqi.SelectRowsEqual(1, Field.VarChar(sequenceName.Parent.FullName), 2, Field.VarChar(sequenceName.Name)).Any();
		}


		private SqlNumber NextValue(ObjectName name) {
			lock (this) {
				var sequence = (Sequence)GetSequence(name);

				if (sequence.SequenceInfo.Type == SequenceType.Native)
					// Native generator
					return Transaction.NextTableId(name);

				// Custom sequence generator
				var currentVal = sequence.CurrentValue;

				// Increment the current value.
				sequence.IncrementCurrentValue();

				// Have we reached the current cached point?
				if (currentVal == sequence.LastValue) {
					// Increment the generator
					for (int i = 0; i < sequence.SequenceInfo.Cache; ++i) {
						sequence.IncrementLastValue();
					}

					// Update the state
					UpdateSequenceState(sequence);

				}

				return sequence.CurrentValue;
			}
		}

		private SqlNumber SetValue(ObjectName name, SqlNumber value) {
			lock (this) {
				var sequence = (Sequence) GetSequence(name);

				if (sequence.SequenceInfo.Type == SequenceType.Native)
					return Transaction.SetTableId(name, value);

				// Custom sequence generator
				sequence.CurrentValue = value;
				sequence.LastValue = value;

				// Update the state
				UpdateSequenceState(sequence);

				return value;
			}
		}

		private SqlNumber GetCurrentValue(ObjectName name) {
			lock (this) {
				var sequence = (Sequence) GetSequence(name);

				if (sequence.SequenceInfo.Type == SequenceType.Native)
					return Transaction.NextTableId(name);

				// Custom sequence generator
				return sequence.CurrentValue;
			}
		}

		IDbObject IObjectManager.GetObject(ObjectName objName) {
			return GetSequence(objName);
		}

		public ISequence GetSequence(ObjectName sequenceName) {
			// Is the generator already in the cache?
			Sequence sequence;

			if (!sequenceKeyMap.TryGetValue(sequenceName, out sequence)) {
				// This sequence generator is not in the cache so we need to query the
				// sequence table for this.
				var seqi = Transaction.GetTable(SystemSchema.SequenceInfoTableName);

				var schemaVal = Field.VarChar(sequenceName.Parent.FullName);
				var nameVal = Field.VarChar(sequenceName.Name);
				var list = seqi.SelectRowsEqual(2, nameVal, 1, schemaVal).ToList();

				if (list.Count == 0) {
					throw new ArgumentException(String.Format("Sequence '{0}' not found.", sequenceName));
				} else if (list.Count() > 1) {
					throw new Exception("Assert failed: multiple sequence keys with same name.");
				}

				int rowIndex = list.First();
				var sid = seqi.GetValue(rowIndex, 0);
				var sschema = seqi.GetValue(rowIndex, 1);
				var sname = seqi.GetValue(rowIndex, 2);
				var stype = seqi.GetValue(rowIndex, 3);

				// Is this a custom sequence generator?
				// (stype == 1) == true
				if (stype.IsEqualTo(OneValue)) {
					// Native generator.
					sequence = new Sequence(this, (SqlNumber) sid.Value, SequenceInfo.Native(sequenceName));
				} else {
					// Query the sequence table.
					var seq = Transaction.GetTable(SystemSchema.SequenceTableName);

					list = seq.SelectRowsEqual(0, sid).ToList();

					if (!list.Any())
						throw new Exception("Sequence table does not contain sequence information.");
					if (list.Count() > 1)
						throw new Exception("Sequence table contains multiple generators for id.");

					rowIndex = list.First();
					var lastValue = (SqlNumber) seq.GetValue(rowIndex, 1).Value;
					var increment = (SqlNumber) seq.GetValue(rowIndex, 2).Value;
					var minvalue = (SqlNumber) seq.GetValue(rowIndex, 3).Value;
					var maxvalue = (SqlNumber) seq.GetValue(rowIndex, 4).Value;
					var start = (SqlNumber) seq.GetValue(rowIndex, 5).Value;
					var cache = (long) seq.GetValue(rowIndex, 6).AsBigInt();
					bool cycle = seq.GetValue(rowIndex, 7).AsBoolean();

					var info = new SequenceInfo(sequenceName, start, increment, minvalue, maxvalue, cache, cycle);
					sequence = new Sequence(this, (SqlNumber) sid.Value, lastValue, info);

					// Put the generator in the cache
					sequenceKeyMap[sequenceName] = sequence;
				}

			}

			// Return the generator
			return sequence;

		}

		#region Sequence

		class Sequence : ISequence {
			private readonly SequenceManager manager;

			public Sequence(SequenceManager manager, SqlNumber id, SequenceInfo sequenceInfo) 
				: this(manager, id, SqlNumber.Null, sequenceInfo) {
			}

			public Sequence(SequenceManager manager, SqlNumber id, SqlNumber lastValue, SequenceInfo sequenceInfo) {
				this.manager = manager;
				Id = id;
				FullName = sequenceInfo.SequenceName;
				SequenceInfo = sequenceInfo;
				LastValue = lastValue;
				CurrentValue = lastValue;
			}

			public SqlNumber Id { get; private set; }

			public ObjectName FullName { get; private set; }

			DbObjectType IDbObject.ObjectType {
				get { return DbObjectType.Sequence; }
			}

			public SequenceInfo SequenceInfo { get; private set; }

			public SqlNumber LastValue { get; set; }

			public SqlNumber CurrentValue { get; set; }

			public SqlNumber GetCurrentValue() {
				return manager.GetCurrentValue(FullName);
			}

			public SqlNumber NextValue() {
				return manager.NextValue(FullName);
			}

			public SqlNumber SetValue(SqlNumber value) {
				return manager.SetValue(FullName, value);
			}

			private SqlNumber IncrementValue(SqlNumber val) {
				val += SequenceInfo.Increment;
				if (val > SequenceInfo.MaxValue) {
					if (SequenceInfo.Cycle) {
						val = SequenceInfo.MinValue;
					} else {
						throw new InvalidOperationException("Sequence out of bounds.");
					}
				}
				if (val < SequenceInfo.MinValue) {
					if (SequenceInfo.Cycle) {
						val = SequenceInfo.MaxValue;
					} else {
						throw new InvalidOperationException("Sequence out of bounds.");
					}
				}
				return val;
			}

			public void IncrementCurrentValue() {
				CurrentValue = IncrementValue(CurrentValue);
			}

			public void IncrementLastValue() {
				LastValue = IncrementValue(LastValue);
			}
		}

		#endregion
	}
}
