﻿using System;
using System.IO;
using System.Text;

using Deveel.Math;

namespace Deveel.Data.Functions {
	internal class MiscFunctionFactory : FunctionFactory {
		public override void Init() {
			// Casting functions
			AddFunction("tonumber", typeof(ToNumberFunction));
			AddFunction("sql_cast", typeof(SQLCastFunction));
			// Security
			AddFunction("user", typeof(UserFunction), FunctionType.StateBased);
			AddFunction("privgroups", typeof(PrivGroupsFunction), FunctionType.StateBased);
			// Sequence operations
			AddFunction("uniquekey", typeof(UniqueKeyFunction), FunctionType.StateBased);
			AddFunction("nextval", typeof(NextValFunction), FunctionType.StateBased);
			AddFunction("currval", typeof(CurrValFunction), FunctionType.StateBased);
			AddFunction("setval", typeof(SetValFunction), FunctionType.StateBased);
			// Misc
			AddFunction("hextobinary", typeof(HexToBinaryFunction));
			AddFunction("binarytohex", typeof(BinaryToHexFunction));
			// Lists
			AddFunction("least", typeof(LeastFunction));
			AddFunction("greatest", typeof(GreatestFunction));
			// Branch
			AddFunction("if", typeof(IfFunction));
			AddFunction("coalesce", typeof(CoalesceFunction));

			// identity
			AddFunction("identity", typeof(IdentityFunction), FunctionType.StateBased);

			AddFunction("version", typeof(VersionFunction));
		}

		#region ToNumberFunction

		// Casts the expression to a BigDecimal number.  Useful in conjunction with
		// 'dateob'
		[Serializable]
		class ToNumberFunction : Function {
			public ToNumberFunction(Expression[] parameters)
				: base("tonumber", parameters) {

				if (ParameterCount != 1)
					throw new Exception("TONUMBER function must have one argument.");
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				// Casts the first parameter to a number
				return this[0].Evaluate(group, resolver, context).CastTo(TType.NumericType);
			}

		}

		#endregion

		#region IfFunction

		// Conditional - IF(a < 0, NULL, a)
		[Serializable]
		class IfFunction : Function {
			public IfFunction(Expression[] parameters)
				: base("if", parameters) {
				if (ParameterCount != 3) {
					throw new Exception(
						"IF function must have exactly three arguments.");
				}
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				TObject res = this[0].Evaluate(group, resolver, context);
				if (res.TType is TBooleanType) {
					// Does the result equal true?
					if (res.CompareTo(TObject.GetBoolean(true)) == 0) {
						// Resolved to true so evaluate the first argument
						return this[1].Evaluate(group, resolver, context);
					} else {
						// Otherwise result must evaluate to NULL or false, so evaluate
						// the second parameter
						return this[2].Evaluate(group, resolver, context);
					}
				}
				// Result was not a bool so return null
				return TObject.Null;
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				// It's impossible to know the return type of this function until runtime
				// because either comparator could be returned.  We could assume that
				// both branch expressions result in the same type of object but this
				// currently is not enforced.

				// Returns type of first argument
				TType t1 = this[1].ReturnTType(resolver, context);
				// This is a hack for null values.  If the first parameter is null
				// then return the type of the second parameter which hopefully isn't
				// also null.
				if (t1 is TNullType) {
					return this[2].ReturnTType(resolver, context);
				}
				return t1;
			}
		}

		#endregion

		#region IdentityFunction

		sealed class IdentityFunction : Function {
			public IdentityFunction(Expression[] parameters)
				: base("identity", parameters) {
			}
			
			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				string table_name = this[0].Evaluate(group, resolver, context);
				long v = -1;
				try {
					context.CurrentSequenceValue(table_name);
				} catch (StatementException) {
					if (context is DatabaseQueryContext) {
						v = ((DatabaseQueryContext)context).Connection.CurrentUniqueID(table_name);
					} else {
						throw new InvalidOperationException();
					}
				}

				if (v == -1)
					throw new InvalidOperationException("Unable to determine the sequence of the table " + table_name);

				return TObject.GetInt8(v);
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return TType.NumericType;
			}
		}


		#endregion

		#region UserFunction

		// Returns the user name
		[Serializable]
		class UserFunction : Function {
			public UserFunction(Expression[] parameters)
				: base("user", parameters) {

				if (ParameterCount > 0) {
					throw new Exception("'user' function must have no arguments.");
				}
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver,
											 IQueryContext context) {
				return TObject.GetString(context.UserName);
			}

			protected override TType ReturnTType() {
				return TType.StringType;
			}
		}

		#endregion

		#region PrivGroupsFunction

		// Returns the comma (",") deliminated priv groups the user belongs to.
		[Serializable]
		class PrivGroupsFunction : Function {
			public PrivGroupsFunction(Expression[] parameters)
				: base("privgroups", parameters) {

				if (ParameterCount > 0) {
					throw new Exception("'privgroups' function must have no arguments.");
				}
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				throw new Exception("'PrivGroups' function currently not working.");
			}

			protected override TType ReturnTType() {
				return TType.StringType;
			}

		}

		#endregion

		#region BinaryToHexFunction

		[Serializable]
		class BinaryToHexFunction : Function {

			readonly static char[] digits = {
		                                	'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		                                	'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
		                                	'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
		                                	'u', 'v', 'w', 'x', 'y', 'z'
		                                };

			public BinaryToHexFunction(Expression[] parameters)
				: base("binarytohex", parameters) {

				// One parameter - our hex string.
				if (ParameterCount != 1) {
					throw new Exception(
						"'binarytohex' function must have only 1 argument.");
				}
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver,
											 IQueryContext context) {
				TObject ob = this[0].Evaluate(group, resolver, context);
				if (ob.IsNull) {
					return ob;
				} else if (ob.TType is TBinaryType) {
					StringBuilder buf = new StringBuilder();
					IBlobAccessor blob = (IBlobAccessor)ob.Object;
					Stream bin = blob.GetInputStream();
					try {
						int bval = bin.ReadByte();
						while (bval != -1) {
							//TODO: check if this is correct...
							buf.Append(digits[((bval >> 4) & 0x0F)]);
							buf.Append(digits[(bval & 0x0F)]);
							bval = bin.ReadByte();
						}
					} catch (IOException e) {
						Console.Error.WriteLine(e.Message);
						Console.Error.WriteLine(e.StackTrace);
						throw new Exception("IO ApplicationException: " + e.Message);
					}

					return TObject.GetString(buf.ToString());
				} else {
					throw new Exception("'binarytohex' parameter type is not a binary object.");
				}

			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return TType.StringType;
			}

		}

		#endregion

		#region HexToBinaryFunction

		[Serializable]
		class HexToBinaryFunction : Function {
			public HexToBinaryFunction(Expression[] parameters)
				: base("hextobinary", parameters) {

				// One parameter - our hex string.
				if (ParameterCount != 1)
					throw new Exception("'hextobinary' function must have only 1 argument.");
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				String str = this[0].Evaluate(group, resolver, context).Object.ToString();

				int str_len = str.Length;
				if (str_len == 0) {
					return new TObject(TType.BinaryType, new ByteLongObject(new byte[0]));
				}
				// We translate the string to a byte array,
				byte[] buf = new byte[(str_len + 1) / 2];
				int index = 0;
				if (buf.Length * 2 != str_len) {
					buf[0] = (byte)Char.GetNumericValue(str[0].ToString(), 16);
					++index;
				}
				int v = 0;
				for (int i = index; i < str_len; i += 2) {
					v = ((int)Char.GetNumericValue(str[i].ToString(), 16) << 4) |
						((int)Char.GetNumericValue(str[i + 1].ToString(), 16));
					buf[index] = (byte)(v & 0x0FF);
					++index;
				}

				return new TObject(TType.BinaryType, new ByteLongObject(buf));
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return TType.BinaryType;
			}

		}

		#endregion

		#region LeastFunction

		[Serializable]
		class LeastFunction : Function {
			public LeastFunction(Expression[] parameters)
				: base("least", parameters) {

				if (ParameterCount < 1)
					throw new Exception("Least function must have at least 1 argument.");
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				TObject least = null;
				for (int i = 0; i < ParameterCount; ++i) {
					TObject ob = this[i].Evaluate(group, resolver, context);
					if (ob.IsNull) {
						return ob;
					}
					if (least == null || ob.CompareTo(least) < 0) {
						least = ob;
					}
				}
				return least;
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return this[0].ReturnTType(resolver, context);
			}

		}

		#endregion

		#region GreatestFunction

		[Serializable]
		class GreatestFunction : Function {
			public GreatestFunction(Expression[] parameters)
				: base("greatest", parameters) {

				if (ParameterCount < 1) {
					throw new Exception("Greatest function must have at least 1 argument.");
				}
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver,
											 IQueryContext context) {
				TObject great = null;
				for (int i = 0; i < ParameterCount; ++i) {
					TObject ob = this[i].Evaluate(group, resolver, context);
					if (ob.IsNull) {
						return ob;
					}
					if (great == null || ob.CompareTo(great) > 0) {
						great = ob;
					}
				}
				return great;
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return this[0].ReturnTType(resolver, context);
			}
		}

		#endregion

		#region CoalesceFunction

		// Coalesce - COALESCE(address2, CONCAT(city, ', ', state, '  ', zip))
		[Serializable]
		class CoalesceFunction : Function {
			public CoalesceFunction(Expression[] parameters)
				: base("coalesce", parameters) {
				if (ParameterCount < 1) {
					throw new Exception("COALESCE function must have at least 1 parameter.");
				}
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				int count = ParameterCount;
				for (int i = 0; i < count - 1; ++i) {
					TObject res = this[i].Evaluate(group, resolver, context);
					if (!res.IsNull) {
						return res;
					}
				}
				return this[count - 1].Evaluate(group, resolver, context);
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				// It's impossible to know the return type of this function until runtime
				// because either comparator could be returned.  We could assume that
				// both branch expressions result in the same type of object but this
				// currently is not enforced.

				// Go through each argument until we find the first parameter we can
				// deduce the class of.
				int count = ParameterCount;
				for (int i = 0; i < count; ++i) {
					TType t = this[i].ReturnTType(resolver, context);
					if (!(t is TNullType)) {
						return t;
					}
				}
				// Can't work it out so return null type
				return TType.NullType;
			}

		}

		#endregion

		#region CurrValFunction

		[Serializable]
		class CurrValFunction : Function {
			public CurrValFunction(Expression[] parameters)
				: base("currval", parameters) {

				// The parameter is the name of the table you want to bring the unique
				// key in from.
				if (ParameterCount != 1) {
					throw new Exception("'currval' function must have only 1 argument.");
				}
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				String str = this[0].Evaluate(group, resolver, context).Object.ToString();
				long v = context.CurrentSequenceValue(str);
				return TObject.GetInt8(v);
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return TType.NumericType;
			}
		}

		#endregion

		#region NextValFunction

		[Serializable]
		class NextValFunction : Function {
			public NextValFunction(Expression[] parameters)
				: base("nextval", parameters) {

				// The parameter is the name of the table you want to bring the unique
				// key in from.
				if (ParameterCount != 1)
					throw new Exception("'nextval' function must have only 1 argument.");
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				String str = this[0].Evaluate(group, resolver, context).Object.ToString();
				long v = context.NextSequenceValue(str);
				return TObject.GetInt8(v);
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return TType.NumericType;
			}

		}

		#endregion

		#region SetValFunction

		[Serializable]
		class SetValFunction : Function {
			public SetValFunction(Expression[] parameters)
				: base("setval", parameters) {

				// The parameter is the name of the table you want to bring the unique
				// key in from.
				if (ParameterCount != 2) {
					throw new Exception("'setval' function must have 2 arguments.");
				}
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				String str = this[0].Evaluate(group, resolver, context).Object.ToString();
				BigNumber num = this[1].Evaluate(group, resolver, context).ToBigNumber();
				long v = num.ToInt64();
				context.SetSequenceValue(str, v);
				return TObject.GetInt8(v);
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return TType.NumericType;
			}
		}

		#endregion

		#region UniqueKeyFunction

		[Serializable]
		class UniqueKeyFunction : Function {
			public UniqueKeyFunction(Expression[] parameters)
				: base("uniquekey", parameters) {

				// The parameter is the name of the table you want to bring the unique
				// key in from.
				if (ParameterCount != 1) {
					throw new Exception("'uniquekey' function must have only 1 argument.");
				}
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				String str = this[0].Evaluate(group, resolver, context).Object.ToString();
				long v = context.NextSequenceValue(str);
				return TObject.GetInt8(v);
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return TType.NumericType;
			}

		}

		#endregion

		#region SQLCastFunction

		[Serializable]
		class SQLCastFunction : Function {

			private readonly TType cast_to_type;

			public SQLCastFunction(Expression[] parameters)
				: base("sql_cast", parameters) {

				// Two parameters - the value to cast and the type to cast to (encoded)
				if (ParameterCount != 2) {
					throw new Exception("'sql_cast' function must have only 2 arguments.");
				}

				// Get the encoded type and parse it into a TType object and cache
				// locally in this object.  We expect that the second parameter of this
				// function is always constant.
				Expression exp = parameters[1];
				if (exp.Count != 1) {
					throw new Exception(
						"'sql_cast' function must have simple second parameter.");
				}

				Object vob = parameters[1].Last;
				if (vob is TObject) {
					TObject ob = (TObject)vob;
					String encoded_type = ob.Object.ToString();
					cast_to_type = TType.DecodeString(encoded_type);
				} else {
					throw new Exception("'sql_cast' function must have simple second parameter.");
				}
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				TObject ob = this[0].Evaluate(group, resolver, context);
				// If types are the same then no cast is necessary and we return this
				// object.
				if (ob.TType.SQLType == cast_to_type.SQLType) {
					return ob;
				}
				// Otherwise cast the object and return the new typed object.
				Object casted_ob = TType.CastObjectToTType(ob.Object, cast_to_type);
				return new TObject(cast_to_type, casted_ob);

			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return cast_to_type;
			}

		}

		#endregion

		#region VersionFunction

		[Serializable]
		private class VersionFunction : Function {
			public VersionFunction(Expression[] parameters) 
				: base("version", parameters) {
			}

			public override TObject Evaluate(IGroupResolver group, IVariableResolver resolver, IQueryContext context) {
				Version version = ProductInfo.Current.Version;
				return TObject.GetString(version.ToString(2));
			}

			public override TType ReturnTType(IVariableResolver resolver, IQueryContext context) {
				return TType.StringType;
			}
		}

		#endregion
	}
}