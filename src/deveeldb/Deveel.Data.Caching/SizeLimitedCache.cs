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

namespace Deveel.Data.Caching {
	public class SizeLimitedCache : MemoryCache {
		public SizeLimitedCache(int maxSize) {
			MaxSize = maxSize;
		}

		public int MaxSize { get; private set; }

		protected override void CheckClean() {
			// If we have reached maximum cache size, remove some elements from the
			// end of the list
			if (NodeCount >= MaxSize) {
				Clean();
			}
		}

		protected override void UpdateElementAccess(object key, CacheValue cacheValue) {
			base.UpdateElementAccess(key, cacheValue);

			while (IndexList.Count > MaxSize) {
				RemoveUnlocked(IndexList.Last.Value.Key);
			}
		}
	}
}
