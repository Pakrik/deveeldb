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

namespace Deveel.Data.Store {
	public sealed class LocalFileSystem : IFileSystem {
		public bool FileExists(string path) {
			return File.Exists(path);
		}

		IFile IFileSystem.OpenFile(string path, bool readOnly) {
			return OpenFile(path, readOnly);
		}

		public LocalFile OpenFile(string fileName, bool readOnly) {
			if (!FileExists(fileName))
				throw new IOException(string.Format("The file '{0}' does not exist: cannot be opened", fileName));

			var access = readOnly ? FileAccess.Read : FileAccess.ReadWrite;
			var stream = File.Open(fileName, FileMode.Open, access);
			return new LocalFile(fileName, stream, readOnly);
		}

		IFile IFileSystem.CreateFile(string path) {
			return CreateFile(path);
		}

		public LocalFile CreateFile(string fileName) {
			if (String.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("fileName");

			if (FileExists(fileName))
				throw new IOException(string.Format("The file '{0}' already exists: cannot create.", fileName));

			var stream = File.Create(fileName, 2048, FileOptions.WriteThrough);
			return new LocalFile(fileName, stream, false);
		}

		public bool DeleteFile(string path) {
			File.Delete(path);
			return !FileExists(path);
		}

		public string CombinePath(string path1, string path2) {
			return Path.Combine(path1, path2);
		}

		public bool RenameFile(string sourcePath, string destPath) {
			File.Move(sourcePath, destPath);
			return File.Exists(destPath);
		}

		public bool DirectoryExists(string path) {
			return Directory.Exists(path);
		}

		public void CreateDirectory(string path) {
			Directory.CreateDirectory(path);
		}

		public long GetFileSize(string path) {
			return new FileInfo(path).Length;
		}
	}
}
