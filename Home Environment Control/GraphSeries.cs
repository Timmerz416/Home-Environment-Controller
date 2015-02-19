using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot;

using Ruminations.Database.Generic;
using System.Data.Common;

namespace Home_Environment_Control {
	public interface ISeriesRange {
		string MinimumString();
		string MaximumString();
		double GetMinimum();
		double GetMaximum();
		void SetMinimum(double Min);
		void SetMaximum(double Max);
	}

	public class DoubleRange : ISeriesRange {
		double _min;
		double _max;

		#region Parameters
		public double Min {
			get { return _min; }
			set { _min = value; }
		}

		public double Max {
			get { return _max; }
			set { _max = value; }
		}
		#endregion Parameters

		#region Methods
		public DoubleRange() {
			_min = 0;
			_max = 1;
		}

		public DoubleRange(double Minimum, double Maximum) {
			_min = Minimum;
			_max = Maximum;
		}

		public string MinimumString() {
			return _min.ToString();
		}

		public string MaximumString() {
			return _max.ToString();
		}

		public double GetMinimum() {
			return _min;
		}

		public double GetMaximum() {
			return _max;
		}

		public void SetMinimum(double Min) {
			_min = Min;
		}

		public void SetMaximum(double Max) {
			_max = Max;
		}
		#endregion Methods
	}

	public class DateTimeRange : ISeriesRange {
		DateTime _min;
		DateTime _max;

		#region Parameters
		public DateTime Min {
			get { return _min; }
			set { _min = value; }
		}

		public DateTime Max {
			get { return _max; }
			set { _max = value; }
		}
		#endregion Parameters

		#region Methods
		public DateTimeRange() {
			// Default to the last 24 hour period
			_min = DateTime.Now.Subtract(new TimeSpan(24, 0, 0));
			_max = DateTime.Now;
		}

		public DateTimeRange(DateTime Minimum, DateTime Maximum) {
			// Need to set the times in terms of seconds since the UNIX epoch to work with MySQL
			_min = Minimum;
			_max = Maximum;
		}

		public string MinimumString() {
			return _min.ToString("yyyy-MM-dd HH:mm:ss");
		}

		public string MaximumString() {
			return _max.ToString("yyyy-MM-dd HH:mm:ss");
		}

		public double GetMinimum() {
			return DateTimeAxis.ToDouble(_min);
		}

		public double GetMaximum() {
			return DateTimeAxis.ToDouble(_max);
		}

		public void SetMinimum(double Min) {
			_min = DateTimeAxis.ToDateTime(Min);
		}

		public void SetMaximum(double Max) {
			_max = DateTimeAxis.ToDateTime(Max);
		}
		#endregion Methods
	}

	public class BasicPlotSeries : LineSeries {
		private Location _source;
		private DataTypes _xAxisType;
		private DataTypes _yAxisType;

		#region Parameters
		public Location Source {
			get { return _source; }
			set { _source = value; }
		}

		public DataTypes XAxisType {
			get { return _xAxisType; }
			set { _xAxisType = value; }
		}

		public DataTypes YAxisType {
			get { return _yAxisType; }
			set { _yAxisType = value; }
		}
		#endregion Parameters

		#region Methods
		public BasicPlotSeries() {
			// Set the data to nothing
			_source = null;
			_xAxisType = _yAxisType = DataTypes.None;
		}

		public BasicPlotSeries(Location Source, DataTypes XAxis, DataTypes YAxis) {
			// Set the members
			_source = Source;
			_xAxisType = XAxis;
			_yAxisType = YAxis;
		}

		public virtual void SetGraphData(IDBConnection DataSource, ISeriesRange TimeRange) {
			// Get the x-axis data to query over
			string xLabel = getSQLTypeString(_xAxisType);
			string queryBase = "SELECT " + xLabel + ", " + getSQLTypeString(_yAxisType) + " FROM measurements WHERE measure_time >= '" + TimeRange.MinimumString() + "' AND measure_time <= '" + TimeRange.MaximumString() + "'";

			// Iterate through each radio
			foreach(Radio radio in _source.Radios) {
				string dbQuery = queryBase + " AND radio_id='" + radio.Name + "' ORDER BY " + xLabel + " ASC";

				// Run the query and fill the data
				using(DbDataReader result = DataSource.RunQuery(dbQuery)) {
					// Delete current data
					Points.Clear();

					// Iterate through all the results
					while(result.Read()) {
						// Create the point and add through the virtual method
						DataPoint newPoint = new DataPoint(_xAxisType == DataTypes.Time ? OxyPlot.Axes.DateTimeAxis.ToDouble(result.GetDateTime(0)) : result.GetDouble(0), result.GetDouble(1));
						addPoint(newPoint);
					}
				}
			}
		}

		protected virtual void addPoint(DataPoint NewPoint) {
			// Check to see if any points exist
			if(Points.Count == 0) {	// Initilize the dataset limits
				MinX = MaxX = NewPoint.X;
				MinY = MaxY = NewPoint.Y;
			} else {	// Evaluate for new limits
				if(NewPoint.X < MinX) MinX = NewPoint.X;
				else if(NewPoint.X > MaxX) MaxX = NewPoint.X;

				if(NewPoint.Y < MinY) MinY = NewPoint.Y;
				else if(NewPoint.Y > MaxY) MaxY = NewPoint.Y;
			}

			// Simply add the point to the list of points
			Points.Add(NewPoint);
		}

		private string getSQLTypeString(DataTypes DBType) {
			// Check that a none value was not passed
			if(DBType == DataTypes.None) throw new Exception("Cannot extract 'None' type data from the database.");

			// All types are based on name except time
			if(DBType == DataTypes.Time) return "measure_time";
			else return Enum.GetName(typeof(DataTypes), DBType).ToLower();
		}
		#endregion Methods
	}
}
