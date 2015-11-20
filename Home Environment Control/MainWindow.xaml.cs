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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;

using Ruminations.Database.MySQL;

namespace Home_Environment_Control {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private MySQLConnection _dbConnection;
		private Network _network;
		private bool _isCorrelationPlot;
		private SocketListener _socketCtrl;

		#region Methods
		public MainWindow() {
			// Initialize the components
			InitializeComponent();

			// Connect to the database
			StartDBConnection();

            // Open the login window
            LoginWindow loginPage = new LoginWindow(_dbConnection);
            if((bool) loginPage.ShowDialog()) {
				// Initialize the data model
				InputNetworkData();

                // Set the data context for this window
                TimeSeriesPlot curPlot = new TimeSeriesPlot();
                _isCorrelationPlot = false;
                this.DataContext = curPlot;

				// Setup the socket listening port
				_socketCtrl = new SocketListener(6232);
				_socketCtrl.DataReceived += socketCtrl_DataReceived;
            } else {
				// Exit the program
				Environment.Exit(0);
			}
		}

		private void StartDBConnection() {
			// Close any existing databases
			if(_dbConnection != null) _dbConnection.Dispose();

			// Create the new connection
			_dbConnection = new MySQLConnection("192.168.2.53", "data_logger", "QwTXBQ3pQjdUXrMH", "home_monitor");
//			_dbConnection = new MySQLConnection("192.168.2.53", "tl1", "1tiM&Merz9", "home_monitor");
		}

		private void InputNetworkData() {
			// Initialize the database description
			_network = new Network();
			_network.Populate(_dbConnection);
		}
		#endregion Methods

		#region Event Handlers
		private void TimeSeriesGraph_Click(object sender, RoutedEventArgs e) {
			// Check that the plot isn't already a time series
			if(!_isCorrelationPlot) return;

			// Warn user of changes
			if(MessageBox.Show("You are about to delete all currently plotted data. Proceed?", "Warning", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
				// Set the new plot
				TimeSeriesPlot newPlot = new TimeSeriesPlot();
				this.DataContext = newPlot;

				// Redraw the plot
				_isCorrelationPlot = false;
				_graph.InvalidatePlot();
			}
		}

		private void CorrelationGraph_Click(object sender, RoutedEventArgs e) {
			// Check that the plot isn't already a correlation
			if(_isCorrelationPlot) return;

			// Warn user of changes
			if(MessageBox.Show("You are about to delete all currently plotted data. Proceed?", "Warning", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
				// Set the new plot
				CorrelationPlot newPlot = new CorrelationPlot();
				this.DataContext = newPlot;

				// Redraw the plot
				_isCorrelationPlot = true;
				_graph.InvalidatePlot();
			}
		}

		private void axesDates_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			// Update the graph
			DataPlotModel modelDriver = this.DataContext as DataPlotModel;
			modelDriver.UpdateGraph(_dbConnection);
		}

		private void AddSeriesButton_Click(object sender, RoutedEventArgs e) {
			// Create a new series
			GraphSeriesDialog curGraphSettings = new GraphSeriesDialog(_network, _isCorrelationPlot);
			curGraphSettings.ShowDialog();

			// Check the return value
			if(curGraphSettings.IsOK) {
				// Get the series and add it to the graph
				BasicPlotSeries curSeries = curGraphSettings.Series;

				// Get the plot model
				DataPlotModel modelDriver = this.DataContext as DataPlotModel;

				// Reset the graph
				modelDriver.AddSeries(curSeries);
				modelDriver.UpdateGraph(_dbConnection);
			}
		}

		//=====================================================================
		// RelayStatus_Click
		//=====================================================================
		/// <summary>
		/// Event handler for when the relay status button clicked.  Requests the status
		/// of the relay.
		/// </summary>
		/// <param name="sender">Object sending the event</param>
		/// <param name="e">Event arguments</param>
		private void RelayStatus_Click(object sender, RoutedEventArgs e) {
			using(Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
				try {
					// Send the command to get the status of relay
					server.Connect(new IPEndPoint(IPAddress.Parse("192.168.2.100"), 5267));
					byte[] cmd = Encoding.UTF8.GetBytes("ST");
					int sentBytes = server.Send(cmd, SocketFlags.None);

					// Output result known from this side
					if(sentBytes != cmd.Length) throw new Exception("Error sending command: send return status is " + sentBytes);
				} catch(Exception err) {
					MessageBox.Show("Caught Exception with message '" + err.Message + "' when requesting relay status update.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		//=====================================================================
		// socketCtrl_DataReceived
		//=====================================================================
		/// <summary>
		/// Handles data that is collected through the listening socket.
		/// </summary>
		/// <param name="socketStr">The data read from the socket</param>
		private void socketCtrl_DataReceived(byte[] socketStr) {
			MessageBox.Show(Encoding.UTF8.GetString(socketStr));
		}

		//=====================================================================
		// ClockStatus_Click
		//=====================================================================
		/// <summary>
		/// Event handler for clicking the clock status button - will request the
		/// current clock settings.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ClockStatus_Click(object sender, RoutedEventArgs e) {
			using(Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
				try {
					// Send the command to get the status of relay
					server.Connect(new IPEndPoint(IPAddress.Parse("192.168.2.100"), 5267));
					byte[] cmd = Encoding.UTF8.GetBytes("CR:GET");
					int sentBytes = server.Send(cmd, SocketFlags.None);

					// Output result known from this side
					if(sentBytes != cmd.Length) throw new Exception("Error sending command: send return status is " + sentBytes);
				} catch(Exception err) {
					MessageBox.Show("Caught Exception with message '" + err.Message + "' when requesting clock status update.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		#endregion Event Handlers
	}
}
