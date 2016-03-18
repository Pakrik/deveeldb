﻿// 
//  Copyright 2010-2015 Deveel
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

namespace Deveel.Data.Security {
	class SecurityModule : ISystemModule {
		public string ModuleName {
			get { return "Security Management"; }
		}

		public string Version {
			get { return "2.0"; }
		}

		public void Register(IScope systemScope) {
			systemScope.Bind<IUserManager>()
				.To<UserManager>()
				.InSessionScope();

			systemScope.Bind<IPrivilegeManager>()
				.To<PrivilegeManager>()
				.InSessionScope();

			systemScope.Bind<IUserIdentifier>()
				.To<ClearTextUserIdentifier>();

#if !PCL
			systemScope.Bind<IUserIdentifier>()
				.To<Pkcs12UserIdentifier>();
#endif
		}
	}
}
