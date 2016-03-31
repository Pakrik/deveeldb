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
using System.Runtime.Serialization;

using Deveel.Data.Security;
using Deveel.Data.Sql.Types;

namespace Deveel.Data.Sql.Statements {
	[Serializable]
	public sealed class CreateTypeStatement : SqlStatement {
		public CreateTypeStatement(ObjectName typeName, UserTypeMember[] members) 
			: this(typeName, members, false) {
		}

		public CreateTypeStatement(ObjectName typeName, UserTypeMember[] members, bool replaceIfExists) {
			if (typeName == null)
				throw new ArgumentNullException("typeName");
			if (members == null)
				throw new ArgumentNullException("members");

			if (members.Length == 0)
				throw new ArgumentException("At least one member must be specified.");

			TypeName = typeName;
			Members = members;
			ReplaceIfExists = replaceIfExists;
		}

		private CreateTypeStatement(SerializationInfo info, StreamingContext context)
			: base(info, context) {
			TypeName = (ObjectName)info.GetValue("TypeName", typeof(ObjectName));
			Members = (UserTypeMember[])info.GetValue("Members", typeof(UserTypeMember[]));
			ReplaceIfExists = info.GetBoolean("Replace");
		}

		public ObjectName TypeName { get; private set; }

		public bool ReplaceIfExists { get; set; }

		public UserTypeMember[] Members { get; private set; }

		protected override SqlStatement PrepareStatement(IRequest context) {
			var schemaName = context.Access.ResolveSchemaName(TypeName.ParentName);
			var typeName = new ObjectName(schemaName, TypeName.Name);

			return new CreateTypeStatement(typeName, Members, ReplaceIfExists);
		}

		protected override void ExecuteStatement(ExecutionContext context) {
			if (!context.User.CanCreateInSchema(TypeName.ParentName))
				throw new SecurityException(String.Format("The user '{0}' has no rights to create in schema '{1}'.",
					context.User.Name, TypeName.ParentName));

			if (context.DirectAccess.TypeExists(TypeName)) {
				if (!ReplaceIfExists)
					throw new StatementException(String.Format("The type '{0}' already exists.", TypeName));

				context.DirectAccess.DropType(TypeName);
			}

			// TODO: support the parent

			var typeInfo = new UserTypeInfo(TypeName);
			foreach (var member in Members) {
				typeInfo.AddMember(member);
			}

			typeInfo.Owner = context.User.Name;
			
			context.DirectAccess.CreateType(typeInfo);
			context.DirectAccess.GrantOn(DbObjectType.Type, TypeName, context.User.Name, Privileges.TableAll, true);
		}

		protected override void GetData(SerializationInfo info) {
			info.AddValue("TypeName", TypeName);
			info.AddValue("Members", Members);
			info.AddValue("Replace", ReplaceIfExists);
		}
	}
}