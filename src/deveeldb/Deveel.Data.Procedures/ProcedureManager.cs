// 
//  Copyright 2010  Deveel
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

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;

namespace Deveel.Data.Procedures {
	///<summary>
	/// A DatabaseConnection procedure manager.
	///</summary>
	/// <remarks>
	/// This controls adding, updating, deleting and querying/calling 
	/// stored procedures.
	/// </remarks>
	public class ProcedureManager {
		/// <summary>
		/// The DatabaseConnection.
		/// </summary>
		private readonly DatabaseConnection connection;

		/// <summary>
		/// The context.
		/// </summary>
		private readonly DatabaseQueryContext context;


		internal ProcedureManager(DatabaseConnection connection) {
			this.connection = connection;
			context = new DatabaseQueryContext(connection);
		}

		/// <summary>
		/// Gets a procedure entry.
		/// </summary>
		/// <param name="table">The table containing procedures informations (SystemFunctions).</param>
		/// <param name="procedure_name">Name of the procedure to return.</param>
		/// <returns>
		/// Returns a one.row table containing informations about the procedure
		/// entry wanted.
		/// </returns>
		private Table FindProcedureEntry(Table table, ProcedureName procedure_name) {

			Operator EQUALS = Operator.Get("=");

			VariableName schemav = table.GetResolvedVariable(0);
			VariableName namev = table.GetResolvedVariable(1);

			Table t = table.SimpleSelect(context, namev, EQUALS,
			                             new Expression(TObject.CreateString(procedure_name.Name)));
			t = t.ExhaustiveSelect(context, Expression.Simple(
			                                	schemav, EQUALS, TObject.CreateString(procedure_name.Schema)));

			// This should be at most 1 row in size
			if (t.RowCount > 1) {
				throw new Exception(
					"Assert failed: multiple procedure names for " + procedure_name);
			}

			// Return the entries found.
			return t;

		}

		/// <summary>
		/// Formats a string that gives information about the procedure, return
		/// type and param types.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="ret"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		private static String ProcedureInfoString(ProcedureName name,
		                                          TType ret, TType[] parameters) {
			StringBuilder buf = new StringBuilder();
			if (ret != null) {
				buf.Append(ret.ToSQLString());
				buf.Append(" ");
			}
			buf.Append(name.Name);
			buf.Append("(");
			for (int i = 0; i < parameters.Length; ++i) {
				buf.Append(parameters[i].ToSQLString());
				if (i < parameters.Length - 1) {
					buf.Append(", ");
				}
			}
			buf.Append(")");
			return buf.ToString();
		}

		///<summary>
		/// Given a location string as defined for a .NET stored procedure, this
		/// parses the string into the various parts.
		///</summary>
		///<param name="str"></param>
		/// <remarks>
		/// For example, given the string <c>MyCompany.StoredProcedures.MyFunctions.MinFunction()</c> 
		/// this will parse the string out to the class called <c>MyCompany.StoredProcedures.MyFunctions</c> 
		/// and the method <c>MinFunction</c> with no arguments.  This function will work event if 
		/// the method name is not given, or the method name does not have an arguments specification.
		/// </remarks>
		///<returns></returns>
		///<exception cref="StatementException"></exception>
		public static String[] ParseLocationString(String str) {
			// Look for the first parenthese
			int parenthese_delim = str.IndexOf("(");
			String class_method;

			if (parenthese_delim != -1) {
				// This represents class/method
				class_method = str.Substring(0, parenthese_delim);
				// This will be deliminated by a '.'
				int method_delim = class_method.LastIndexOf(".");
				if (method_delim == -1) {
					throw new StatementException(
						"Incorrectly formatted method string: " + str);
				}
				String class_str = class_method.Substring(0, method_delim);
				String method_str = class_method.Substring(method_delim + 1);
				// Next parse the argument list
				int end_parenthese_delim = str.LastIndexOf(")");
				if (end_parenthese_delim == -1) {
					throw new StatementException(
						"Incorrectly formatted method string: " + str);
				}
				String arg_list_str =
					str.Substring(parenthese_delim + 1, end_parenthese_delim - (parenthese_delim + 1));
				// Now parse the list of arguments
				ArrayList arg_list = new ArrayList();
				string[] tok = arg_list_str.Split(',');
				for (int i = 0; i < tok.Length; i++) {
					arg_list.Add(tok[i]);
				}

				// Form the parsed array and return it
				int sz = arg_list.Count;
				String[] return_array = new String[2 + sz];
				return_array[0] = class_str;
				return_array[1] = method_str;
				for (int i = 0; i < sz; ++i) {
					return_array[i + 2] = (String) arg_list[i];
				}
				return return_array;

			}

			// No parenthese so we assume this is a class
			return new String[] {str};
		}

		///<summary>
		/// Returns true if the procedure with the given name exists.
		///</summary>
		///<param name="procedure_name"></param>
		///<returns></returns>
		public bool ProcedureExists(ProcedureName procedure_name) {
			DataTable table = connection.GetTable(Database.SysFunction);
			return FindProcedureEntry(table, procedure_name).RowCount == 1;

		}

		///<summary>
		/// Returns true if the procedure with the given table name exists.
		///</summary>
		///<param name="procedure_name"></param>
		///<returns></returns>
		public bool ProcedureExists(TableName procedure_name) {
			return ProcedureExists(new ProcedureName(procedure_name));
		}

		/// <summary>
		/// Defines a stored procedure.
		/// </summary>
		/// <param name="procedure_name">The name of the procedure.</param>
		/// <param name="specification"></param>
		/// <param name="return_type">The return type of the procedure (if null,
		/// the procedure doesn't return any value).</param>
		/// <param name="param_types">The parameters types.</param>
		/// <param name="username">The name of the user defining the procedure.</param>
		/// <remarks>
		/// If the procedure has been defined then it is overwritten with 
		/// this informations.
		/// </remarks>
		/// <exception cref="StatementException">
		/// If an ambigous reference was found for the given 
		/// <paramref name="procedure_name"/>.
		/// </exception>
		public void DefineProcedure(ProcedureName procedure_name,
		                            String specification,
		                            TType return_type, TType[] param_types,
		                            String username) {

			TableName proc_table_name =
				new TableName(procedure_name.Schema, procedure_name.Name);

			// Check this name is not reserved
			DatabaseConnection.CheckAllowCreate(proc_table_name);

			DataTable table = connection.GetTable(Database.SysFunction);

			// The new row to insert/update    
			DataRow dataRow = new DataRow(table);
			dataRow.SetValue(0, procedure_name.Schema);
			dataRow.SetValue(1, procedure_name.Name);
			dataRow.SetValue(2, "Type1");
			dataRow.SetValue(3, specification);
			if (return_type != null) {
				dataRow.SetValue(4, TType.Encode(return_type));
			}
			dataRow.SetValue(5, TType.Encode(param_types));
			dataRow.SetValue(6, username);

			// Find the entry from the procedure table that equal this name
			Table t = FindProcedureEntry(table, procedure_name);

			// Delete the entry if it already exists.
			if (t.RowCount == 1) {
				table.Delete(t);
			}

			// Insert the new entry,
			table.Add(dataRow);

			// Notify that this database object has been successfully created.
			connection.DatabaseObjectCreated(proc_table_name);

		}

		/// <summary>
		/// Deletes the procedure with the given name.
		/// </summary>
		/// <param name="procedure_name">Name of the procedure to delete.</param>
		/// <exception cref="StatementException">
		/// If an ambigous reference or none procedure was found for the 
		/// given <paramref name="procedure_name"/>.
		/// </exception>
		public void DeleteProcedure(ProcedureName procedure_name) {

			DataTable table = connection.GetTable(Database.SysFunction);

			// Find the entry from the procedure table that equal this name
			Table t = FindProcedureEntry(table, procedure_name);

			// If no entries then generate error.
			if (t.RowCount == 0) {
				throw new StatementException("Procedure " + procedure_name +
				                             " doesn't exist.");
			}

			table.Delete(t);

			// Notify that this database object has been successfully dropped.
			connection.DatabaseObjectDropped(
				new TableName(procedure_name.Schema, procedure_name.Name));

		}

		/// <summary>
		/// Generates an internal table that models informations about the
		/// procedures within the given transaction.
		/// </summary>
		/// <param name="transaction"></param>
		/// <returns>
		/// Returns an <see cref="IInternalTableInfo"/> used to model the list 
		/// of procedures that are accessible within the given transaction.
		/// </returns>
		internal static IInternalTableInfo CreateInternalTableInfo(Transaction transaction) {
			return new ProcedureInternalTableInfo(transaction);
		}

		/// <summary>
		/// Invokes the procedure with the given name and the given parameters.
		/// </summary>
		/// <param name="procedure_name">Name of the procedure to invoke.</param>
		/// <param name="parameters">The parameters to pass to the procedure for
		/// the invoke, or null if no parameters.</param>
		/// <returns>
		/// Returns a <see cref="TObject"/> result of the invoke of the procedure.
		/// </returns>
		/// <exception cref="StatementException">
		/// If an ambigous reference or none procedure was found for the 
		/// given <paramref name="procedure_name"/>.
		/// </exception>
		public TObject InvokeProcedure(ProcedureName procedure_name,
		                               TObject[] parameters) {

			DataTable table = connection.GetTable(Database.SysFunction);

			// Find the entry from the procedure table that equals this name
			Table t = FindProcedureEntry(table, procedure_name);
			if (t.RowCount == 0) {
				throw new StatementException("Procedure " + procedure_name +
				                             " doesn't exist.");
			}

			//TODO: check this...
			int row_index = t.GetRowEnumerator().RowIndex;
			TObject type_ob = t.GetCellContents(2, row_index);
			TObject location_ob = t.GetCellContents(3, row_index);
			TObject return_type_ob = t.GetCellContents(4, row_index);
			TObject param_types_ob = t.GetCellContents(5, row_index);
			TObject owner_ob = t.GetCellContents(6, row_index);

			String type = type_ob.Object.ToString();
			String location = location_ob.Object.ToString();
			TType return_type = null;
			if (!return_type_ob.IsNull) {
				return_type = TType.DecodeString(return_type_ob.Object.ToString());
			}
			TType[] param_types =
				TType.DecodeTypes(param_types_ob.Object.ToString());
			String owner = owner_ob.Object.ToString();

			// Check the number of parameters given match the function parameters length
			if (parameters.Length != param_types.Length) {
				throw new StatementException(
					"Parameters given do not match the parameters of the procedure: " +
					ProcedureInfoString(procedure_name, return_type, param_types));
			}

			// The different procedure types,
			if (type.Equals("Type1")) {
				return InvokeType1Procedure(procedure_name, location,
				                            return_type, param_types, owner, parameters);
			}
			
			throw new Exception("Unknown procedure type: " + type);
		}

		/// <summary>
		/// Resolves a type specification string to a <see cref="System.Type"/>.
		/// </summary>
		/// <param name="type_string"></param>
		/// <returns></returns>
		private static Type ResolveToType(String type_string) {
			// Trim the string
			type_string = type_string.Trim();
			// Is this an array?  Count the number of array dimensions.
			int dimensions = -1;
			int last_index = type_string.Length;
			while (last_index > 0) {
				++dimensions;
				last_index = type_string.LastIndexOf("[]", last_index) - 1;
			}
			// Remove the array part
			int array_end = type_string.Length - (dimensions * 2);
			String class_part = type_string.Substring(0, array_end);
			// Check there's no array parts in the class part
			if (class_part.IndexOf("[]") != -1) {
				throw new Exception(
					"Type specification incorrectly formatted: " + type_string);
			}

			// Convert the specification to a .NET Type.  For example,
			// String is converted to typeof(System.String), etc.
			Type cl;
			// Is there a '.' in the class specification?
			if (class_part.IndexOf(".") != -1) {
				// Must be a specification such as 'System.Uri' or 'System.Collection.IList'.
				try {
					cl = Type.GetType(class_part);
				} catch (TypeLoadException) {
					throw new Exception("Type not found: " + class_part);
				}
			}

				// Try for a primitive types
			else if (class_part.Equals("boolean") ||
					 class_part.Equals("bool")) {
				cl = typeof(bool);
			} else if (class_part.Equals("byte")) {
				cl = typeof(byte);
			} else if (class_part.Equals("short")) {
				cl = typeof(short);
			} else if (class_part.Equals("char")) {
				cl = typeof(char);
			} else if (class_part.Equals("int")) {
				cl = typeof(int);
			} else if (class_part.Equals("long")) {
				cl = typeof(long);
			} else if (class_part.Equals("float")) {
				cl = typeof(float);
			} else if (class_part.Equals("double")) {
				cl = typeof(double);
			} else {
				// Not a primitive type so try resolving against System.* or some
				// key classes in Deveel.Data.*
				if (class_part.Equals("IProcedureConnection")) {
					cl = typeof(IProcedureConnection);
				} else {
					try {
						cl = Type.GetType("System." + class_part);
					} catch (TypeLoadException) {
						// No luck so give up,
						throw new Exception("Type not found: " + class_part);
					}
				}
			}

			// Finally make into a dimension if necessary
			if (dimensions > 0) {
				// This is a little untidy way of doing this.  Perhaps a better approach
				// would be to make an array encoded string.
				cl = Array.CreateInstance(cl, new int[dimensions]).GetType();
			}

			return cl;

		}




		/// <summary>
		/// Given a location and a list of parameter types, returns an
		/// immutable <see cref="System.Reflection.MethodInfo"/> that can 
		/// be used to invoke a stored procedure.
		/// </summary>
		/// <param name="location_str"></param>
		/// <param name="param_types"></param>
		/// <remarks>
		/// The returned object can be cached if necessary. Note that 
		/// this method will generate an error for the following situations:
		/// a) The invokation type or method was not found, b) there is 
		/// not an invokation method with the required number of arguments 
		/// or that matches the method specification.
		/// </remarks>
		/// <returns>
		/// Returns <b>null</b> if the invokation method could not be found.
		/// </returns>
		public static MethodInfo GetProcedureMethod(String location_str, TType[] param_types) {
			// Parse the location string
			String[] loc_parts = ParseLocationString(location_str);

			// The name of the class
			String class_name;
			// The name of the invokation method in the class.
			String method_name;
			// The object specification that must be matched.  If any entry is 'null'
			// then the argument parameter is discovered.
			Type[] object_specification;
			bool firstProcedureConnectionIgnore;

			if (loc_parts.Length == 1) {
				// This means the location_str only specifies a class name, so we use
				// 'invoke' as the static method to call, and discover the arguments.
				class_name = loc_parts[0];
				method_name = "Invoke";
				// All null which means we discover the arg types dynamically
				object_specification = new Type[param_types.Length];
				// ignore IProcedureConnection is first argument
				firstProcedureConnectionIgnore = true;
			} else {
				// This means we specify a class and method name and argument
				// specification.
				class_name = loc_parts[0];
				method_name = loc_parts[1];
				object_specification = new Type[loc_parts.Length - 2];

				for (int i = 0; i < loc_parts.Length - 2; ++i) {
					String java_spec = loc_parts[i + 2];
					object_specification[i] = ResolveToType(java_spec);
				}

				firstProcedureConnectionIgnore = false;
			}

			Type procedure_class;
			try {
				// Reference the procedure's class.
				procedure_class = Type.GetType(class_name);
			} catch (TypeLoadException) {
				throw new Exception("Procedure class not found: " + class_name);
			}

			// Get all the methods in this class
			MethodInfo[] methods = procedure_class.GetMethods();
			MethodInfo invoke_method = null;
			// Search for the invoke method
			for (int i = 0; i < methods.Length; ++i) {
				MethodInfo method = methods[i];

				if (method.IsStatic && method.IsPublic &&
				    method.Name.Equals(method_name)) {

					bool params_match;

					// Get the parameters for this method
					ParameterInfo[] method_args = method.GetParameters();

					// If no methods, and object_specification has no args then this is a
					// match.
					if (method_args.Length == 0 && object_specification.Length == 0) {
						params_match = true;
					} else {
						int search_start = 0;
						// Is the first arugments a IProcedureConnection implementation?
						if (firstProcedureConnectionIgnore &&
						    typeof(IProcedureConnection).IsAssignableFrom(method_args[0].ParameterType)) {
							search_start = 1;
						}
						// Do the number of arguments match
						if (object_specification.Length ==
						    method_args.Length - search_start) {
							// Do they match the specification?
							bool match_spec = true;
							for (int n = 0;
							     n < object_specification.Length && match_spec;
							     ++n) {
								Type ob_spec = object_specification[n];
								if (ob_spec != null &&
								    ob_spec != method_args[n + search_start].ParameterType) {
									match_spec = false;
								}
							}
							params_match = match_spec;
						} else {
							params_match = false;
						}
					}

					if (params_match) {
						if (invoke_method == null) {
							invoke_method = method;
						} else {
							throw new Exception("Ambiguous public static " +
							                    method_name + " methods in stored procedure class '" +
							                    class_name + "'");
						}
					}

				}

			}

			// Return the invoke method we found
			return invoke_method;

		}



		// ---------- Various procedure type invokation methods ----------

		/// <summary>
		/// Invokes a static procedure.
		/// </summary>
		/// <param name="procedure_name"></param>
		/// <param name="location_str"></param>
		/// <param name="return_type"></param>
		/// <param name="param_types"></param>
		/// <param name="owner"></param>
		/// <param name="param_values"></param>
		/// <remarks>
		/// A type 1 procedure is represented by a single class with a 
		/// static invokation method (called Invoke). The parameters of 
		/// the static 'Invoke' method must be compatible class parameters 
		/// defined for the procedure, and the return class must also be
		/// compatible with the procedure return type.
		/// </remarks>
		/// <returns></returns>
		/// <exception cref="StatementException">
		/// If the Invoke method does not contain arguments that are compatible 
		/// with the parameters given or 
		/// </exception>
		/// <exception cref="System.SystemException">
		/// If the class contains more than a single public static <i>Invoke</i>
		/// method.
		/// </exception>
		private TObject InvokeType1Procedure(ProcedureName procedure_name,
		                                     String location_str, TType return_type, TType[] param_types,
		                                     String owner, TObject[] param_values) {

			// Search for the invokation method for this stored procedure
			MethodInfo invoke_method = GetProcedureMethod(location_str, param_types);

			// Did we find an invoke method?
			if (invoke_method == null) {
				throw new Exception("Could not find the invokation method for " +
				                    "the location string '" + location_str + "'");
			}

			// Go through each argument of this class and work out how we are going
			// cast from the database engine object to the object.
			ParameterInfo[] method_params = invoke_method.GetParameters();

			// Is the first param a IProcedureConnection implementation?
			int start_param;
			Object[] values;
			if (method_params.Length > 0 &&
			    typeof (IProcedureConnection).IsAssignableFrom(method_params[0].ParameterType)) {
				start_param = 1;
				values = new Object[param_types.Length + 1];
			}
			else {
				start_param = 0;
				values = new Object[param_types.Length];
			}

			// For each type    
			for (int i = 0; i < param_types.Length; ++i) {
				TObject value = param_values[i];
				TType proc_type = param_types[i];
				Type parameterType = method_params[i + start_param].ParameterType;
				String type_str = parameterType.Name;

				// First null check,
				if (value.IsNull) {
					values[i + start_param] = null;
				}
				else {
					TType value_type = value.TType;
					// If not null, is the value and the procedure type compatible
					if (proc_type.IsComparableType(value_type)) {

						bool error_cast = false;
						Object cast_value = null;

						// Compatible types,
						// Now we need to convert the parameter value into an object,
						if (value_type is TStringType) {
							// A String type can be represented as a System.String,
							// or as a System.IO.TextReader.
							IStringAccessor accessor = (IStringAccessor) value.Object;
							if (parameterType == typeof (String)) {
								cast_value = accessor.ToString();
							}
							else if (parameterType == typeof (TextReader)) {
								cast_value = accessor.GetTextReader();
							}
							else {
								error_cast = true;
							}
						}
						else if (value_type is TBooleanType) {
							if (parameterType == typeof (bool)) {
								cast_value = value.Object;
							}
							else {
								error_cast = true;
							}
						}
						else if (value_type is TDateType) {
							DateTime d = (DateTime) value.Object;
							if (parameterType == typeof (DateTime)) {
								cast_value = d;
							}
							else {
								error_cast = true;
							}
						}
						else if (value_type is TNumericType) {
							// Number can be cast to any one of the numeric types
							BigNumber num = (BigNumber) value.Object;
							if (parameterType == typeof (BigNumber)) {
								cast_value = num;
							}
							else if (parameterType == typeof (byte)) {
								cast_value = num.ToByte();
							}
							else if (parameterType == typeof (short)) {
								cast_value = num.ToInt16();
							}
							else if (parameterType == typeof (int)) {
								cast_value = num.ToInt32();
							}
							else if (parameterType == typeof (long)) {
								cast_value = num.ToInt64();
							}
							else if (parameterType == typeof (float)) {
								cast_value = num.ToSingle();
							}
							else if (parameterType == typeof (double)) {
								cast_value = num.ToDouble();
							}
							else if (parameterType == typeof (decimal)) {
								cast_value = num.ToBigDecimal();
							}
							else {
								error_cast = true;
							}
						}
						else if (value_type is TBinaryType) {
							// A binary type can translate to a System.IO.Stream or a
							// byte[] array.
							IBlobAccessor blob = (IBlobAccessor) value.Object;
							if (parameterType == typeof (Stream)) {
								cast_value = blob.GetInputStream();
							}
							else if (parameterType == typeof (byte[])) {
								byte[] buf = new byte[blob.Length];
								try {
									Stream input = blob.GetInputStream();
									int n = 0;
									int len = blob.Length;
									while (len > 0) {
										int count = input.Read(buf, n, len);
										if (count == -1) {
											throw new IOException("End of stream.");
										}
										n += count;
										len -= count;
									}
								}
								catch (IOException e) {
									throw new Exception("IO Error: " + e.Message);
								}
								cast_value = buf;
							}
							else {
								error_cast = true;
							}

						}

						// If the cast of the parameter was not possible, report the error.
						if (error_cast) {
							throw new StatementException("Unable to cast argument " + i +
							                             " ... " + value_type.ToSQLString() + " to " + type_str +
							                             " for procedure: " +
							                             ProcedureInfoString(procedure_name, return_type, param_types));
						}

						// Set the value for this parameter
						values[i + start_param] = cast_value;

					}
					else {
						// The parameter is not compatible -
						throw new StatementException("Parameter (" + i + ") not compatible " +
						                             value.TType.ToSQLString() + " -> " + proc_type.ToSQLString() +
						                             " for procedure: " +
						                             ProcedureInfoString(procedure_name, return_type, param_types));
					}

				} // if not null

			} // for each parameter

			// Create the user that has the privs of this procedure.
			User priv_user = new User(owner, connection.Database,
			                          "/Internal/Procedure/", DateTime.Now);

			// Create the IProcedureConnection object.
			IProcedureConnection proc_connection =
				connection.CreateProcedureConnection(priv_user);
			Object result;
			try {
				// Now the 'connection' will be set to the owner's user privs.

				// Set the IProcedureConnection object as an argument if necessary.
				if (start_param > 0) {
					values[0] = proc_connection;
				}

				// The values array should now contain the parameter values formatted
				// as objects.

				// Invoke the method
				try {
					result = invoke_method.Invoke(null, values);
				} catch (AccessViolationException e) {
					connection.Debug.WriteException(e);
					throw new StatementException("Illegal access exception when invoking " +
					                             "stored procedure: " + e.Message);
				} catch (TargetInvocationException e) {
					Exception real_e = e.InnerException;
					connection.Debug.WriteException(real_e);
					throw new StatementException("Procedure Exception: " + real_e.Message);
				}

			} finally {
				connection.DisposeProcedureConnection(proc_connection);
			}

			// If return_type is null, there is no result from this procedure (void)
			if (return_type == null)
				return null;
			// Cast to a valid return object and return.
			return TObject.CreateAndCastFromObject(return_type, result);
		}

		// ---------- Inner classes ----------

		/// <summary>
		/// An object that models the list of procedures as table objects 
		/// in a transaction.
		/// </summary>
		private sealed class ProcedureInternalTableInfo : InternalTableInfo2 {

			internal ProcedureInternalTableInfo(Transaction transaction)
				: base(transaction, Database.SysFunction) {
			}

			private static DataTableInfo CreateTableInfo(String schema, String name) {
				// Create the DataTableInfo that describes this entry
				DataTableInfo info = new DataTableInfo(new TableName(schema, name));

				// Add column definitions
				info.AddColumn("type", TType.StringType);
				info.AddColumn("location", TType.StringType);
				info.AddColumn("return_type", TType.StringType);
				info.AddColumn("param_args", TType.StringType);
				info.AddColumn("owner", TType.StringType);

				// Set to immutable
				info.IsReadOnly = true;

				// Return the data table info
				return info;
			}


			public override String GetTableType(int i) {
				return "FUNCTION";
			}

			public override DataTableInfo GetTableInfo(int i) {
				TableName tableName = GetTableName(i);
				return CreateTableInfo(tableName.Schema, tableName.Name);
			}

			public override IMutableTableDataSource CreateInternalTable(int index) {
				IMutableTableDataSource table = transaction.GetTable(Database.SysFunction);
				IRowEnumerator row_e = table.GetRowEnumerator();
				int p = 0;
				int i;
				int row_i = -1;
				while (row_e.MoveNext()) {
					i = row_e.RowIndex;
					if (p == index) {
						row_i = i;
					} else {
						++p;
					}
				}

				if (p != index)
					throw new Exception("Index out of bounds.");

				string schema = table.GetCellContents(0, row_i).Object.ToString();
				string name = table.GetCellContents(1, row_i).Object.ToString();

				DataTableInfo tableInfo = CreateTableInfo(schema, name);
				TObject type = table.GetCellContents(2, row_i);
				TObject location = table.GetCellContents(3, row_i);
				TObject returnType = table.GetCellContents(4, row_i);
				TObject paramTypes = table.GetCellContents(5, row_i);
				TObject owner = table.GetCellContents(6, row_i);

				// Implementation of IMutableTableDataSource that describes this
				// procedure.
				GTDataSourceImpl dataSource = new GTDataSourceImpl(transaction.System, tableInfo);
				dataSource.type = type;
				dataSource.location = location;
				dataSource.return_type = returnType;
				dataSource.param_types = paramTypes;
				dataSource.owner = owner;
				return dataSource;
			}

			private class GTDataSourceImpl : GTDataSource {
				private readonly DataTableInfo tableInfo;
				internal TObject type;
				internal TObject location;
				internal TObject return_type;
				internal TObject param_types;
				internal TObject owner;

				public GTDataSourceImpl(TransactionSystem system, DataTableInfo tableInfo)
					: base(system) {
					this.tableInfo = tableInfo;
				}

				public override DataTableInfo TableInfo {
					get { return tableInfo; }
				}

				public override int RowCount {
					get { return 1; }
				}

				public override TObject GetCellContents(int col, int row) {
					switch (col) {
						case 0:
							return type;
						case 1:
							return location;
						case 2:
							return return_type;
						case 3:
							return param_types;
						case 4:
							return owner;
						default:
							throw new Exception("Column out of bounds.");
					}
				}
			}
		}
	}
}