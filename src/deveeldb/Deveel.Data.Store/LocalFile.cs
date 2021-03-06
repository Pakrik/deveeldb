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
	public sealed class LocalFile : IFile {
		private System.IO.FileStream fileStream;

		internal LocalFile(string fileName, System.IO.FileStream fileStream, bool readOnly) {
			if (String.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("fileName");

			if (fileStream == null)
				throw new ArgumentNullException("fileStream");

			this.fileStream = fileStream;
			FileName = fileName;
			IsReadOnly = readOnly;
		}

		~LocalFile() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				if (fileStream != null)
					fileStream.Dispose();
			}

			fileStream = null;
		}

		public string FileName {get; private set; }

		public bool IsReadOnly { get; private set; }

		public long Position {
			get { return fileStream.Position; }
		}

		public long Length {
			get { return fileStream.Length; }
		}

		public bool Exists {
			get { return File.Exists(FileName); }
		}

		public long Seek(long offset, SeekOrigin origin) {
			return fileStream.Seek(offset, origin);
		}

		public void SetLength(long value) {
			fileStream.SetLength(value);
		}

		public int Read(byte[] buffer, int offset, int length) {
			return fileStream.Read(buffer, offset, length);
		}

		public void Write(byte[] buffer, int offset, int length) {
			fileStream.Write(buffer, offset, length);
		}

		public void Flush(bool writeThrough) {
			fileStream.Flush();
		}

		public void Close() {
			fileStream.Close();
		}

		public void Delete() {
			try {
				File.Delete(FileName);
			} finally {
				if (fileStream != null)
					fileStream.Dispose();

				fileStream = null;
			}
			
		}
	}
}
