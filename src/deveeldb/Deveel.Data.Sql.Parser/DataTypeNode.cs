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
using System.Collections.Generic;
using System.Linq;

using Deveel.Data.Sql.Objects;
using Deveel.Data.Sql.Types;

namespace Deveel.Data.Sql.Parser {
	/// <summary>
	/// Describes the information of a data type as found in a SQL string.
	/// </summary>
	class DataTypeNode : SqlNode {
		/// <summary>
		/// Constructs an empty <see cref="DataTypeNode"/>.
		/// </summary>
		internal DataTypeNode() {
			IsPrimitive = true;
			Metadata = new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets a boolean value indicating if the data type found is a primitive.
		/// </summary>
		/// <seealso cref="SqlType.IsPrimitive"/>
		/// <seealso cref="PrimitiveTypes.IsPrimitive(Deveel.Data.Sql.Types.SqlTypeCode)"/>
		public bool IsPrimitive { get; private set; }

		/// <summary>
		/// Gets the name of the data type, if <see cref="IsPrimitive"/> is <c>true</c>.
		/// </summary>
		public string TypeName { get; private set; }

		/// <summary>
		/// Gets the size specification of the data-type.
		/// </summary>
		/// <remarks>
		/// In case the type is a <c>NUMERIC</c> type, this value is not indicated:
		/// rather check <see cref="Scale"/> property.
		/// </remarks>
		public int Size { get; private set; }

		/// <summary>
		/// Gets a value indicating if the data-type specification has
		/// a size value.
		/// </summary>
		/// <seealso cref="Size"/>
		public bool HasSize { get; private set; }

		/// <summary>
		/// Gets the scaling factor of a numeric data-type.
		/// </summary>
		/// <remarks>
		/// This value is present only if the data-type is <c>NUMERIC</c>.
		/// </remarks>
		/// <seealso cref="HasScale"/>
		/// <seealso cref="SqlNumber.Scale"/>
		/// <seealso cref="NumericType.Scale"/>
		public int Scale { get; private set; }

		/// <summary>
		/// Gets a value indicating if the data-type specification has
		/// a scaling factor value indicated.
		/// </summary>
		/// <seealso cref="Scale"/>
		public bool HasScale { get; private set; }

		/// <summary>
		/// Get the precision of a data-type that is <c>NUMERIC</c>.
		/// </summary>
		/// <seealso cref="NumericType"/>
		/// <seealso cref="HasPrecision"/>
		public int Precision { get; private set; }

		/// <summary>
		/// In case this data-type is a <c>NUMERIC</c> type, this gets
		/// a value indicating if a precision specification was defined.
		/// </summary>
		public bool HasPrecision { get; private set; }

		/// <summary>
		/// In case the data-type is a <c>STRING</c> type, this gets the
		/// locale string used to collate the values handled by the type.
		/// </summary>
		/// <seealso cref="StringType.Locale"/>
		/// <seealso cref="HasLocale"/>
		public string Locale { get; private set; }

		/// <summary>
		/// In case the data-type is a <c>STRING</c> type, this indicates if the
		/// data-type specification includes a locale definition.
		/// </summary>
		/// <seealso cref="Locale"/>
		public bool HasLocale { get; private set; }

		public string Encoding { get; private set; }

		public bool HasEncoding { get; private set; }

		public Dictionary<string, string> Metadata { get; private set; } 

		protected override ISqlNode OnChildNode(ISqlNode node) {
			if (node.NodeName == "identity_type") {
				TypeName = "IDENTITY";
			} else if (node.NodeName == "boolean_type") {
				GetBooleanType(node);
			} else if (node.NodeName == "decimal_type") {
				GetNumberType(node);
			} else if (node.NodeName == "character_type" ||
			           node.NodeName == "binary_type") {
				GetSizedType(node);
			} else if (node.NodeName == "date_type" ||
			           node.NodeName == "integer_type" ||
			           node.NodeName == "float_type") {
				GetSimpleType(node);
			} else if (node.NodeName == "interval_type") {
			} else if (node.NodeName == "user_type") {
				GetUserType(node);
			} else if (node.NodeName == "row_type") {

			}

			return base.OnChildNode(node);
		}

		private void GetBooleanType(ISqlNode node) {
			IsPrimitive = true;
			TypeName = "BOOLEAN";
		}

		private void GetUserType(ISqlNode node) {
			IsPrimitive = false;
			TypeName = node.FindNode<ObjectNameNode>().Name;
			var optMetaList = node.FindByName("user_type_meta_opt");
			if (optMetaList != null) {
				GetMeta(optMetaList.ChildNodes.FirstOrDefault());
			}
		}

		private void GetMeta(ISqlNode node) {
			if (node == null)
				return;

			var metas = node.FindNodes<DataTypeMetaNode>();
			foreach (var meta in metas) {
				Metadata[meta.Name] = meta.Value;
			}
		}

		private void GetSimpleType(ISqlNode node) {
			var type = node.ChildNodes.First();
			TypeName = type.NodeName;
		}

		private void GetSizedType(ISqlNode node) {
			foreach (var childNode in node.ChildNodes) {
				if (childNode.NodeName == "long_varchar") {
					TypeName = "LONG VARCHAR";
				} else if (childNode is SqlKeyNode) {
					TypeName = ((SqlKeyNode) childNode).Text;
				} else if (childNode.NodeName == "datatype_size") {
					GetDataSize(childNode);
				} else if (childNode.NodeName == "locale_opt") {
					GetLocale(childNode);
				} else if (childNode.NodeName == "encoding_opt") {
					GetEncoding(childNode);
				}
			}
		}

		private void GetLocale(ISqlNode node) {
			foreach (var childNode in node.ChildNodes) {
				if (childNode is StringLiteralNode) {
					Locale = ((StringLiteralNode) childNode).Value;
					HasLocale = true;
				}
			}
		}

		private void GetEncoding(ISqlNode node) {
			foreach (var childNode in node.ChildNodes) {
				if (childNode is StringLiteralNode) {
					Encoding = ((StringLiteralNode) childNode).Value;
					HasEncoding = true;
				}
			}
		}

		private void GetDataSize(ISqlNode node) {
			foreach (var childNode in node.ChildNodes) {
				if (childNode is IntegerLiteralNode) {
					Size = (int) ((IntegerLiteralNode) childNode).Value;
					HasSize = true;
				}
			}
		}

		private void GetNumberType(ISqlNode node) {
			foreach (var childNode in node.ChildNodes) {
				if (childNode is SqlKeyNode) {
					TypeName = ((SqlKeyNode) childNode).Text;
				} else if (childNode.NodeName == "number_precision") {
					GetNumberPrecision(childNode);
				}
			}

		}

		private void GetNumberPrecision(ISqlNode node) {
			foreach (var childNode in node.ChildNodes) {
				if (!HasScale) {
					Scale = (int) ((IntegerLiteralNode) childNode).Value;
					HasScale = true;
				} else {
					Precision = (int) ((IntegerLiteralNode) childNode).Value;
					HasPrecision = true;
				}
			}
		}
	}
}