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

using Deveel.Data.DbSystem;
using Deveel.Data.Types;

namespace Deveel.Data.Sql.Query {
	[Serializable]
	public sealed class QueryReference {
		public QueryReference(ObjectName name, int level) {
			Level = level;
			Name = name;
		}

		public int Level { get; private set; }

		public ObjectName Name { get; private set; }

		public DataObject Value { get; private set; }

		public DataType ReturnType {
			get { return Value == null ? null : Value.Type; }
		}

		public void Evaluate(IVariableResolver resolver) {
			Value = resolver.Resolve(Name);
		}
	}
}