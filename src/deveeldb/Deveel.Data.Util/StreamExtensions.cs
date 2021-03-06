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
using System.IO;

namespace Deveel.Data.Util {
	public static class StreamExtensions {
		public static void CopyTo(this Stream source, Stream destination) {
			CopyTo(source, destination, 2048);
		}

		public static void CopyTo(this Stream source, Stream destination, int bufferSize) {
			if (source == null)
				throw new ArgumentNullException("source");
			if (destination == null)
				throw new ArgumentNullException("destination");

			if (!source.CanRead)
				throw new ArgumentException("The source stream cannot be read.");
			if (!destination.CanWrite)
				throw new ArgumentException("The destination stream cannot be write");

			var buffer = new byte[bufferSize];
			int readCount;

			while ((readCount = source.Read(buffer, 0, bufferSize)) != 0) {
				destination.Write(buffer, 0, readCount);
			}
		}
	}
}
