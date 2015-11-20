using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;

namespace Ruminations.Database.Generic {

	//=========================================================================
	// ResultSet Interface
	//=========================================================================
	public interface IDBResultSet {
	}

	//=========================================================================
	// DBConnection Interface
	//=========================================================================
	/// <summary>
	/// Common interface for running queries through different database engines.
	/// </summary>
    public interface IDBConnection {
		int RunCommand(string Query);
		DbDataReader RunQuery(string Query);
    }

	//=========================================================================
	// DBException Class
	//=========================================================================
	/// <summary>
	/// A generic exception class for this library's implementation of the database
	/// </summary>
	public class DBException : Exception {
		//---------------------------------------------------------------------
		// Error Messages
		//---------------------------------------------------------------------
		private static uint NUM_ERRORS = 3;
		private static uint UNKNOWN_ERROR = 0;
		public static uint CONNECTION_ERROR = 1;
		public static uint LOGIN_ERROR = 2;

		private static string[] ErrorMessages = { "Undefined exception",
			"Unable to connect to server",
			"Invalid username/password"
		};

		//---------------------------------------------------------------------
		// Members
		//---------------------------------------------------------------------
		protected uint _errorCode;

		public uint ErrorCode {
			get { return _errorCode; }
		}

		//---------------------------------------------------------------------
		// Constructors
		//---------------------------------------------------------------------
		DBException() : base() { _errorCode = 0; }
		DBException(string message) : base(message) { _errorCode = 0; }
		DBException(string message, Exception innerException) : base(message, innerException) { _errorCode = 0; }
	}

	//=========================================================================
	// DBConverter Static Class
	//=========================================================================
	/// <summary>
	/// Static class with a set of conversion functions for dealing with database.
	/// </summary>
	public static class DBConverter {
		public static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);	// The time of the UNIX epoch

		//---------------------------------------------------------------------
		// ToDateTimeString
		//---------------------------------------------------------------------
		/// <summary>
		/// Converts time in seconds since the UNIX epoch into a string with the date and time.
		/// </summary>
		/// <param name="UnixSeconds">Seconds since the UNIX epoch</param>
		/// <returns>Date and Time string with format: Y-m-d H:i:s</returns>
		public static string ToDateTimeString(double UnixSeconds) {
			return UnixEpoch.AddSeconds(UnixSeconds).ToString("Y-m-d H:i:s");
		}
	}
}
