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
using System.Linq;

using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Statements;

using NUnit.Framework;

namespace Deveel.Data.Sql.Compile {
	[TestFixture]
	public sealed class AssignVariableTests : SqlCompileTestBase {
		[Test]
		public void MarkedVariableToConstant() {
			const string sql = ":a := 23";

			var result = Compile(sql);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);

			Assert.AreEqual(1, result.Statements.Count);

			var statement = result.Statements.ElementAt(0);

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<AssignVariableStatement>(statement);

			var assignVar = (AssignVariableStatement) statement;

			Assert.IsInstanceOf<SqlVariableReferenceExpression>(assignVar.VariableReference);
			Assert.IsInstanceOf<SqlConstantExpression>(assignVar.ValueExpression);
		}


		[Test]
		public void UnmarkedVariableToConstant() {
			const string sql = "a := 8779.90";

			var result = Compile(sql);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);

			Assert.AreEqual(1, result.Statements.Count);

			var statement = result.Statements.ElementAt(0);

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<AssignVariableStatement>(statement);

			var assignVar = (AssignVariableStatement)statement;

			Assert.IsInstanceOf<SqlVariableReferenceExpression>(assignVar.VariableReference);
			Assert.IsInstanceOf<SqlConstantExpression>(assignVar.ValueExpression);
		}
	}
}