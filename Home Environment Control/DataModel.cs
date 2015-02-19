using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ruminations.Database.Generic;
using System.Data.Common;

namespace Home_Environment_Control {
	#region Types
	[Flags]
	public enum DataTypes { None = 0, Time = 1, Temperature = 2, Luminosity = 4, Pressure = 8, Humidity = 16, Power = 32 }
	#endregion Types

	public class Radio {
		private string _name;
		private uint _dbId;
		private DataTypes _recordedData;

		#region Parameters
		public uint ID {
			get { return _dbId; }
		}

		public string Name {
			get { return _name; }
		}

		public DataTypes RecordedData {
			get { return _recordedData; }
			set { _recordedData = value; }
		}
		#endregion Parameters

		#region Methods
		public Radio() {
			_name = "";
			_dbId = 0;
			_recordedData = DataTypes.None;
		}

		public Radio(uint ID, string Name) {
			_name = Name;
			_dbId = ID;
			_recordedData = DataTypes.None;
		}

		public override string ToString() {
			return _name;
		}
		#endregion Methods
	}

	public struct RadioListItem {
		public DateTime assignTime;
		public Radio radio;

		#region Methods
		public RadioListItem(DateTime SetTime, Radio SetRadio) {
			assignTime = SetTime;
			radio = SetRadio;
		}

		public override string ToString() {
			return "Radio " + radio.ToString() + " installed at " + assignTime.ToString("s");
		}
		#endregion Methods
	}

	public class Location {
		private string _name;					// The name of the location
		private uint _dbId;						// The ID of the location in the database
		private DataTypes _recordedData;		// The enum of all types of sensor data collected at that location
		private LocationType _type;				// Identifies the type of location
		private List<RadioListItem> _radioList;	// The list of radios at that location

		#region Custom Types
		public enum LocationType { Undefined, Indoor, Outdoor, Control };
		#endregion Custom Types

		#region Parameters
		public string Name {
			get { return _name; }
		}

		public uint ID {
			get { return _dbId; }
		}

		public DataTypes RecordedData {
			get { return _recordedData; }
		}

		public LocationType Type {
			get { return _type; }
			set { _type = value; }
		}

		public List<Radio> Radios {
			get { 
				List<Radio> radioList = new List<Radio>();
				foreach(RadioListItem radioItem in _radioList) radioList.Add(radioItem.radio);
				return radioList;
			}
		}
		#endregion Parameters

		#region Methods
		public Location(uint ID, string Name) {
			_name = Name;
			_dbId = ID;
			_recordedData = DataTypes.None;
			_radioList = new List<RadioListItem>();
			_type = LocationType.Undefined;
		}

		public void AddRadio(Radio NewRadio, DateTime AssignTime) {
			// Create the struct and add it to the list
			RadioListItem newItem = new RadioListItem(AssignTime, NewRadio);
			_radioList.Add(newItem);

			// Update the recorded data
			_recordedData |= NewRadio.RecordedData;
		}

		public void AddRadio(RadioListItem NewRadioItem) {
			this.AddRadio(NewRadioItem.radio, NewRadioItem.assignTime);
		}

		public void Populate(IDBConnection DataSource) {
			// Create list of radios at this location
			string dbQuery = "SELECT * FROM radios WHERE location_id=" + _dbId + " ORDER BY assign_time ASC";
			using(DbDataReader radioList = DataSource.RunQuery(dbQuery)) {
				// Iterate through each radio
				while(radioList.Read()) {
					// Add the radio to the list
					Radio addRadio = new Radio((uint) radioList.GetInt32(0), radioList.GetString(1));
					this.AddRadio(addRadio, radioList.GetDateTime(3));
				}
			}

			// Determine the recorded data for this location - look at all radios prior to the last one
			string queryStart, queryEnd;
			string[] queryColumn = new string[] { "temperature", "luminosity", "pressure", "humidity", "power" };
			for(int i = 0; i < _radioList.Count - 1; i++) {	// Iterate through items up to last one
				queryStart = "SELECT COUNT(*) FROM measurements WHERE radio_id='" + _radioList[i].radio.Name + "' AND ";
				queryEnd = " IS NOT NULL AND measure_time >= '" + _radioList[i].assignTime.ToString("yyyy-MM-dd HH:mm:ss") + "' AND measure_time < '" + _radioList[i+1].assignTime.ToString("yyyy-MM-dd HH:mm:ss") + "'";

				// Iterate through each type of recorded data
				_radioList[i].radio.RecordedData |= DataTypes.Time;
				for(int j = 0; j < 5; j++) {
					// Query the database for the data
					dbQuery = queryStart + queryColumn[j] + queryEnd;
					using(DbDataReader columnCount = DataSource.RunQuery(dbQuery)) {
						columnCount.Read();
						if(columnCount.GetInt32(0) > 0) {
							// Increment the appropriate counter
							if(j == 0) _radioList[i].radio.RecordedData |= DataTypes.Temperature;
							else if(j == 1) _radioList[i].radio.RecordedData |= DataTypes.Luminosity;
							else if(j == 2) _radioList[i].radio.RecordedData |= DataTypes.Pressure;
							else if(j == 3) _radioList[i].radio.RecordedData |= DataTypes.Humidity;
							else _radioList[i].radio.RecordedData |= DataTypes.Power;
						}
					}
				}
				_recordedData |= _radioList[i].radio.RecordedData;
			}

			//  Determine the recorded data for the last location
			int listPos = _radioList.Count - 1;
			queryStart = "SELECT COUNT(*) FROM measurements WHERE radio_id='" + _radioList[listPos].radio.Name + "' AND ";
			queryEnd = " IS NOT NULL AND measure_time >= '" + _radioList[listPos].assignTime.ToString("yyyy-MM-dd HH:mm:ss") + "'";

			// Iterate through each type of recorded data
			_radioList[listPos].radio.RecordedData |= DataTypes.Time;
			for(int j = 0; j < 5; j++) {
				// Query the database for the data
				dbQuery = queryStart + queryColumn[j] + queryEnd;
				using(DbDataReader columnCount = DataSource.RunQuery(dbQuery)) {
					columnCount.Read();
					if(columnCount.GetInt32(0) > 0) {
						// Increment the appropriate counter
						if(j == 0) _radioList[listPos].radio.RecordedData |= DataTypes.Temperature;
						else if(j == 1) _radioList[listPos].radio.RecordedData |= DataTypes.Luminosity;
						else if(j == 2) _radioList[listPos].radio.RecordedData |= DataTypes.Pressure;
						else if(j == 3) _radioList[listPos].radio.RecordedData |= DataTypes.Humidity;
						else _radioList[listPos].radio.RecordedData |= DataTypes.Power;
					}
				}
			}
			_recordedData |= _radioList[listPos].radio.RecordedData;
		}

		public override string ToString() {
			return _name;
		}
		#endregion Methods
	}

	public class Network {
		private List<Location> _locations;	// Array of locations in the network

		#region Parameters
		public List<Location> LocationList {
			get { return _locations; }
		}
		#endregion Parameters

		#region Methods
		public Network() {
			// Create the lists
			_locations = new List<Location>();
		}

		public void Populate(IDBConnection DataSource) {
			// Get a list of locations from the database
			string dbQuery = "SELECT * FROM locations";
			using(DbDataReader locationList = DataSource.RunQuery(dbQuery)) {
				while(locationList.Read()) {
					// Create the object
					Location addLocation = new Location((uint) locationList.GetInt32(0), locationList.GetString(1));

					// Get the type
					// To be completed

					// Add to the list
					_locations.Add(addLocation);
				}
			}

			// Populate the location with radio data
			foreach(Location curLocation in _locations) {
				curLocation.Populate(DataSource);
			}
		}
		#endregion Methods
	}
}
