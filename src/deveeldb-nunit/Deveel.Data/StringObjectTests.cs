﻿using System;

using Deveel.Data.Sql.Objects;
using Deveel.Data.Types;

using NUnit.Framework;

namespace Deveel.Data.Deveel.Data {
	[TestFixture]
	public class StringObjectTests {
		[Test]
		public void BasicVarChar_Create() {
			const string s = "Test string";
			var sObj = DataObject.VarChar(s);
			Assert.IsNotNull(sObj);
			Assert.IsInstanceOf<DataObject>(sObj);
			Assert.AreEqual(SqlTypeCode.VarChar, sObj.Type.SqlType);
			Assert.IsInstanceOf<SqlString>(sObj.Value);
			Assert.AreEqual(s, sObj.Value);
		}

		[Test]
		public void BasicVarChar_Compare() {
			const string s = "Test string";
			var sObj1 = DataObject.VarChar(s);
			var sObj2 = DataObject.VarChar(s);

			Assert.IsNotNull(sObj1);
			Assert.IsNotNull(sObj2);

			Assert.IsTrue(sObj1.IsComparableTo(sObj2));
			Assert.AreEqual(0, sObj1.CompareTo(sObj2));
		}
	}
}
