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

using Deveel.Data.Sql.Expressions;

namespace Deveel.Data.Sql {
	public sealed class JoiningSet {
		private readonly List<JoinPart> parts;

		internal JoiningSet() {
			parts = new List<JoinPart>();
		}

		public int PartCount {
			get { return parts.Count; }
		}

		public void AddJoin(JoinType joinType, ObjectName tableName, SqlExpression onExpression) {
			if (tableName == null) 
				throw new ArgumentNullException("tableName");

			parts.Add(new JoinPart(joinType, tableName, onExpression));
		}
	}
}