﻿using System;
using System.IO;

using NUnit.Framework;

namespace Deveel.Data.Serialization {
	[TestFixture]
	public abstract class SerializationTestBase : ContextBasedTest {
		protected T BinaryDeserialize<T>(Stream stream) where T : class {
			var serializer = new BinarySerializer();
			return (T) serializer.Deserialize(stream);
		}

		protected Stream Serialize<T>(T obj) where T : class {
			var stream = new MemoryStream();
			var serializer = new BinarySerializer();
			serializer.Serialize(stream, obj);
			return stream;
		}

		protected void SerializeAndAssert<T>(T obj, Action<T, T> assert) where T : class {
			var stream = Serialize(obj);

			stream.Seek(0, SeekOrigin.Begin);

			var graph = BinaryDeserialize<T>(stream);
			assert(obj, graph);
		}
	}
}
