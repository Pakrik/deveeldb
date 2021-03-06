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
using System.Linq;

using Deveel.Data.Routines;
using Deveel.Data.Security;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Types;

namespace Deveel.Data.Sql.Statements {
	[Serializable]
	public sealed class CreateFunctionStatement : SqlStatement {
		public CreateFunctionStatement(ObjectName functionName, SqlType returnType, SqlStatement body) 
			: this(functionName, returnType, null, body) {
		}

		public CreateFunctionStatement(ObjectName functionName, SqlType returnType, RoutineParameter[] parameters, SqlStatement body) {
			if (functionName == null)
				throw new ArgumentNullException("functionName");
			if (returnType == null)
				throw new ArgumentNullException("returnType");
			if (body == null)
				throw new ArgumentNullException("body");

			FunctionName = functionName;
			ReturnType = returnType;
			Parameters = parameters;
			Body = body;
		}

		public ObjectName FunctionName { get; private set; }

		public SqlType ReturnType { get; private set; }

		public RoutineParameter[] Parameters { get; set; }

		public bool ReplaceIfExists { get; set; }

		public SqlStatement Body { get; set; }

		protected override void ConfigureSecurity(ExecutionContext context) {
			// TODO: scan the body and find other assertions
			context.Assertions.AddCreate(FunctionName, DbObjectType.Routine);
			context.Actions.AddResourceGrant(FunctionName, DbObjectType.Routine, PrivilegeSets.RoutineAll);
		}

		protected override SqlStatement PrepareExpressions(IExpressionPreparer preparer) {
			var body = (PlSqlBlockStatement) Body.Prepare(preparer);
			return new CreateFunctionStatement(FunctionName, ReturnType, Parameters, body) {
				ReplaceIfExists = ReplaceIfExists
			};
		}

		protected override SqlStatement PrepareStatement(IRequest context) {
			var schemaName = context.Access().ResolveSchemaName(FunctionName.ParentName);
			var functionName = new ObjectName(schemaName, FunctionName.Name);
			var returnType = ReturnType.Resolve(context);

			var parameters = new List<RoutineParameter>();
			if (Parameters != null) {
				foreach (var parameter in Parameters) {
					parameters.Add((RoutineParameter)((IStatementPreparable)parameter).Prepare(context));
				}
			}

			var body = (PlSqlBlockStatement) Body.Prepare(context);
			return new CreateFunctionStatement(functionName, returnType, parameters.ToArray(), body) {
				ReplaceIfExists = ReplaceIfExists
			};
		}

		protected override void ExecuteStatement(ExecutionContext context) {
			//if (!context.User.CanCreateInSchema(FunctionName.ParentName))
			//	throw new SecurityException();

			if (context.DirectAccess.RoutineExists(FunctionName)) {
				if (!ReplaceIfExists)
					throw new StatementException(String.Format("A routine named '{0}' already exists in the database.", FunctionName));

				context.DirectAccess.DeleteRoutine(FunctionName);
			}

			var parameters = new RoutineParameter[0];
			if (Parameters != null)
				parameters = Parameters.ToArray();

			var functionInfo = new PlSqlFunctionInfo(FunctionName, parameters, ReturnType, Body) {
				Owner = context.User.Name
			};

			context.DirectAccess.CreateRoutine(functionInfo);
			//context.DirectAccess.GrantOn(DbObjectType.Routine, FunctionName, context.User.Name, Privileges.Execute, true);
		}

		protected override void AppendTo(SqlStringBuilder builder) {
			var orReplace = ReplaceIfExists ? "OR REPLACE" : "";
			builder.AppendFormat("CREATE {0}FUNCTION ", orReplace);
			FunctionName.AppendTo(builder);

			builder.Append("(");
			if (Parameters != null && Parameters.Length > 0) {
				for (int i = 0; i < Parameters.Length; i++) {
					Parameters[i].AppendTo(builder);

					if (i < Parameters.Length - 1)
						builder.Append(", ");
				}
			}

			builder.Append(")");

			builder.Append(" RETURN ");
			ReturnType.AppendTo(builder);
			builder.AppendLine(" IS");

			builder.Indent();

			Body.AppendTo(builder);

			builder.DeIndent();
		}
	}
}
