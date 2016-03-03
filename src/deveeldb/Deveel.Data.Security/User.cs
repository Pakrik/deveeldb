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
using System.Collections.Generic;

using Deveel.Data;
using Deveel.Data.Sql;

namespace Deveel.Data.Security {
	/// <summary>
	/// Provides the information for a user in a database system
	/// </summary>
	public sealed class User {
		public static readonly User System = new User(SystemName);
		public static readonly User Public = new User(PublicName);

		private Dictionary<ObjectName, Privileges> grantCache;

		/// <summary>
		/// Constructs a new user with the given name.
		/// </summary>
		/// <param name="name"></param>
		internal User(string name) {
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			Name = name;
		}

		/// <summary>
		/// The name of the <c>PUBLIC</c> special user.
		/// </summary>
		public const string PublicName = "@PUBLIC";

		/// <summary>
		/// The name of the <c>SYSTEM</c> special user.
		/// </summary>
		public const string SystemName = "@SYSTEM";

		/// <summary>
		/// Gets the name that uniquely identify a user within a database system.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets a boolean value indicating if this user represents the
		/// <c>SYSTEM</c> special user.
		/// </summary>
		/// <seealso cref="SystemName"/>
		public bool IsSystem {
			get { return Name.Equals(SystemName); }
		}

		/// <summary>
		/// Gets a boolean value indicating if this user represents the
		/// <c>PUBLIC</c> special user.
		/// </summary>
		public bool IsPublic {
			get { return Name.Equals(PublicName); }
		}
	}
}