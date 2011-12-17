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

using Deveel.Data.Caching;
using Deveel.Data.Control;
using Deveel.Data.Functions;
using Deveel.Data.Store;
using Deveel.Data.Text;
using Deveel.Diagnostics;

namespace Deveel.Data {
	/// <summary>
	/// A class that provides information and global functions for the 
	/// transaction layer in the engine. 
	/// </summary>
	/// <remarks>
	/// Shared information includes configuration details, logging, etc.
	/// </remarks>
	public class TransactionSystem : IDisposable {
		/// <summary>
		/// The stats object that keeps track of database statistics.
		/// </summary>
		private readonly Stats stats = new Stats();

		/// <summary>
		/// If this is set to true, during boot up the engine will reindex all the
		/// tables that weren't closed.  If false, the engine will only reindex the
		/// tables that have unchecked in modifications.
		/// </summary>
		private bool always_reindex_dirty_tables = false;

		/// <summary>
		/// The configuration properties of the entire database system.
		/// </summary>
		private DbConfig config = null;

		/// <summary>
		///  The DataCellCache that is a shared resource between on database's.
		/// </summary>
		private DataCellCache data_cell_cache = null;

		/// <summary>
		/// The path in the file system for the database files. 
		/// </summary>
		/// <remarks>
		/// Note that this will be null if the database does not exist in a local 
		/// file system.  For this reason it's best not to write code that relies 
		/// on the use of this value.
		/// </remarks>
		private string db_path;

		/// <summary>
		/// The dispatcher.
		/// </summary>
		private DatabaseDispatcher dispatcher;

		/// <summary>
		/// Set to true if the file handles should NOT be synchronized with the
		/// system file IO when the indices are written.  If this is true, then the
		/// database is not as fail safe, however File IO performance is improved.
		/// </summary>
		private bool dont_synch_filesystem = false;

		/// <summary>
		///  The list of FunctionFactory objects that handle different functions from SQL.
		/// </summary>
		private ArrayList function_factory_list;

		/// <summary>
		/// The IFunctionLookup object that can resolve a FunctionDef object to a
		/// IFunction object.
		/// </summary>
		private DSFunctionLookup function_lookup;

		/// <summary>
		/// Set to true if the parser should ignore case when searching for a schema,
		/// table or column using an identifier.
		/// </summary>
		private bool ignore_case_for_identifiers = false;

		/// <summary>
		/// The log directory.
		/// </summary>
		private string log_directory;

		/// <summary>
		/// Set to true if lookup comparison lists are enabled.
		/// </summary>
		private bool lookup_comparison_list_enabled = false;

		/// <summary>
		/// Set to true if the database is in Read only mode.  This is set from the
		/// configuration file.
		/// </summary>
		private bool read_only_access = false;

		/// <summary>
		/// The regular expression library bridge for the library we are configured
		/// to use.
		/// </summary>
		private IRegexLibrary regex_library;

		/// <summary>
		/// Set to false if there is conservative index memory storage.  If true,
		/// all root selectable schemes are stored behind a soft reference that will
		/// be garbage collected.
		/// </summary>
		private bool soft_index_storage = false;

		/// <summary>
		/// The underlying IStoreSystem implementation that encapsulates the behaviour
		/// for storing data persistantly.
		/// </summary>
		private IStoreSystem store_system;

		/// <summary>
		/// A logger to output any debugging messages.
		/// </summary>
		private IDebugLogger logger;

		// ---------- Low level row listeners ----------

		/// <summary>
		/// A list of table names and listeners that are notified of add and remove
		/// events in a table.
		/// </summary>
		private ArrayList table_listeners;

		/// <summary>
		/// Set to true if locking checks should be performed each time a table is 
		/// accessed.
		/// </summary>
		private bool table_lock_check = false;

		/// <summary>
		/// Transaction option, if this is true then a transaction error is generated
		/// during commit if a transaction selects data from a table that has
		/// committed changes to it during commit time.
		/// </summary>
		private bool transaction_error_on_dirty_select = true;


		///<summary>
		///</summary>
		public TransactionSystem() {
			// Setup generate properties from the environment.
			stats.Set(0, "OS.Platform: " + Environment.OSVersion.Platform);
			stats.Set(0, "OS.Version: " + Environment.OSVersion.VersionString);
			stats.Set(0, "Runtime.Version: " + Environment.Version);
			stats.Set(0, "Machine.ProcessorCount: " + Environment.ProcessorCount);
			table_listeners = new ArrayList();
		}

		~TransactionSystem() {
			Dispose(false);
		}

		/// <summary>
		/// Returns true if the database is in read only mode.
		/// </summary>
		/// <remarks>
		/// In read only mode, any 'write' operations are not permitted.
		/// </remarks>
		public bool ReadOnlyAccess {
			get { return read_only_access; }
		}

		/// <summary>
		/// Returns the path of the database in the local file system if the database
		/// exists within the local file system.
		/// </summary>
		/// <remarks>
		/// If the database is not within the local file system then null is returned. 
		/// It is recommended this method is not used unless for legacy or compatability 
		/// purposes.
		/// </remarks>
		public string DatabasePath {
			get { return db_path; }
		}

		/// <summary>
		/// Returns true if the database should perform checking of table locks.
		/// </summary>
		public bool TableLockingEnabled {
			get { return table_lock_check; }
		}

		/// <summary>
		/// Returns true if we should generate lookup caches in InsertSearch otherwise
		/// returns false.
		/// </summary>
		public bool LookupComparisonListEnabled {
			get { return lookup_comparison_list_enabled; }
		}

		///<summary>
		/// Returns true if all table indices are kept behind a soft reference that
		/// can be garbage collected.
		///</summary>
		public bool SoftIndexStorage {
			get { return soft_index_storage; }
		}

		///<summary>
		/// Returns the status of the 'always_reindex_dirty_tables' property.
		///</summary>
		public bool AlwaysReindexDirtyTables {
			get { return always_reindex_dirty_tables; }
		}

		///<summary>
		///  Returns true if we shouldn't synchronize with the file system when
		/// important indexing information is flushed to the disk.
		///</summary>
		public bool DontSynchFileSystem {
			get { return dont_synch_filesystem; }
		}

		///<summary>
		/// Returns true if during commit the engine should look for any selects
		/// on a modified table and fail if they are detected.
		///</summary>
		public bool TransactionErrorOnDirtySelect {
			get { return transaction_error_on_dirty_select; }
		}

		///<summary>
		/// Returns true if the parser should ignore case when searching for
		/// schema/table/column identifiers.
		///</summary>
		public bool IgnoreIdentifierCase {
			get { return ignore_case_for_identifiers; }
		}

		///<summary>
		/// Returns the regular expression library from the configuration file.
		///</summary>
		///<exception cref="ApplicationException"></exception>
		public IRegexLibrary RegexLibrary {
			get {
				if (regex_library != null) {
					return regex_library;
				}
				throw new ApplicationException("No regular expression library found in classpath " +
				                               "and/or in configuration file.");
			}
		}

		/// <summary>
		/// Gets an instance to the configurations set to the database system.
		/// </summary>
		internal DbConfig Config {
			get { return config; }
		}

		// ---------- Store System encapsulation ----------

		/// <summary>
		/// Returns the IStoreSystem encapsulation being used in this database.
		/// </summary>
		internal IStoreSystem StoreSystem {
			get { return store_system; }
		}

		// ---------- Debug logger methods ----------

		///<summary>
		/// Returns the IDebugLogger object that is used to log debug message. 
		///</summary>
		/// <remarks>
		/// This property must always return a debug logger that we can log to.
		/// </remarks>
		public IDebugLogger Debug {
			get { return logger; }
		}

		// ---------- Function factories ----------

		///<summary>
		/// Returns a <see cref="IFunctionLookup"/> object that will search through the 
		/// function factories in this database system and find and resolve a function.
		///</summary>
		/// <remarks>
		/// The returned object may throw an exception from the <see cref="FunctionDef.GetFunction"/> 
		/// method if the <see cref="FunctionDef"/> is invalid. For example, if the number 
		/// of parameters is incorrect or the name can not be found.
		/// </remarks>
		public IFunctionLookup FunctionLookup {
			get { return function_lookup; }
		}

		// ---------- System preparers ----------

		// ---------- Database System Statistics Methods ----------

		/// <summary>
		/// Returns a <see cref="Stats"/> object that can be used to keep 
		/// track of database statistics for this environment.
		/// </summary>
		public Stats Stats {
			get { return stats; }
		}

		// ---------- Log directory management ----------

		/// <summary>
		/// Gets or sets the current log directory or null if no logging 
		/// should occur.
		/// </summary>
		/// <remarks>
		/// Setting this should preferably be called during initialization.
		/// </remarks>
		public string LogDirectory {
			get { return log_directory; }
			set { log_directory = value; }
		}

		// ---------- Cache Methods ----------

		/// <summary>
		/// Returns a <see cref="DataCellCache"/> object that is a shared 
		/// resource between all database's running on this runtime.
		/// </summary>
		internal DataCellCache DataCellCache {
			get { return data_cell_cache; }
		}

		// ---------- Dispatch methods ----------

		/// <summary>
		/// Returns the <see cref="DatabaseDispatcher"/> object.
		/// </summary>
		private DatabaseDispatcher Dispatcher {
			get {
				lock (this) {
					if (dispatcher == null) {
						dispatcher = new DatabaseDispatcher(this);
					}
					return dispatcher;
				}
			}
		}

		//private void SetupFSync(IDbConfig dbConfig) {
		//    string fsyncTypeString = dbConfig.GetValue("fsync_type");
		//    if (fsyncTypeString != null) {
		//        // if the 'fsync_type' is set and the value is "default" we use 
		//        // the one retrieved automatically at the call of FSync class...
		//        if (String.Compare(fsyncTypeString, "default", true) == 0)
		//            return;

		//        Type type = Type.GetType(fsyncTypeString, false, true);
		//        if (type == null) {
		//            // there is no custom implementation of the fsync() operation.
		//            Debug.Write(DebugLevel.Warning, this,
		//                        "The value of 'fsync_type' is set but the type '" + fsyncTypeString + "' was not found.");
		//            return;
		//        }

		//        IFSync fsync = null;

		//        if (typeof(IFSync).IsAssignableFrom(type)) {
		//            try {
		//                fsync = (IFSync) Activator.CreateInstance(type, true);
		//            } catch (Exception e) {
		//                Debug.Write(DebugLevel.Warning, this,
		//                            "Error while initializing the fsynch handler class '" + fsyncTypeString + "': " + e.Message);
		//            }
		//        } else {
		//            // handle the case the type implements the Sync function but
		//            // doesn't implements the IFSync interface...
		//            try {
		//                fsync = FSync.Create(type);
		//            } catch (Exception) {
		//                Debug.Write(DebugLevel.Warning, this, "The provided type '" + type.FullName + "' does not implement the Sync(FileStream) method.");
		//            }
		//        }

		//        if (fsync != null)
		//            FSync.SetFSync(fsync);
		//    }
		//}

		/// <summary>
		/// Sets up the log file from the config information.
		/// </summary>
		private void SetupLog() {
			//// Conditions for not initializing a log directory;
			////  1. Read only access is enabled
			////  2. log_path is empty or not set

			string logPathString = config.LogPath;
			string root_path_var = config.GetValue("root_path");

			bool readOnly = config.ReadOnly;
			bool debugLogs = config.GetBooleanValue("debug_logs", true);

			if (debugLogs && !readOnly && !String.IsNullOrEmpty(logPathString)) {
				// First set up the debug information in this VM for the 'Debug' class.
				string logPath = config.ParseFileString(root_path_var, logPathString);
				// If the path doesn't exist the make it.
				if (!Directory.Exists(logPath))
					Directory.CreateDirectory(logPath);
				
				LogDirectory = logPath;
			}

			string loggerTypeString = config.GetValue("logger_type");

			Type loggerType = null;
			if (loggerTypeString != null) {
				loggerType = Type.GetType(loggerTypeString, false, true);
				if (!typeof(IDebugLogger).IsAssignableFrom(loggerType))
					loggerType = null;
			}

			// in case we don't log...
			if (readOnly || !debugLogs)
				loggerType = typeof (EmptyDebugLogger);

			if (loggerType == null)
				loggerType = typeof (DefaultDebugLogger);

			logger = (IDebugLogger) Activator.CreateInstance(loggerType, true);
			logger.Init(config);
		}

		/// <summary>
		/// Given a regular expression string representing a particular library, this
		/// will return the name of the type to use as a bridge between the library
		/// and the database engine.
		/// </summary>
		/// <returns>
		/// Returns null if the library name is invalid.
		/// </returns>
		private static String RegexStringToClass(String lib) {
			if (lib.Equals("System.RegEx")) {
				return "Deveel.Data.Text.SystemRegexLibrary";
			} else if (lib.Equals("Apache.RegEx")) {
				return "Deveel.Data.Text.ApacheRegexLibrary";
			} else if (lib.Equals("Gnu.RegEx")) {
				return "Deveel.Data.Text.GNURegexLibrary";
			} else {
				return null;
			}
		}

		/// <summary>
		/// Inits the <see cref="TransactionSystem"/> with the configuration 
		/// properties of the system.
		/// </summary>
		/// <param name="config"></param>
		/// <remarks>
		/// This can only be called once, and should be called at database boot time.
		/// </remarks>
		public virtual void Init(DbConfig config) {
			function_factory_list = new ArrayList();
			function_lookup = new DSFunctionLookup();

			if (config != null) {
				this.config = config;

				// Set the read_only property
				read_only_access = config.ReadOnly;

				// Setup the log
				SetupLog();

				// The storage encapsulation that has been configured.
				string storageSystem = config.GetStringValue(ConfigKeys.StorageSystem, "v1file");

				// Construct the system store.
				if (String.Compare(storageSystem, "v1file", true) == 0) {
					Debug.Write(DebugLevel.Message, this, "Storage System: v1 file storage mode.");
					store_system = new V1FileStoreSystem();
				} else if (String.Compare(storageSystem, "v1heap", true) == 0) {
					Debug.Write(DebugLevel.Message, this, "Storage System: v1 heap storage mode.");
					store_system = new V1HeapStoreSystem();
				} else {
					string error_msg = "Unknown storage_system property: " + storageSystem;

					Type storageSystemType = Type.GetType(storageSystem, false, true);
					if (storageSystemType == null || 
						!typeof(IStoreSystem).IsAssignableFrom(storageSystemType)) {
						Debug.Write(DebugLevel.Error, this, error_msg);
						throw new Exception(error_msg);
					}

					try {
						store_system = (IStoreSystem) Activator.CreateInstance(storageSystemType, true);
					} catch(Exception e) {
						error_msg = "Error initializing '" + storageSystemType.FullName + "': " + e.Message;
						Debug.Write(DebugLevel.Error, this, error_msg);
						throw new Exception(error_msg);
					}
				}

				// init the storage system
				store_system.Init(this);

				// Register the default function factory,
				AddFunctionFactory(FunctionFactory.Default);

				// Set up the DataCellCache from the values in the configuration
				int maxCacheSize = 0, max_cache_entry_size = 0;

				maxCacheSize = config.GetIntegerValue(ConfigKeys.DataCacheSize, 0);
				max_cache_entry_size = config.GetIntegerValue(ConfigKeys.MaxCacheEntrySize, 0);

				if (maxCacheSize >= 4096 &&
				    max_cache_entry_size >= 16 &&
				    max_cache_entry_size < (maxCacheSize/2)) {
					Debug.Write(DebugLevel.Message, this,"Internal Data Cache size:          " + maxCacheSize);
					Debug.Write(DebugLevel.Message, this,"Internal Data Cache max cell size: " + max_cache_entry_size);

					// Find a prime hash size depending on the size of the cache.
					int hash_size = DataCellCache.ClosestPrime(maxCacheSize/55);

					string cacheTypeString = config.GetStringValue("cache_type", "heap");
					Type cacheType = String.Compare(cacheTypeString, "heap", true) == 0
					            	? typeof(MemoryCache)
					            	: Type.GetType(cacheTypeString, false, true);

					if (cacheType == null)
						cacheType = typeof(MemoryCache);

					ICache cache;
					if (cacheType == typeof(MemoryCache)) {
						cache = new MemoryCache(hash_size, maxCacheSize, 20);
					} else {
						cache = (ICache) Activator.CreateInstance(cacheType, true);
					}

					// Set up the data_cell_cache
					data_cell_cache = new DataCellCache(this, cache, maxCacheSize, max_cache_entry_size, hash_size);
				} else {
					Debug.Write(DebugLevel.Message, this, "Internal Data Cache disabled.");
				}

				// Are lookup comparison lists enabled?
				//      lookup_comparison_list_enabled =
				//                            GetConfigBoolean("lookup_comparison_list", false);
				lookup_comparison_list_enabled = false;
				Debug.Write(DebugLevel.Message, this, "lookup_comparison_list = " + lookup_comparison_list_enabled);

				// Should we open the database in Read only mode?
				Debug.Write(DebugLevel.Message, this, "read_only = " + read_only_access);
				if (read_only_access) stats.Set(1, "DatabaseSystem.read_only");

				//      // Hard Sync file system whenever we update index files?
				//      if (is_file_store_mode) {
				//        dont_synch_filesystem = GetConfigBoolean("dont_synch_filesystem", false);
				//        Debug.Write(DebugLevel.Message, this,
				//                      "dont_synch_filesystem = " + dont_synch_filesystem);
				//      }

				// Generate transaction error if dirty selects are detected?
				transaction_error_on_dirty_select = config.GetBooleanValue(ConfigKeys.TransactionErrorOnDirtySelect, true);
				Debug.Write(DebugLevel.Message, this, "transaction_error_on_dirty_select = " + transaction_error_on_dirty_select);

				// Case insensitive identifiers?
				ignore_case_for_identifiers = config.GetBooleanValue(ConfigKeys.IgnoreIdentifiersCase, false);
				Debug.Write(DebugLevel.Message, this, "ignore_case_for_identifiers = " + ignore_case_for_identifiers);

				// What regular expression library are we using?
				// If we want the engine to support other regular expression libraries
				// then include the additional entries here.

				string regexBridge;
				string libUsed;
				string forceLib = config.GetStringValue("force_regex_library", null);

				// Are we forcing a particular regular expression library?
				if (forceLib != null) {
					libUsed = forceLib;
					// Convert the library string to a class name
					regexBridge = RegexStringToClass(forceLib);
				} else {
					string lib = config.GetStringValue(ConfigKeys.RegexLibrary, "Deveel.Data.Text.SystemRegexLibrary");
					libUsed = lib;
					// Convert the library string to a class name
					regexBridge = lib != null ? RegexStringToClass(lib) : "Deveel.Data.Text.SystemRegexLibrary";
				}

				if (regexBridge != null) {
					try {
						Type c = Type.GetType(regexBridge);
						regex_library = (IRegexLibrary) Activator.CreateInstance(c);
						Debug.Write(DebugLevel.Message, this, "Using regex bridge: " + libUsed);
					} catch (Exception e) {
						Debug.Write(DebugLevel.Error, this, "Unable to load regex bridge: " + regexBridge);
						Debug.WriteException(DebugLevel.Warning, e);
					}
				} else {
					if (libUsed != null)
						Debug.Write(DebugLevel.Error, this, "Regex library not known: " + libUsed);
					Debug.Write(DebugLevel.Message, this, "Regex features disabled.");
				}

				// ---------- Plug ins ---------

				try {
					// The 'function_factories' property.
					string functionFactories = config.GetStringValue("function_factories", null);
					if (functionFactories != null) {
						string[] factories = functionFactories.Split(';');
						for (int i = 0; i < factories.Length; ++i) {
							string factory_class = factories[i];
							Type c = Type.GetType(factory_class);
							FunctionFactory fun_factory = (FunctionFactory) Activator.CreateInstance(c);
							AddFunctionFactory(fun_factory);
							Debug.Write(DebugLevel.Message, this, "Successfully added function factory: " + factory_class);
						}
					} else {
						Debug.Write(DebugLevel.Message, this, "No 'function_factories' config property found.");
						// If resource missing, do nothing...
					}
				} catch (Exception e) {
					Debug.Write(DebugLevel.Error, this, "Error parsing 'function_factories' configuration property.");
					Debug.WriteException(e);
				}

				// Flush the contents of the function lookup object.
				FlushCachedFunctionLookup();
			}
		}

		///<summary>
		/// Registers a new FunctionFactory with the database system.
		///</summary>
		///<param name="factory"></param>
		/// <remarks>
		/// The function factories are used to resolve a function name into a IFunction object.
		/// Function factories are checked in the order they are added to the database system.
		/// </remarks>
		public void AddFunctionFactory(FunctionFactory factory) {
			lock (function_factory_list) {
				function_factory_list.Add(factory);
			}
			factory.Init();
		}

		///<summary>
		/// Flushes the <see cref="IFunctionLookup"/> object returned by the 
		/// <see cref="FunctionLookup"/> property.
		///</summary>
		/// <remarks>
		/// This should be called if the function factory list has been modified 
		/// in some way.
		/// </remarks>
		public void FlushCachedFunctionLookup() {
			FunctionFactory[] factories;
			lock (function_factory_list) {
				factories = (FunctionFactory[]) function_factory_list.ToArray(typeof (FunctionFactory));
			}
			function_lookup.flushContents(factories);
		}

		///<summary>
		/// Given a <see cref="DataConstraintInfo"/>, this will prepare 
		/// the expression and return a new prepared <see cref="DataConstraintInfo"/>.
		///</summary>
		///<param name="tableInfo"></param>
		///<param name="check"></param>
		/// <remarks>
		/// The default implementation of this is to do nothing.  However, a sub-class 
		/// of the system choose to prepare the expression, such as resolving the 
		/// functions via the function lookup, and resolving the sub-queries, etc.
		/// </remarks>
		///<returns></returns>
		public DataConstraintInfo PrepareTransactionCheckConstraint(DataTableInfo tableInfo, DataConstraintInfo check) {
			// Resolve the expression to this table and row and evaluate the
			// check constraint.
			
			tableInfo.ResolveColumns(IgnoreIdentifierCase, check.CheckExpression);

			return check;
		}


		/// <summary>
		///  Creates an event object that is passed into <see cref="PostEvent"/> 
		/// method to run the given <see cref="EventHandler"/> method after the 
		/// time has passed.
		/// </summary>
		/// <param name="callback"></param>
		/// <remarks>
		/// The event created here can be safely posted on the event queue as many
		/// times as you like.  It's useful to create an event as a persistant object
		/// to service some event.  Just post it on the dispatcher when you want
		/// it run!
		/// </remarks>
		/// <returns></returns>
		internal object CreateEvent(EventHandler callback) {
			return Dispatcher.CreateEvent(callback);
		}

		/// <summary>
		/// Adds a new event to be dispatched on the queue after 'time_to_wait'
		/// milliseconds has passed.
		/// </summary>
		/// <param name="timeToWait"></param>
		/// <param name="e">An event object returned by <see cref="CreateEvent"/>.</param>
		internal void PostEvent(int timeToWait, object e) {
			Dispatcher.PostEvent(timeToWait, e);
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				GC.SuppressFinalize(this);
				Dispose();
			}
		}

		public virtual void Dispose() {
			//if (buffer_manager != null) {
			//    try {
			//        // Set a check point
			//        store_system.SetCheckPoint();
			//        // Stop the buffer manager
			//        buffer_manager.Stop();
			//    } catch (IOException e) {
			//        Console.Out.WriteLine("Error stopping buffer manager.");
			//        Console.Out.Write(e.StackTrace);
			//    }
			//}
			//buffer_manager = null;
			if (store_system != null)
				store_system.Dispose();
			store_system = null;
			regex_library = null;
			data_cell_cache = null;
			config = null;
			log_directory = null;
			function_factory_list = null;
			if (dispatcher != null) {
				dispatcher.Finish();
			}
			//    trigger_manager = null;
			dispatcher = null;
			Debug.Dispose();
		}


		// ---------- Inner classes ----------

		#region Nested type: DSFunctionLookup

		/// <summary>
		/// A <see cref="IFunctionLookup"/> implementation that will look up a 
		/// function from a list of <see cref="FunctionFactory"/> objects 
		/// provided with.
		/// </summary>
		private sealed class DSFunctionLookup : IFunctionLookup {
			private FunctionFactory[] factories;

			#region IFunctionLookup Members

			public IFunction GenerateFunction(FunctionDef function_def) {
				lock (this) {
					for (int i = 0; i < factories.Length; ++i) {
						IFunction f = factories[i].GenerateFunction(function_def);
						if (f != null) {
							return f;
						}
					}
					return null;
				}
			}

			public bool IsAggregate(FunctionDef function_def) {
				lock (this) {
					for (int i = 0; i < factories.Length; ++i) {
						IFunctionInfo f_info =
							factories[i].GetFunctionInfo(function_def.Name);
						if (f_info != null) {
							return f_info.Type == FunctionType.Aggregate;
						}
					}
					return false;
				}
			}

			#endregion

			public void flushContents(FunctionFactory[] factories) {
				lock (this) {
					this.factories = factories;
				}
			}
		}

		#endregion
	}
}