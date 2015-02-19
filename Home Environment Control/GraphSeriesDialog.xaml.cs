using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ruminations.Database.Generic;

namespace Home_Environment_Control {
	/// <summary>
	/// Interaction logic for CustomGraph.xaml
	/// </summary>
	public partial class GraphSeriesDialog : Window {
		#region Members
		private BasicPlotSeries _series;
		private bool _ok = false;
		private bool _isCorrelation;
		#endregion Members

		#region Parameters
		public bool IsOK {
			get { return _ok; }
		}

		public BasicPlotSeries Series {
			get { return _series; }
		}
		#endregion Parameters

		#region Methods
		public GraphSeriesDialog() {
			InitializeComponent();
		}

		public GraphSeriesDialog(Network LocalNetwork, bool IsCorrelation) {
			InitializeComponent();

			// Set the data contexts based on the series type
			_isCorrelation = this.xAxisCombo.IsEnabled = IsCorrelation;
			_series = new BasicPlotSeries();
			this.DataContext = _series;

			// Populate the combo
			foreach(Location curLocation in LocalNetwork.LocationList) {
				// Create combo entry
				ComboBoxItem curItem = new ComboBoxItem();
				curItem.Content = curLocation.Name;
				curItem.Tag = curLocation;

				// Add to the list
				this.radioCombo.Items.Add(curItem);
			}
		}
		#endregion Methods

		#region Event Handlers
		private void radioCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			// Enable the Ok button
			this.okButton.IsEnabled = true;

			// Delete any items currently in the combos
			this.xAxisCombo.Items.Clear();
			this.yAxisCombo.Items.Clear();

			// Set the contents of the list box for plottable items.  First, get the data types
			DataTypes radioDataTypes = ((Location)((ComboBoxItem)this.radioCombo.SelectedItem).Tag).RecordedData;

			// Check the types that are present and change them
			ComboBoxItem tempItem;
			foreach(DataTypes type in (DataTypes[])Enum.GetValues(typeof(DataTypes))) {
				// Add the combo item if it is present for the radio
				if((radioDataTypes & type) != 0) {
					if(!_isCorrelation || type != DataTypes.Time) {
						for(int i = 0; i < 2; i++) {
							tempItem = new ComboBoxItem();
							tempItem.Content = Enum.GetName(typeof(DataTypes), type);
							tempItem.Tag = type;
							if(i == 0) this.xAxisCombo.Items.Add(tempItem);
							else if(type != DataTypes.Time) this.yAxisCombo.Items.Add(tempItem);
						}
					}
				}
			}
			this.xAxisCombo.SelectedIndex = this.yAxisCombo.SelectedIndex = 0;
		}

		private void okButton_Click(object sender, RoutedEventArgs e) {
			// Get the title and the data
			_series.Title = nameBox.Text;
			_series.Source = (Location)((ComboBoxItem)this.radioCombo.SelectedItem).Tag;
			_series.XAxisType = (DataTypes)((ComboBoxItem)this.xAxisCombo.SelectedItem).Tag;
			_series.YAxisType = (DataTypes)((ComboBoxItem)this.yAxisCombo.SelectedItem).Tag;

			// Close the dialog
			_ok = true;
			this.Close();
		}
		#endregion Event Handlers

	}
}
