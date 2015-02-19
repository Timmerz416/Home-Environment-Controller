using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System.Diagnostics;

using Ruminations.Database.Generic;

namespace Home_Environment_Control {
	public abstract class AxisRange {
		private bool _autoScale;
		protected ISeriesRange _range;

		#region Parameters
		public bool IsAuto {
			get { return _autoScale; }
			set { _autoScale = value; }
		}

		public double PlotMaximum {
			get { return _range.GetMaximum(); }
			set { _range.SetMaximum(value); }
		}

		public double PlotMinimum {
			get { return _range.GetMinimum(); }
			set { _range.SetMinimum(value); }
		}
		#endregion Parameters

		#region Methods
		public AxisRange() {
			// By default, the scale is set to autoscale;
			_autoScale = true;
		}

		public AxisRange(bool AutoScale) {
			// Set the autscale behaviour
			_autoScale = AutoScale;
		}
		#endregion Methods
	}

	public class DoubleAxisRange : AxisRange {
		#region Methods
		public DoubleAxisRange() : base() {
			// Autoscale by default, set default range
			_range = new DoubleRange();
		}

		public DoubleAxisRange(double Minimum, double Maximum) : base(false) {
			// Set the limits
			_range = new DoubleRange(Minimum, Maximum);
		}
		#endregion Methods
	}

	public class TimeAxisRange : AxisRange {
		#region Parameters
		public DateTime Minimum {
			get { return ((DateTimeRange)_range).Min; }
			set { ((DateTimeRange)_range).Min = value; }
		}

		public DateTime Maximum {
			get { return ((DateTimeRange)_range).Max; }
			set { ((DateTimeRange)_range).Max = value; }
		}
		#endregion Parameters

		#region Methods
		public TimeAxisRange() : base(false) {
			// Set default behaviour
			_range = new DateTimeRange();
		}

		public TimeAxisRange(DateTime Minimum, DateTime Maximum) : base(false) {
			// Set the limits
			_range = new DateTimeRange(Minimum, Maximum);
		}

		public TimeAxisRange(DateTimeRange PlotRange) : base(false) {
			_range = PlotRange;
		}
		#endregion Methods
	}

	public class TimeSeriesAxis : DateTimeAxis {
		public TimeSeriesAxis(AxisPosition Pos, DateTime Minimum, DateTime Maximum, string Title) : base(Pos, Minimum, Maximum, Title) {
			// Set the features of the axis
			IntervalType = DateTimeIntervalType.Manual;
			MajorGridlineStyle = LineStyle.Solid;
			MajorStep = 1.0;
			MinorIntervalType = DateTimeIntervalType.Manual;
			MinorGridlineStyle = LineStyle.Dash;
			MinorStep = 0.125;
			StringFormat = "yyyy-MM-dd";
		}
	}

	public abstract class DataPlotModel {
		private PlotModel _plotModel;		// The plot model for the OxyPlot control
		private DateTimeRange _dataRange;	// The data range over which to collect data from the database
		private bool _isXAuto;				// Flag for autoscaling the x-axis
		private bool _isYAuto;				// Flag for autoscaling the y-axis

		#region Parameters
		public PlotModel Model {
			get { return _plotModel; }
			private set { _plotModel = value; }
		}

		public DateTime MinDate {
			get { return _dataRange.Min; }
			set { _dataRange.Min = value; }
		}

		public DateTime MaxDate {
			get { return _dataRange.Max; }
			set { _dataRange.Max = value; }
		}

		protected DateTimeRange DataRange {
			get { return _dataRange; }
		}

		public bool IsXAutoscale {
			get { return _isXAuto; }
			set { _isXAuto = value; }
		}

		public bool IsYAutoscale {
			get { return _isYAuto; }
			set { _isYAuto = value; }
		}
		#endregion Parameters

		#region Methods
		public DataPlotModel() {
			// Create the model
			_plotModel = new PlotModel();

			// Create the default data range and initialize the y-axis
			_dataRange = new DateTimeRange();

			// Setup the axes
			createAxes();
			_isXAuto = _isYAuto = true;	// Autoscaling is on by default
		}

		protected abstract void createAxes();

		public virtual void AddSeries(BasicPlotSeries NewSeries) {
			// Add the series to the model
			_plotModel.Series.Add(NewSeries);
		}

		public void UpdateGraph(IDBConnection dbConnection) {
			// Update all the data series
			bool firstPass = true;
			foreach(BasicPlotSeries curSeries in _plotModel.Series) {
				curSeries.SetGraphData(dbConnection, _dataRange);

				// Determine the limits
				if(firstPass) {	// First time through, so initialize limits based on this series
					if(_isXAuto) {	// Reset limits if x-axis is autoscaled
						_plotModel.Axes[0].Minimum = curSeries.MinX;
						_plotModel.Axes[0].Maximum = curSeries.MaxX;
					}
					if(_isYAuto) {	// Reset limits if y-axis is autoscaled
						_plotModel.Axes[1].Minimum = curSeries.MinY;
						_plotModel.Axes[1].Maximum = curSeries.MaxY;
					}
					firstPass = false;
				} else {	// Limits already initalized
					if(_isXAuto) {	// Only do this if autoscaling the x-axis is active
						if(curSeries.MinX < _plotModel.Axes[0].Minimum) _plotModel.Axes[0].Minimum = curSeries.MinX;
						if(curSeries.MaxX > _plotModel.Axes[0].Maximum) _plotModel.Axes[0].Maximum = curSeries.MaxX;
					}
					if(_isYAuto) {	// Only if y-axis autoscaling is on
						if(curSeries.MinY < _plotModel.Axes[1].Minimum) _plotModel.Axes[1].Minimum = curSeries.MinY;
						if(curSeries.MaxY > _plotModel.Axes[1].Maximum) _plotModel.Axes[1].Maximum = curSeries.MaxY;
					}
				}
			}

			// Update the graph
			if(_isXAuto) _plotModel.Axes[0].Reset();
			if(_isYAuto) _plotModel.Axes[1].Reset();
			_plotModel.InvalidatePlot(true);
		}
		#endregion Methods
	}

	public class TimeSeriesPlot : DataPlotModel {
		#region Methods
		protected override void createAxes() {
			// Create the DateTime axis and initialize it
			TimeSeriesAxis xAxis = new TimeSeriesAxis(AxisPosition.Bottom, DateTime.Now.Subtract(new TimeSpan(24, 0, 0)), DateTime.Now, "Datetime");
			xAxis.MinimumPadding = xAxis.MaximumPadding = 0.05;
			Model.Axes.Add(xAxis);

			// Create the Linear axis and initialize
			LinearAxis yAxis = new LinearAxis(AxisPosition.Left, 0.0, 1.0);
			yAxis.MinimumPadding = yAxis.MaximumPadding = 0.05;
			Model.Axes.Add(yAxis);
		}
		#endregion Methods
	}

	public class CorrelationPlot : DataPlotModel {
		#region Methods
		protected override void createAxes() {
			// Create the Linear axes
			Model.Axes.Add(new LinearAxis(AxisPosition.Bottom, 0.0, 1.0));
			Model.Axes.Add(new LinearAxis(AxisPosition.Left, 0.0, 1.0));
		}
		#endregion Methods
	}
}
