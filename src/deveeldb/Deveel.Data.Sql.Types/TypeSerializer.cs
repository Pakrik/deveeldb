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
using System.Globalization;
using System.IO;
using System.Text;

namespace Deveel.Data.Sql.Types {
	static class TypeSerializer {
		public static void SerializeTo(BinaryWriter writer, SqlType type) {
			writer.Write((byte) type.TypeCode);

			if (type.IsPrimitive) {
				if (type is NumericType) {
					var numericType = (NumericType) type;
					writer.Write(numericType.Precision);
					writer.Write(numericType.Scale);
				} else if (type is StringType) {
					var stringType = (StringType) type;
					writer.Write(stringType.MaxSize);

					if (stringType.Locale != null) {
						writer.Write((byte) 1);
						writer.Write(stringType.Locale.Name);
					} else {
						writer.Write((byte) 0);
					}
				} else if (type is BinaryType) {
					var binaryType = (BinaryType) type;

					writer.Write(binaryType.MaxSize);
				} else if (type is BooleanType ||
				           type is IntervalType ||
				           type is DateType ||
				           type is NullType) {
					// nothing to add to the SQL Type Code
				} else {
					throw new NotSupportedException(String.Format("The data type '{0}' cannot be serialized.", type.GetType().FullName));
				}
			} else if (type is UserType) {
				var userType = (UserType) type;
				writer.Write((byte) 1); // The code of custom type
				writer.Write(userType.FullName.FullName);
			} else if (type is QueryType) {
				// nothing to do for the Query Type here
			} else if (type is ArrayType) {
				var arrayType = (ArrayType) type;
				writer.Write(arrayType.Length);
			} else {
				throw new NotSupportedException();
			}
		}

		public static SqlType Deserialize(BinaryReader reader, ITypeResolver resolver) {

			var typeCode = (SqlTypeCode)reader.ReadByte();

			if (BooleanType.IsBooleanType(typeCode))
				return PrimitiveTypes.Boolean(typeCode);
			if (IntervalType.IsIntervalType(typeCode))
				return PrimitiveTypes.Interval(typeCode);
			if (DateType.IsDateType(typeCode))
				return PrimitiveTypes.DateTime(typeCode);

			if (StringType.IsStringType(typeCode)) {
				var maxSize = reader.ReadInt32();

				CultureInfo locale = null;
				var hasLocale = reader.ReadByte() == 1;
				if (hasLocale) {
					var name = reader.ReadString();
					locale = new CultureInfo(name);
				}

				// TODO: Get the encoding from the serialization...
				return PrimitiveTypes.String(typeCode, maxSize, Encoding.Unicode, locale);
			}

			if (NumericType.IsNumericType(typeCode)) {
				var size = reader.ReadInt32();
				var scale = reader.ReadInt32();

				return PrimitiveTypes.Numeric(typeCode, size, scale);
			}

			if (BinaryType.IsBinaryType(typeCode)) {
				var size = reader.ReadInt32();
				return PrimitiveTypes.Binary(typeCode, size);
			}

			if (typeCode == SqlTypeCode.Type) {
				// TODO:
			}

			if (typeCode == SqlTypeCode.QueryPlan)
				return new QueryType();

			if (typeCode == SqlTypeCode.Array) {
				var size = reader.ReadInt32();
				return new ArrayType(size);
			}

			if (typeCode == SqlTypeCode.Null)
				return PrimitiveTypes.Null();

			throw new NotSupportedException();			
		}

		public static SqlType Deserialize(Stream stream, ITypeResolver typeResolver) {
			var reader = new BinaryReader(stream, Encoding.Unicode);
			return Deserialize(reader, typeResolver);
		}
	}
}
