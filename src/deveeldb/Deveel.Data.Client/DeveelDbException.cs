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
using System.Data;
using System.Data.Common;

namespace Deveel.Data.Client {
	public class DeveelDbException : DbException {
		public DeveelDbException()
			: this(null) {
		}

		public DeveelDbException(string message)
			: this(message, null) {
		}

		public DeveelDbException(string message, Exception innerException)
			: base(message, innerException) {
		}

		public override int ErrorCode {
			get {
				if (InnerException is DeveelDbException)
					return ((DeveelDbException) InnerException).ErrorCode;

				return base.ErrorCode;
			}
		}
	}
}
