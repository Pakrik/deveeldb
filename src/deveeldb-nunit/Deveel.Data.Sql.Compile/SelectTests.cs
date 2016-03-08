﻿using System;
using System.Linq;

using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Statements;

using NUnit.Framework;

namespace Deveel.Data.Sql.Compile {
	[TestFixture]
	public sealed class SelectTests : SqlCompileTestBase {
		[Test]
		public void WithFromClause() {
			const string sql = "SELECT col1 AS a FROM table";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);

			Assert.AreEqual(1, result.Statements.Count);

			var statement = result.Statements.FirstOrDefault();

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<SelectStatement>(statement);

			var selectStatement = (SelectStatement)statement;
			Assert.IsNotNull(selectStatement.QueryExpression);
			Assert.IsNull(selectStatement.OrderBy);
		}

		[Test]
		public void WithVariable() {
			const string sql = "SELECT :a";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);

			Assert.AreEqual(1, result.Statements.Count);

			var statement = result.Statements.FirstOrDefault();

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<SelectStatement>(statement);

			var selectStatement = (SelectStatement)statement;
			Assert.IsNotNull(selectStatement.QueryExpression);
			Assert.IsNull(selectStatement.OrderBy);

			Assert.IsNotNull(selectStatement.QueryExpression.SelectColumns);

			var selectCols = selectStatement.QueryExpression.SelectColumns.ToList();
			Assert.AreEqual(1, selectCols.Count);
			Assert.IsInstanceOf<SqlVariableReferenceExpression>(selectCols[0].Expression);
		}

		[Test]
		public void WithFunction() {
			const string sql = "SELECT user()";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);

			Assert.AreEqual(1, result.Statements.Count);

			var statement = result.Statements.FirstOrDefault();

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<SelectStatement>(statement);

			var selectStatement = (SelectStatement)statement;
			Assert.IsNotNull(selectStatement.QueryExpression);
			Assert.IsNull(selectStatement.OrderBy);

			Assert.IsNotNull(selectStatement.QueryExpression.SelectColumns);

			var selectCols = selectStatement.QueryExpression.SelectColumns.ToList();
			Assert.AreEqual(1, selectCols.Count);
			Assert.IsInstanceOf<SqlFunctionCallExpression>(selectCols[0].Expression);
		}

		[Test]
		public void WithOrderByClause() {
			const string sql = "SELECT col1 AS a FROM table ORDER BY a ASC";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);

			Assert.AreEqual(1, result.Statements.Count);

			var statement = result.Statements.FirstOrDefault();

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<SelectStatement>(statement);

			var selectStatement = (SelectStatement)statement;
			Assert.IsNotNull(selectStatement.QueryExpression);
			Assert.IsNotNull(selectStatement.OrderBy);
		}

		[Test]
		public void CountAll() {
			const string sql = "SELECT COUNT(*) FROM table";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);

			Assert.AreEqual(1, result.Statements.Count);

			var statement = result.Statements.FirstOrDefault();

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<SelectStatement>(statement);

			var selectStatement = (SelectStatement)statement;
			Assert.IsNotNull(selectStatement.QueryExpression);

			var queryExpression = selectStatement.QueryExpression;

			Assert.IsNotNull(queryExpression.SelectColumns);
			Assert.IsNotEmpty(queryExpression.SelectColumns);

			var item = queryExpression.SelectColumns.First();
			
			Assert.IsNotNull(item);
			Assert.IsInstanceOf<SqlFunctionCallExpression>(item.Expression);

			var funcCall = (SqlFunctionCallExpression) item.Expression;
			Assert.IsNotEmpty(funcCall.Arguments);
			Assert.AreEqual(1, funcCall.Arguments.Length);
			Assert.IsInstanceOf<SqlReferenceExpression>(funcCall.Arguments[0]);
		}

		[Test]
		public void FromGlobbedTable() {
			const string sql = "SELECT table.* FROM table";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);

			Assert.AreEqual(1, result.Statements.Count);

			var statement = result.Statements.FirstOrDefault();

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<SelectStatement>(statement);

			var selectStatement = (SelectStatement)statement;
			Assert.IsNotNull(selectStatement.QueryExpression);

			var queryExpression = selectStatement.QueryExpression;
			
			Assert.IsNotNull(queryExpression.SelectColumns);
			Assert.IsNotEmpty(queryExpression.SelectColumns);

			var item = queryExpression.SelectColumns.First();

			Assert.IsNotNull(item);
			Assert.IsNotNull(item.ReferenceName);
			Assert.IsTrue(item.IsGlob);
			Assert.AreEqual("table", item.TableName.FullName);
		}
	}
}
