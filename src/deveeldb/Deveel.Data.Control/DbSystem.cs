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
using System.Data;
using System.Text;

using Deveel.Data.Client;
using Deveel.Data.Configuration;
using Deveel.Data.DbSystem;
using Deveel.Data.Protocol;
using Deveel.Data.Security;

namespace Deveel.Data.Control {
	///<summary>
	/// An object used to access and control a single database system running 
	/// in the current runtime.
	///</summary>
	/// <remarks>
	/// This object provides various access methods to safely manipulate the 
	/// database, as well as allowing server plug-ins.  For example, a TCP/IP 
	/// server component might be plugged into this object to open the database 
	/// to remote access.
	/// </remarks>
	public sealed class DbSystem : IDisposable {
		/// <summary>
		/// The DbController object.
		/// </summary>
		private DbController controller;

		/// <summary>
		/// The name of the database referenced.
		/// </summary>
		private readonly string name;

		/// <summary>
		/// The <see cref="DbConfig"/> object that describes the startup configuration 
		/// of the database.
		/// </summary>
		private IDbConfig config;

		/// <summary>
		/// The underlying <see cref="Deveel.Data.DbSystem.Database"/> object of this system.
		/// </summary>
		/// <remarks>
		/// This object gives low level access to the system.
		/// </remarks>
		private readonly Database database;

		/// <summary>
		/// An internal counter for internal connections created on this system.
		/// </summary>
		private int internalCounter;

		/// <summary>
		/// A collection of all the connections opened by the system.
		/// </summary>
		private Hashtable connections;


		internal DbSystem(DbController controller, string name, IDbConfig config, Database database) {
			this.name = name;
			this.controller = controller;
			this.config = config;
			this.database = database;
			internalCounter = 0;

			// Register the shut down delegate,
			database.RegisterShutDownDelegate(new EventHandler(Shutdown));

			// Enable commands to the database system...
			database.SetIsExecutingCommands(true);
		}

		/// <summary>
		/// Desctructor of the class <see cref="DbSystem"/>.
		/// </summary>
		~DbSystem() {
			Dispose(false);
		}

		private void Shutdown(object sender, EventArgs e) {
			// InternalDispose();
			Dispose();
		}

		///<summary>
		/// Returns an immutable version of the database system configuration.
		///</summary>
		public IDbConfig Config {
			get { return config; }
		}

		// ---------- Internal access methods ----------

		///<summary>
		/// Returns the <see cref="Database"/> object for this control that can be used
		/// to access the database system at a low level.
		///</summary>
		/// <remarks>
		/// This property only works correctly if the database engine has successfully
		/// been initialized.
		/// <para>
		/// This object is generally not very useful unless you intend to perform some 
		/// sort of low level function on the database.  This object can be used to bypass 
		/// the SQL layer and talk directly with the internals of the database.
		/// </para>
		/// </remarks>
		public Database Database {
			get { return database; }
		}

		///<summary>
		/// Makes a connection to the database and returns a <see cref="IDbConnection"/> 
		/// object that can be used to execute queries on the database.
		///</summary>
		///<param name="schema">The initial database schema to start the connection in.</param>
		///<param name="username">The user to login to the database under.</param>
		///<param name="password">The password of the user.</param>
		/// <remarks>
		/// This is a standard connection that talks directly with the database without 
		/// having to go through any communication protocol layers.
		/// <para>
		/// For example, if this control is for a database server, the <see cref="IDbConnection"/>
		/// returned here does not go through the TCP/IP connection.  For this reason certain database 
		/// configuration constraints (such as number of concurrent connection on the database) may not 
		/// apply to this connection.
		/// </para>
		/// </remarks>
		///<returns>
		/// Returns a <see cref="IDbConnection"/> instance used to access the database.
		/// </returns>
		/// <exception cref="DataException">
		/// Thrown if the login fails with the credentials given.
		/// </exception>
		public IDbConnection GetConnection(string schema, string username, string password) {
			// Create the host string, formatted as 'Internal/[hash number]/[counter]'
			StringBuilder buf = new StringBuilder();
			buf.Append("Internal/");
			buf.Append(GetHashCode());
			buf.Append('/');
			lock (this) {
				buf.Append(internalCounter);
				++internalCounter;
			}

			string hostString = buf.ToString();

			// Create the database interface for an internal database connection.
			IDatabaseInterface dbInterface = new DatabaseInterface(controller, name, hostString);
			// Create the DeveelDbConnection object (very minimal cache settings for an
			// internal connection).
			var s = new DeveelDbConnectionStringBuilder();
			s.Schema = schema;
			s.UserName = username;
			s.Password = password;

			var connection = new DBSConnection(this, internalCounter, s.ToString(), dbInterface, 8, 4092000);
			// Attempt to log in with the given username and password (default schema)
			connection.Open();
			if (connection.State != ConnectionState.Open)
				throw new InvalidOperationException("Unable to open the connection.");

			// And return the new connection
			return connection;
		}

		///<summary>
		/// Makes a connection to the database and returns a <see cref="IDbConnection"/> 
		/// object that can be used to execute queries on the database.
		///</summary>
		///<param name="username">The user to login to the database under.</param>
		///<param name="password">The password of the user.</param>
		/// <remarks>
		/// This is a standard connection that talks directly with the database without 
		/// having to go through any communication protocol layers.
		/// <para>
		/// For example, if this control is for a database server, the <see cref="IDbConnection"/>
		/// returned here does not go through the TCP/IP connection.  For this reason certain database 
		/// configuration constraints (such as number of concurrent connection on the database) may not 
		/// apply to this connection.
		/// </para>
		/// </remarks>
		///<returns>
		/// Returns a <see cref="IDbConnection"/> instance used to access the database.
		/// </returns>
		/// <exception cref="DataException">
		/// Thrown if the login fails with the credentials given.
		/// </exception>
		public IDbConnection GetConnection(String username, String password) {
			return GetConnection(null, username, password);
		}

		// ---------- Global methods ----------

		///<summary>
		/// Sets a flag that causes the database to delete itself from the file 
		/// system when it is shut down.
		///</summary>
		///<param name="status"></param>
		/// <remarks>
		/// This is useful if an application needs a temporary database to work 
		/// with that is released from the file system when the application ends.
		/// <para>
		/// By default, a database is not deleted from the file system when it is
		/// closed.
		/// </para>
		/// <para>
		/// <b>Note</b>: Use with care - setting this flag will cause all data stored 
		/// in the database to be lost when the database is shut down.
		/// </para>
		/// </remarks>
		public void SetDeleteOnClose(bool status) {
			database.SetDeleteOnShutdown(status);
		}

		///<summary>
		/// Closes this database system so it is no longer able to process queries.
		///</summary>
		/// <remarks>
		/// A database may be shut down either through this method or by executing a 
		/// command that shuts the system down (for example, <c>SHUTDOWN</c>).
		/// <para>
		/// When a database system is closed, it is not able to be restarted again
		/// unless a new <see cref="DbSystem"/> object is obtained from the <see cref="DbController"/>.
		/// </para>
		/// <para>
		/// This method also disposes all resources associated with the database 
		/// system (such as threads, etc) so that it may be reclaimed by the garbage 
		/// collector.
		/// </para>
		/// <para>
		/// When this method returns this object is no longer usable.
		/// </para>
		/// </remarks>
		public void Close() {
			if (database != null) {
				database.StartShutDownThread();
				database.WaitUntilShutdown();
			}
		}

		// ---------- Private methods ----------

		/// <summary>
		/// Disposes of all the resources associated with this system.
		/// </summary>
		/// <remarks>
		/// It may only be called from the shutdown delegate registered 
		/// in the constructor.
		/// </remarks>
		private void InternalDispose() {
			if (connections != null && connections.Count > 0) {
				ArrayList list = new ArrayList(connections.Keys);
				for (int i = list.Count - 1; i >= 0; i--) {
					int id = (int) list[i];
					IDbConnection connection = connections[id] as IDbConnection;

					try {
						if (connection != null)
							connection.Dispose();
					} catch(Exception) {
						// we ignore this ...
					}
				}
			}

			connections = null;
			controller = null;
			config = null;
		}

		private class DBSConnection : DeveelDbConnection {
			internal DBSConnection(DbSystem system, int id, string connectionString, IDatabaseInterface db_interface, int cache_size, int max_size)
				: base(connectionString, db_interface, cache_size, max_size) {
				this.system = system;
				this.id = id;
			}

			private readonly int id;
			private readonly DbSystem system;

			internal override bool InternalOpen() {
				if (base.InternalOpen()) {
					if (system.connections == null)
						system.connections = new Hashtable();
					system.connections.Add(id, this);
					return true;
				}
				return false;
			}

			internal override bool InternalClose() {
				if (base.InternalClose()) {
					system.connections.Remove(id);
					return true;
				}

				return false;
			}
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				InternalDispose();
			}
		}

		/// <inheritdoc/> 
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}