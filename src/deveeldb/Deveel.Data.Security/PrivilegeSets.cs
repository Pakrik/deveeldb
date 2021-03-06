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

namespace Deveel.Data.Security {
	public static class PrivilegeSets {
		public static readonly Privileges TableAll = Privileges.Select |
		                                             Privileges.Update |
		                                             Privileges.Delete |
		                                             Privileges.Insert |
		                                             Privileges.References |
		                                             Privileges.Usage | Privileges.Compact;

		public static readonly Privileges TableRead = Privileges.Select | Privileges.Usage;

		public static readonly Privileges SchemaAll = Privileges.Create |
		                                              Privileges.Alter |
		                                              Privileges.Drop |
		                                              Privileges.List;

		public static readonly Privileges SchemaRead = Privileges.List;

		public static readonly Privileges RoutineAll = Privileges.Drop | Privileges.Execute;
	}
}
