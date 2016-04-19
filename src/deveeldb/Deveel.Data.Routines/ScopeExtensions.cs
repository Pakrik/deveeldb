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

using Deveel.Data.Services;
using Deveel.Data.Sql;
using Deveel.Data.Sql.Tables;

namespace Deveel.Data.Routines {
	static class ScopeExtensions {
		public static void UseRoutines(this IScope systemScope) {
			systemScope.Bind<IObjectManager>()
				.To<RoutineManager>()
				.WithKey(DbObjectType.Routine)
				.InTransactionScope();

			systemScope.Bind<ITableCompositeSetupCallback>()
				.To<RoutinesInit>()
				.InQueryScope();

			systemScope.Bind<IDatabaseCreateCallback>()
				.To<RoutinesInit>()
				.InQueryScope();

			systemScope.Bind<IRoutineResolver>()
				.To<SystemFunctionsProvider>()
				.InDatabaseScope();

			systemScope.Bind<IRoutineResolver>()
				.To<RoutineManager>()
				.InTransactionScope();

			systemScope.Bind<ITableContainer>()
				.To<RoutinesTableContainer>()
				.InTransactionScope();
		}
	}
}