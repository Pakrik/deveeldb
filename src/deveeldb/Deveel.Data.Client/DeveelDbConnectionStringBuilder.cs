﻿// 
//  Copyright 2010-2015 Deveel
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Globalization;

using Deveel.Data.Sql;

namespace Deveel.Data.Client {
	[DefaultProperty("DataSource")]
	public sealed class DeveelDbConnectionStringBuilder : DbConnectionStringBuilder {
		public DeveelDbConnectionStringBuilder(string connectionString) {
			InitToDefault();
			ConnectionString = connectionString;
		}

		public DeveelDbConnectionStringBuilder()
			: this(String.Empty) {
		}

		static DeveelDbConnectionStringBuilder() {
			defaults = new Dictionary<string, object>();
			defaults.Add(HostKey, DefaultHost);
			defaults.Add(PortKey, DefaultPort);
			defaults.Add(DatabaseKey, DefaultDatabase);
			defaults.Add(UserNameKey, DefaultUserName);
			defaults.Add(PasswordKey, DefaultPassword);
			defaults.Add(SchemaKey, DefaultSchema);
			defaults.Add(PathKey, DefaultPath);
			defaults.Add(CreateKey, DefaultCreate);
			defaults.Add(BootOrCreateKey, DefaultBootOrCreate);
			defaults.Add(ParameterStyleKey, DefaultParameterStyle);
			defaults.Add(VerboseColumnNamesKey, DefaultVerboseColumnName);
			defaults.Add(PersistSecurityInfoKey, DefaultPersistSecurityInfo);
			defaults.Add(RowCacheSizeKey, DefaultRowCacheSize);
			defaults.Add(MaxCacheSizeKey, DefaultMaxCacheSize);
			defaults.Add(FetchSizeKey, DefaultFetchSize);
			defaults.Add(MaxFetchSizeKey, DefaultMaxFetchSize);
			defaults.Add(AutoCommitKey, DefaultAutoCommit);
			defaults.Add(QueryTimeoutKey, DefaultQueryTimeout);

			keymaps = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			keymaps[HostKey.ToUpper()] = HostKey;
			keymaps["ADDRESS"] = HostKey;
			keymaps["SERVER"] = HostKey;
			keymaps[PortKey.ToUpper()] = PortKey;
			keymaps[DatabaseKey.ToUpper()] = DatabaseKey;
			keymaps["CATALOG"] = DatabaseKey;
			keymaps["INITIAL CATALOG"] = DatabaseKey;
			keymaps["DB"] = DatabaseKey;
			keymaps[SchemaKey.ToUpper()] = SchemaKey;
			keymaps["DEFAULT SCHEMA"] = SchemaKey;
			keymaps[PathKey.ToUpper()] = PathKey;
			keymaps["DATA PATH"] = PathKey;
			keymaps["DATABASE PATH"] = PathKey;
			keymaps["DATAPATH"] = PathKey;
			keymaps["DATABASEPATH"] = PathKey;
			keymaps[CreateKey.ToUpper()] = CreateKey;
			keymaps[BootOrCreateKey] = BootOrCreateKey;
			keymaps["BOOT OR CREATE"] = BootOrCreateKey;
			keymaps["CREATE OR BOOT"] = BootOrCreateKey;
			keymaps["CREATEORBOOT"] = BootOrCreateKey;
			keymaps[CreateKey.ToUpper()] = CreateKey;
			keymaps["CREATE DATABASE"] = CreateKey;
			keymaps[UserNameKey.ToUpper()] = UserNameKey;
			keymaps["USER"] = UserNameKey;
			keymaps["USER NAME"] = UserNameKey;
			keymaps["USER ID"] = UserNameKey;
			keymaps["USERID"] = UserNameKey;
			keymaps["UID"] = UserNameKey;
			keymaps[PasswordKey.ToUpper()] = PasswordKey;
			keymaps["PASS"] = PasswordKey;
			keymaps["PWD"] = PasswordKey;
			keymaps["SECRET"] = PasswordKey;
			keymaps[ParameterStyleKey.ToUpper()] = ParameterStyleKey;
			keymaps["PARAMSTYLE"] = ParameterStyleKey;
			keymaps["PARAMETER STYLE"] = ParameterStyleKey;
			keymaps["USEPARMAMETER"] = ParameterStyleKey;
			keymaps["USE PARAMETER"] = ParameterStyleKey;
			keymaps[VerboseColumnNamesKey.ToUpper()] = VerboseColumnNamesKey;
			keymaps["VERBOSE COLUMNS"] = VerboseColumnNamesKey;
			keymaps["VERBOSE COLUMN NAMES"] = VerboseColumnNamesKey;
			keymaps["VERBOSECOLUMNS"] = VerboseColumnNamesKey;
			keymaps["COLUMNS VERBOSE"] = VerboseColumnNamesKey;
			keymaps[PersistSecurityInfoKey.ToUpper()] = PersistSecurityInfoKey;
			keymaps["PERSIST SECURITY INFO"] = PersistSecurityInfoKey;
			keymaps[RowCacheSizeKey.ToUpper()] = RowCacheSizeKey;
			keymaps["ROW CACHE SIZE"] = RowCacheSizeKey;
			keymaps["CACHE SIZE"] = RowCacheSizeKey;
			keymaps[MaxCacheSizeKey.ToUpper()] = MaxCacheSizeKey;
			keymaps["MAX CACHE SIZE"] = MaxCacheSizeKey;
			keymaps["MAX CACHE"] = MaxCacheSizeKey;
			keymaps[QueryTimeoutKey.ToUpper()] = QueryTimeoutKey;
			keymaps["QUERY TIMEOUT"] = QueryTimeoutKey;
			keymaps[IgnoreIdentifiersCaseKey.ToUpper()] = IgnoreIdentifiersCaseKey;
			keymaps["IGNORE CASE"] = IgnoreIdentifiersCaseKey;
			keymaps["IGNORE ID CASE"] = IgnoreIdentifiersCaseKey;
			keymaps["ID CASE IGNORED"] = IgnoreIdentifiersCaseKey;
			keymaps[StrictGetValueKey.ToUpper()] = StrictGetValueKey;
			keymaps["STRICT"] = StrictGetValueKey;
			keymaps["STRICT GETVALUE"] = StrictGetValueKey;
			keymaps["STRICT VALUE"] = StrictGetValueKey;
			keymaps["STRICTVALUE"] = StrictGetValueKey;
			keymaps[FetchSizeKey.ToUpper()] = FetchSizeKey;
			keymaps["FETCH SIZE"] = FetchSizeKey;
			keymaps["ROW COUNT"] = FetchSizeKey;
			keymaps["ROWCOUNT"] = FetchSizeKey;
			keymaps[MaxFetchSizeKey.ToUpper()] = MaxFetchSizeKey;
			keymaps["MAX FETCH SIZE"] = MaxFetchSizeKey;
			keymaps["MAXFETCHSIZE"] = MaxFetchSizeKey;
			keymaps["MAX ROW COUNT"] = MaxFetchSizeKey;
			keymaps["MAX ROWCOUNT"] = MaxFetchSizeKey;
			keymaps["MAXROWCOUNT"] = MaxFetchSizeKey;
			keymaps[AutoCommitKey.ToUpper()] = AutoCommitKey;
			keymaps["AUTOCOMMIT"] = AutoCommitKey;
			keymaps["AUTO-COMMIT"] = AutoCommitKey;
			keymaps["AUTO_COMMIT"] = AutoCommitKey;
			keymaps["AUTO COMMIT"] = AutoCommitKey;
			keymaps["COMMIT AUTO"] = AutoCommitKey;
			keymaps["COMMIT_AUTO"] = AutoCommitKey;
			keymaps["COMMITAUTO"] = AutoCommitKey;
			keymaps["COMMIT-AUTO"] = AutoCommitKey;
			keymaps["COMMIT"] = AutoCommitKey;
			keymaps["ENLIST"] = EnlistKey;
		}

		private const string HostKey = "Host";
		private const string PortKey = "Port";
		private const string DatabaseKey = "Database";
		private const string UserNameKey = "UserName";
		private const string PasswordKey = "Password";
		private const string SchemaKey = "Schema";
		private const string PathKey = "Path";
		private const string CreateKey = "Create";
		private const string BootOrCreateKey = "BootOrCreate";
		private const string ParameterStyleKey = "ParameterStyle";
		private const string VerboseColumnNamesKey = "VerboseColumnNames";
		private const string PersistSecurityInfoKey = "PersistSecurityInfo";
		private const string RowCacheSizeKey = "RowCacheSize";
		private const string MaxCacheSizeKey = "MaxCacheSize";
		private const string QueryTimeoutKey = "QueryTimeout";
		private const string IgnoreIdentifiersCaseKey = "IgnoreIdentifiersCase";
		private const string StrictGetValueKey = "StrictGetValue";
		private const string FetchSizeKey = "FetchSize";
		private const string MaxFetchSizeKey = "MaxFetchSize";
		private const string AutoCommitKey = "AutoCommit";
		private const string EnlistKey = "Enlist";

		private const string DefaultHost = "localhost";
		private const int DefaultPort = 9157;
		private const string DefaultDatabase = "";
		private const string DefaultUserName = "";
		private const string DefaultPassword = "";
		private const string DefaultSchema = "";
		private const string DefaultPath = ".";
		private const bool DefaultCreate = false;
		private const bool DefaultBootOrCreate = false;
		private const QueryParameterStyle DefaultParameterStyle = QueryParameterStyle.Marker;
		private const bool DefaultVerboseColumnName = false;
		private const bool DefaultPersistSecurityInfo = false;
		private const int DefaultRowCacheSize = 1024;
		private const int DefaultMaxCacheSize = 1024 * 40;
		private const int DefaultQueryTimeout = Int32.MaxValue;
		private const bool DefaultIgnoreIdentifiersCase = true;
		private const bool DefaultStrictGetValue = false;
		private const int DefaultMaxFetchSize = 512;
		private const int DefaultFetchSize = 32;
		private const bool DefaultAutoCommit = true;
		private const bool DefaultEnlist = false;

		private static readonly Dictionary<string, object> defaults;
		private static readonly Dictionary<string, string> keymaps;

		private string host;
		private int port;
		private string database;
		private string userName;
		private string password;
		private string schema;
		private string path;
		private bool verboseColumnNames;
		private QueryParameterStyle paramStyle;
		private bool persistSecurityInfo;
		private int rowCacheSize;
		private int maxCacheSize;
		private int queryTimeout;
		private bool ignoreCase;
		private bool create;
		private bool bootOrCreate;
		private bool strictGetValue;
		private int fetchSize;
		private int maxFetchSize;
		private bool autoCommit;
		private bool enlist;

		public override bool IsFixedSize {
			get { return true; }
		}

		public override object this[string keyword] {
			get {
				keyword = MappKey(keyword);
				if (base.ContainsKey(keyword))
					return base[keyword];
				return defaults[keyword];
			}
			set { SetValue(keyword, value); }
		}

		public override ICollection Keys {
			get {
				var keys = new List<string> {
					HostKey,
					PortKey,
					DatabaseKey,
					SchemaKey,
					PathKey,
					UserNameKey,
					PasswordKey,
					PersistSecurityInfoKey,
					VerboseColumnNamesKey,
					ParameterStyleKey,
					RowCacheSizeKey,
					MaxCacheSizeKey,
					QueryTimeoutKey,
					IgnoreIdentifiersCaseKey,
					CreateKey,
					BootOrCreateKey,
					StrictGetValueKey,
					FetchSizeKey,
					MaxFetchSizeKey,
					AutoCommitKey,
					EnlistKey
				};
				return keys.AsReadOnly();
			}
		}

		public override ICollection Values {
			get {
				var list = new List<object> {
					host,
					port,
					database,
					schema,
					path,
					userName,
					password,
					persistSecurityInfo,
					verboseColumnNames,
					paramStyle,
					rowCacheSize,
					maxCacheSize,
					queryTimeout,
					ignoreCase,
					create,
					bootOrCreate,
					strictGetValue,
					fetchSize,
					maxFetchSize,
					autoCommit,
					enlist
				};
				return list.AsReadOnly();
			}
		}

		[DisplayName("Data Source")]
		[RefreshProperties(RefreshProperties.All)]
		public string DataSource {
			get {
				string dataSource = Host;
				if (Port > 0 && Port != DefaultPort)
					dataSource += ":" + Port;

				return dataSource;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");

				base["Data Source"] = value;

				int index = value.IndexOf(':');
				if (index != -1) {
					Port = Int32.Parse(value.Substring(index));
					value = value.Substring(0, index);
				}

				Host = value;
			}
		}

		[DisplayName("Host Address")]
		[RefreshProperties(RefreshProperties.All)]
		public string Host {
			get { return host; }
			set {
				host = value;
				base[HostKey] = value;
			}
		}

		[DisplayName("Server Port")]
		[RefreshProperties(RefreshProperties.All)]
		public int Port {
			get { return port; }
			set {
				port = value;
				base[PortKey] = value;
			}
		}

		[DisplayName("Database Name")]
		[RefreshProperties(RefreshProperties.All)]
		public string Database {
			get { return database; }
			set {
				base[DatabaseKey] = value;
				database = value;
			}
		}

		[DisplayName("Schema")]
		[RefreshProperties(RefreshProperties.All)]
		public string Schema {
			get { return schema; }
			set {
				base[SchemaKey] = value;
				schema = value;
			}
		}

		[DisplayName("User Name")]
		[RefreshProperties(RefreshProperties.All)]
		public string UserName {
			get { return userName; }
			set {
				base[UserNameKey] = value;
				userName = value;
			}
		}

		[DisplayName("User Password")]
		[PasswordPropertyText(true)]
		[RefreshProperties(RefreshProperties.All)]
		public string Password {
			get { return password; }
			set {
				base[PasswordKey] = value;
				password = value;
			}
		}

		[DisplayName("Persist Security Info")]
		[RefreshProperties(RefreshProperties.All)]
		public bool PersistSecurityInfo {
			get { return persistSecurityInfo; }
			set {
				base[PersistSecurityInfoKey] = value;
				persistSecurityInfo = value;
			}
		}

		[DisplayName("Verbose Column Names")]
		[RefreshProperties(RefreshProperties.All)]
		public bool VerboseColumnNames {
			get { return verboseColumnNames; }
			set {
				base[VerboseColumnNamesKey] = value;
				verboseColumnNames = value;
			}
		}

		[DisplayName("Parameter Style")]
		[RefreshProperties(RefreshProperties.All)]
		public QueryParameterStyle ParameterStyle {
			get { return paramStyle; }
			set {
				base[ParameterStyleKey] = value;
				paramStyle = value;
			}
		}

		[DisplayName("Data Path")]
		[RefreshProperties(RefreshProperties.All)]
		public string Path {
			get { return path; }
			set {
				base[PathKey] = value;
				path = value;
			}
		}

		[DisplayName("Row Cache Size")]
		[RefreshProperties(RefreshProperties.All)]
		public int RowCacheSize {
			get { return rowCacheSize; }
			set {
				base[RowCacheSizeKey] = value;
				rowCacheSize = value;
			}
		}

		[DisplayName("Max Cache Size")]
		[RefreshProperties(RefreshProperties.All)]
		public int MaxCacheSize {
			get { return maxCacheSize; }
			set {
				base[MaxCacheSizeKey] = value;
				maxCacheSize = value;
			}
		}

		[DisplayName("Query Timeout")]
		[RefreshProperties(RefreshProperties.All)]
		public int QueryTimeout {
			get { return queryTimeout; }
			set {
				base[QueryTimeoutKey] = value;
				queryTimeout = value;
			}
		}

		[DisplayName("Ignore Identifiers Case")]
		[RefreshProperties(RefreshProperties.All)]
		public bool IgnoreIdentifiersCase {
			get { return ignoreCase; }
			set {
				base[IgnoreIdentifiersCaseKey] = value;
				ignoreCase = value;
			}
		}

		[DisplayName("Create New Database")]
		[RefreshProperties(RefreshProperties.All)]
		public bool Create {
			get { return create; }
			set {
				base[CreateKey] = value;
				create = value;
			}
		}

		[DisplayName("Boot/Create Database")]
		[RefreshProperties(RefreshProperties.All)]
		public bool BootOrCreate {
			get { return bootOrCreate; }
			set {
				base[BootOrCreateKey] = value;
				bootOrCreate = value;
			}
		}

		[DisplayName("Strict GetValue")]
		[RefreshProperties(RefreshProperties.All)]
		public bool StrictGetValue {
			get { return strictGetValue; }
			set {
				base[StrictGetValueKey] = value;
				strictGetValue = value;
			}
		}

		[DisplayName("Fetch Size")]
		[RefreshProperties(RefreshProperties.All)]
		public int FetchSize {
			get { return fetchSize; }
			set {
				base[FetchSizeKey] = value;
				fetchSize = value;
			}
		}

		[DisplayName("Max Fetch Size")]
		[RefreshProperties(RefreshProperties.All)]
		public int MaxFetchSize {
			get { return maxFetchSize; }
			set {
				base[MaxFetchSizeKey] = value;
				maxFetchSize = value;
			}
		}

		[DisplayName("Auto-Commit")]
		[RefreshProperties(RefreshProperties.All)]
		public bool AutoCommit {
			get { return autoCommit; }
			set {
				base[AutoCommitKey] = value;
				autoCommit = value;
			}
		}

		[DisplayName("Enlist")]
		[RefreshProperties(RefreshProperties.All)]
		public bool Enlist {
			get { return enlist; }
			set {
				base[EnlistKey] = value;
				enlist = value;
			}
		}

		private void InitToDefault() {
			host = DefaultHost;
			port = DefaultPort;
			database = DefaultDatabase;
			userName = DefaultUserName;
			password = DefaultPassword;
			schema = DefaultSchema;
			path = DefaultPath;
			paramStyle = DefaultParameterStyle;
			verboseColumnNames = DefaultVerboseColumnName;
			persistSecurityInfo = DefaultPersistSecurityInfo;
			rowCacheSize = DefaultRowCacheSize;
			maxCacheSize = DefaultMaxCacheSize;
			queryTimeout = DefaultQueryTimeout;
			ignoreCase = DefaultIgnoreIdentifiersCase;
			create = DefaultCreate;
			bootOrCreate = DefaultBootOrCreate;
			strictGetValue = DefaultStrictGetValue;
			fetchSize = DefaultFetchSize;
			maxFetchSize = DefaultMaxFetchSize;
			autoCommit = DefaultAutoCommit;
			enlist = DefaultEnlist;
		}

		private void SetValue(string key, object value) {
			if (key == null)
				throw new ArgumentNullException("key");

			key = MappKey(key);

			switch (key) {
				case HostKey: {
						if (value == null) {
							host = DefaultHost;
							base.Remove(key);
						} else {
							Host = value.ToString();
						}
						break;
					}
				case PortKey:
					if (value == null) {
						port = DefaultPort;
						base.Remove(key);
					} else {
						Port = ToInt32(value);
					}
					break;
				case DatabaseKey:
					if (value == null) {
						database = DefaultDatabase;
						base.Remove(key);
					} else {
						Database = value.ToString();
					}
					break;
				case SchemaKey:
					if (value == null) {
						schema = DefaultSchema;
						base.Remove(key);
					} else {
						Schema = value.ToString();
					}
					break;
				case PathKey:
					if (value == null) {
						path = DefaultPath;
						base.Remove(key);
					} else {
						Path = value.ToString();
					}
					break;
				case UserNameKey:
					if (value == null) {
						userName = DefaultUserName;
						base.Remove(key);
					} else {
						UserName = value.ToString();
					}
					break;
				case PasswordKey:
					if (value == null) {
						password = DefaultPassword;
						base.Remove(key);
					} else {
						Password = value.ToString();
					}
					break;
				case PersistSecurityInfoKey:
					if (value == null) {
						persistSecurityInfo = DefaultPersistSecurityInfo;
						base.Remove(key);
					} else {
						PersistSecurityInfo = ToBoolean(value);
					}
					break;
				case VerboseColumnNamesKey:
					if (value == null) {
						verboseColumnNames = DefaultVerboseColumnName;
						base.Remove(key);
					} else {
						VerboseColumnNames = ToBoolean(value);
					}
					break;
				case ParameterStyleKey:
					if (value == null) {
						paramStyle = DefaultParameterStyle;
						base.Remove(key);
					} else if (value is string) {
						ParameterStyle = (QueryParameterStyle)Enum.Parse(typeof(QueryParameterStyle), (string)value, true);
					} else if (value is int ||
							   value is QueryParameterStyle) {
						ParameterStyle = (QueryParameterStyle)value;
					}
					break;
				case RowCacheSizeKey:
					if (value == null) {
						rowCacheSize = DefaultRowCacheSize;
						base.Remove(key);
					} else {
						RowCacheSize = ToInt32(value);
					}
					break;
				case MaxCacheSizeKey:
					if (value == null) {
						maxCacheSize = DefaultMaxCacheSize;
						base.Remove(key);
					} else {
						MaxCacheSize = ToInt32(value);
					}
					break;
				case QueryTimeoutKey:
					if (value == null) {
						queryTimeout = DefaultQueryTimeout;
						base.Remove(key);
					} else {
						QueryTimeout = ToInt32(value);
					}
					break;
				case IgnoreIdentifiersCaseKey:
					if (value == null) {
						ignoreCase = DefaultIgnoreIdentifiersCase;
						base.Remove(key);
					} else {
						IgnoreIdentifiersCase = ToBoolean(value);
					}
					break;
				case CreateKey:
					if (value == null) {
						create = DefaultCreate;
						base.Remove(key);
					} else {
						Create = ToBoolean(value);
					}
					break;
				case BootOrCreateKey:
					if (value == null) {
						bootOrCreate = DefaultBootOrCreate;
						base.Remove(key);
					} else {
						BootOrCreate = ToBoolean(value);
					}
					break;
				case StrictGetValueKey:
					if (value == null) {
						strictGetValue = DefaultStrictGetValue;
						base.Remove(key);
					} else {
						StrictGetValue = ToBoolean(value);
					}
					break;
				case "DataSource":
					if (value == null) {

					} else {
						string s = value.ToString();
						int index = s.IndexOf(':');
						if (index != -1) {
							string sPort = s.Substring(index + 1);
							Host = s.Substring(0, index);
							Port = Int32.Parse(sPort);
						} else {
							Host = s;
						}
					}
					break;
				case FetchSizeKey:
					if (value == null) {
						fetchSize = DefaultFetchSize;
						base.Remove(FetchSizeKey);
					} else {
						FetchSize = ToInt32(value);
					}
					break;
				case MaxFetchSizeKey:
					if (value == null) {
						maxFetchSize = DefaultMaxFetchSize;
						base.Remove(MaxFetchSizeKey);
					} else {
						MaxFetchSize = ToInt32(value);
					}
					break;
				case AutoCommitKey:
					if (value == null) {
						autoCommit = DefaultAutoCommit;
						base.Remove(AutoCommitKey);
					} else {
						AutoCommit = ToBoolean(value);
					}
					break;
				case EnlistKey:
					if (value == null) {
						enlist = DefaultEnlist;
						base.Remove(EnlistKey);
					} else {
						Enlist = ToBoolean(value);
					}
					break;
				default:
					//TODO: support additional parameters for Boot/Create process...
					throw new ArgumentException("Key '" + key + "' is not recognized.", "key");
			}
		}

		private string MappKey(string key) {
			// this is a special case for DataSource key, that is atipical
			if (String.Equals(key, "DataSource", StringComparison.InvariantCultureIgnoreCase))
				return "DataSource";

			string outKey;
			if (!keymaps.TryGetValue(key, out outKey))
				throw new ArgumentException("The connection string keyword is not supported: " + key);
			return outKey;
		}

		private static bool ToBoolean(object value) {
			if (value == null)
				throw new ArgumentException();

			if (value is bool)
				return (bool)value;

			if (value is string) {
				string s = value.ToString().ToUpper();
				if (s == "YES" || s == "ENABLED" ||
					s == "TRUE" || s == "ON")
					return true;
				if (s == "NO" || s == "DISABLED" ||
					s == "FALSE" || s == "OFF")
					return false;
			}

			if (value is IConvertible)
				return (value as IConvertible).ToBoolean(CultureInfo.InvariantCulture);

			throw new ArgumentException();
		}

		private static int ToInt32(object value) {
			if (value == null)
				throw new ArgumentException();

			if (value is int)
				return (int)value;

			if (value is string) {
				string s = value.ToString();
				return Int32.Parse(s, CultureInfo.InvariantCulture);
			}

			if (value is IConvertible)
				return (value as IConvertible).ToInt32(CultureInfo.InvariantCulture);

			throw new ArgumentException();
		}

		public override void Clear() {
			base.Clear();
			InitToDefault();
		}

		public override bool ContainsKey(string keyword) {
			keyword = keyword.ToUpper().Trim();
			if (!keymaps.ContainsKey(keyword))
				return false;
			return base.ContainsKey(keymaps[keyword]);
		}

		public override bool Remove(string keyword) {
			if (!ContainsKey(keyword))
				return false;

			this[keyword] = null;
			return true;
		}

		public override bool ShouldSerialize(string keyword) {
			if (!ContainsKey(keyword))
				return false;

			keyword = keyword.ToUpper().Trim();
			string key = keymaps[keyword];
			if (persistSecurityInfo && key == PasswordKey)
				return false;

			return base.ShouldSerialize(key);
		}

		public override bool TryGetValue(string keyword, out object value) {
			if (!ContainsKey(keyword)) {
				value = null;
				return false;
			}

			return base.TryGetValue(keymaps[keyword.ToUpper().Trim()], out value);
		}
	}
}
