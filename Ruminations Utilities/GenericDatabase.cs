using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;

namespace Ruminations.Database.Generic {

	// ResultSet Interface
	public interface IDBResultSet {

	}

	// DBConnection Interface
    public interface IDBConnection {
		int RunCommand(string Query);
		DbDataReader RunQuery(string Query);
    }

	public static class DBConverter {
		public static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static string ToDateTimeString(double UnixSeconds) {
			return UnixEpoch.AddSeconds(UnixSeconds).ToString("Y-m-d H:i:s");
		}
	}
}
