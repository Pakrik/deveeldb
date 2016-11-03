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
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace Deveel.Data.Sql {
	/// <summary>
	/// Describes the name of an object within a database.
	/// </summary>
	/// <remarks>
	/// The name of an object is composed by multiple parts, depending on the level
	/// of nesting of the object this name references.
	/// <para>
	/// For example, a reference to a table will be composed by the name of
	/// the schema and the name of the table: <c>Schema.Table</c>, while a reference
	/// to a column will be composed by the name of the schema, the name of the parent
	/// table and the name of the column itself: <c>Schema.Table.Column</c>.
	/// </para>
	/// <para>
	/// Depending on the xecution context, parts of the name can be omitted and will
	/// be resolved at run-time.
	/// </para>
	/// </remarks>
	[DebuggerDisplay("{FullName}")]
	[Serializable]
	public sealed class ObjectName : IEquatable<ObjectName>, IComparable<ObjectName>, ISerializable, ISqlFormattable {
		/// <summary>
		/// The special name used as a wild-card to indicate all the columns of a table
		/// must be referenced in a given context.
		/// </summary>
		public const string GlobName = "*";

		/// <summary>
		/// The character that separates a name from its parent or child.
		/// </summary>
		public const char Separator = '.';

		private readonly int hashCode;

		/// <summary>
		/// Constructs a name reference without a parent.
		/// </summary>
		/// <param name="name">The object name.</param>
		/// <remarks>
		/// <b>NOTE:</b> This constructor is intended to be handling a name
		/// with no parent: if the string provided as <paramref name="name"/>
		/// contains any <see cref="Separator"/> character, this will make the
		/// resolution to fail at run-time. User <see cref="Parse"/> method to
		/// obtain a reference tree.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// If <paramref name="name"/> is <c>null</c> or empty.
		/// </exception>
		/// <seealso cref="ObjectName(ObjectName, String)"/>
		public ObjectName(string name) 
			: this(null, name) {
		}

		/// <summary>
		/// Constructs a name reference with a given parent.
		/// </summary>
		/// <param name="parent">The parent reference of the one being constructed</param>
		/// <param name="name">The name of the object.</param>
		/// <exception cref="ArgumentNullException">
		/// If <paramref name="name"/> is <c>null</c> or empty.
		/// </exception>
		public ObjectName(ObjectName parent, string name) {
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");
			
			Name = name;
			Parent = parent;

			hashCode = ComputeHashCode();
		}

		private ObjectName(SerializationInfo info, StreamingContext context) {
			Name = info.GetString("Name");
			Parent = (ObjectName)info.GetValue("Parent", typeof(ObjectName));

			hashCode = ComputeHashCode();
		}

		/// <summary>
		/// Gets the parent reference of the current one, if any or <c>null</c> if none.
		/// </summary>
		public ObjectName Parent { get; private set; }

		public string ParentName {
			get { return Parent == null ? null : Parent.FullName; }
		}

		/// <summary>
		/// Gets the name of the object being referenced.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the full reference name formatted.
		/// </summary>
		/// <seealso cref="ToString()"/>
		public string FullName {
			get { return ToString(); }
		}

		/// <summary>
		/// Indicates if this reference equivales to <see cref="GlobName"/>.
		/// </summary>
		public bool IsGlob {
			get { return Name.Equals(GlobName); }
		}

		private int ComputeHashCode() {
			var code = Name.GetHashCode() ^ 5623;
			if (Parent != null)
				code ^= Parent.GetHashCode();

			return code;
		}

		/// <summary>
		/// Parses the given string into a <see cref="ObjectName"/> object.
		/// </summary>
		/// <param name="s">The string to parse</param>
		/// <returns>
		/// Returns an instance of <see cref="ObjectName"/> that is the result
		/// of parsing of the string given.
		/// </returns>
		/// <exception cref="FormatException">
		/// If the given input string is of an invalid format.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// If the given string is <c>null</c> or empty.
		/// </exception>
		public static ObjectName Parse(string s) {
			Exception error;
			ObjectName result;
			if (!TryParse(s, out result, out error))
				throw error;

			return result;
		}

		public static bool TryParse(string s, out ObjectName result) {
			Exception error;
			return TryParse(s, out result, out error);
		}

		private static bool TryParse(string s, out ObjectName result, out Exception error) {
			if (String.IsNullOrEmpty(s)) {
				error = new ArgumentNullException("s");
				result = null;
				return false;
			}

			var sp = s.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
			if (sp.Length == 0) {
				error = new FormatException("At least one part of the name must be provided");
				result = null;
				return false;
			}

			if (sp.Length == 1) {
				result = new ObjectName(sp[0]);
				error = null;
				return true;
			}

			ObjectName finalName = null;
			for (int i = 0; i < sp.Length; i++) {
				if (finalName == null) {
					finalName = new ObjectName(sp[i]);
				} else {
					finalName = new ObjectName(finalName, sp[i]);
				}
			}

			result = finalName;
			error = null;
			return true;
		}

		/// <summary>
		/// Creates a new reference to a table, given a schema and a table name.
		/// </summary>
		/// <param name="schemaName">The name of the schema that is the parent of
		/// the given table.</param>
		/// <param name="name">The name of the table to reference.</param>
		/// <returns>
		/// Returns an instance of <see cref="ObjectName"/> that references a table
		/// within the given schema.
		/// </returns>
		public static ObjectName ResolveSchema(string schemaName, string name) {
			var sb = new StringBuilder();
			if (!String.IsNullOrEmpty(schemaName))
				sb.Append(schemaName).Append('.');
			sb.Append(name);

			return Parse(sb.ToString());
		}

		/// <summary>
		/// Creates a reference what is the child of the current one.
		/// </summary>
		/// <param name="name">The name of the child rerefence.</param>
		/// <returns>
		/// Returns an istance of <see cref="ObjectName"/> that has this
		/// one as parent, and is named by the given <paramref name="name"/>.
		/// </returns>
		public ObjectName Child(string name) {
			return new ObjectName(this, name);
		}

		public ObjectName Child(ObjectName childName) {
			var baseName = this;
			ObjectName parent = childName.Parent;
			while (parent != null) {
				baseName = baseName.Child(parent.Name);
				parent = parent.Parent;
			}

			baseName = baseName.Child(childName.Name);
			return baseName;
		}

		/// <summary>
		/// Compares this instance of the object reference to a given one and
		/// returns a value indicating if the two instances equivales.
		/// </summary>
		/// <param name="other">The other object reference to compare.</param>
		/// <returns></returns>
		public int CompareTo(ObjectName other) {
			if (other == null)
				return -1;

			int v = 0;
			if (Parent != null)
				v = Parent.CompareTo(other.Parent);

			if (v == 0)
				v = String.Compare(Name, other.Name, StringComparison.Ordinal);

			return v;
		}

		public override string ToString() {
			var builder = new SqlStringBuilder();
			this.AppendTo(builder);
			return builder.ToString();
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
			info.AddValue("Name", Name);
			info.AddValue("Parent", Parent, typeof(ObjectName));
		}

		public override bool Equals(object obj) {
			var other = obj as ObjectName;
			if (other == null)
				return false;

			return Equals(other);
		}

		public bool Equals(ObjectName other) {
			return Equals(other, false);
		}

		/// <summary>
		/// Compares this object name with the other one given, according to the
		/// case sensitivity specified.
		/// </summary>
		/// <param name="other">The other <see cref="ObjectName"/> to compare.</param>
		/// <param name="ignoreCase">The specification to either ignore the case for comparison.</param>
		/// <returns>
		/// Returns <c>true</c> if the two instances are equal, according to the case sensitivity
		/// given, or <c>false</c> otherwise.
		/// </returns>
		public bool Equals(ObjectName other, bool ignoreCase) {
			if (other == null)
				return false;

			if (!ignoreCase)
				return hashCode == other.hashCode;

			if (Parent != null && other.Parent == null)
				return false;
			if (Parent == null && other.Parent != null)
				return false;

			if (Parent != null && !Parent.Equals(other.Parent, true))
				return false;

			var comparison = StringComparison.OrdinalIgnoreCase;
			return String.Equals(Name, other.Name, comparison);
		}

		public override int GetHashCode() {
			return hashCode;
		}

		void ISqlFormattable.AppendTo(SqlStringBuilder builder) {
			if (Parent != null) {
				Parent.AppendTo(builder);
				builder.Append('.');
			}

			builder.Append(Name);
		}
	}
}