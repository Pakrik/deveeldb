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
using System.Linq;

using Deveel.Data;
using Deveel.Data.Sql;
using Deveel.Data.Sql.Expressions;

namespace Deveel.Data.Routines {
	public abstract class ProcedureInfo : RoutineInfo {
		public ProcedureInfo(ObjectName routineName, RoutineParameter[] parameters) 
			: base(routineName, parameters) {
		}

		public override RoutineType RoutineType {
			get { return RoutineType.Procedure; }
		}

		internal override bool MatchesInvoke(Invoke invoke, IRequest request) {
			if (invoke == null)
				return false;

			bool ignoreCase = true;
			if (request != null)
				ignoreCase = request.Query.IgnoreIdentifiersCase();

			if (!RoutineName.Equals(invoke.RoutineName, ignoreCase))
				return false;


			var inputParams = Parameters.Where(parameter => parameter.IsInput).ToList();
			if (invoke.Arguments.Length != inputParams.Count)
				return false;

			for (int i = 0; i < invoke.Arguments.Length; i++) {
				// TODO: support variable evaluation here? or evaluate parameters before reaching here?
				if (!invoke.Arguments[i].IsConstant())
					return false;

				var argType = invoke.Arguments[i].ReturnType(request, null);
				var paramType = Parameters[i].Type;

				// TODO: verify if this is assignable (castable) ...
				if (!paramType.IsComparable(argType))
					return false;
			}

			return true;
		}
	}
}