using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Ruminations.Database.Generic;
using System.Diagnostics;
using System.Data.Common;

namespace Ruminations.Database.MySQL {
	public class MySQLConnection : IDBConnection, IDisposable {
		#region Members
		private MySqlConnection _dbConnection;	// The connection to the MqSQL Database
		#endregion Members

		#region Constructors
		public MySQLConnection(string ConnectionString) {
			// Create the connection object
			_dbConnection = new MySqlConnection(ConnectionString);

			// Open the connection
			_dbConnection.Open();
		}

		public MySQLConnection(string Host, string Port, string Username, string Password, string InitialDB) {
			// Set the connection string
			string connString = "Host=" + Host + ";Port=" + Port + ";Database=" + InitialDB + ";Username=" + Username + ";Password=" + Password + ";ConvertZeroDateTime=True";

			// Create the connection
			_dbConnection = new MySqlConnection(connString);

			// Open the connection
			_dbConnection.Open();
		}

		public MySQLConnection(string Host, string Username, string Password, string InitialDB) : this(Host, "3306", Username, Password, InitialDB) { }
		#endregion Constructors

		#region Methods
		public int RunCommand(string Query) {
			// Send the command
			MySqlCommand cmd = new MySqlCommand(Query, _dbConnection);
			int result = cmd.ExecuteNonQuery();

			return result;
		}

		public DbDataReader RunQuery(string Query) {
			// Send the command
			MySqlCommand cmd = new MySqlCommand(Query, _dbConnection);
			MySqlDataReader result = cmd.ExecuteReader();

			return result;
		}

		public void Dispose() {
			// Close the connection and dispose it
			_dbConnection.Close();
			_dbConnection.Dispose();
		}
		#endregion Methods
	}
}
