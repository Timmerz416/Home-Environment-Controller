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
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;

namespace Home_Environment_Control {
	//=========================================================================
	// RelayTimeDialog class
	//=========================================================================
	/// <summary>
	/// Interaction logic for RelayTimeDialog.xaml
	/// </summary>
	public partial class RelayTimeDialog : Window, INotifyPropertyChanged {
		//---------------------------------------------------------------------
		// Events
		//---------------------------------------------------------------------
		public event PropertyChangedEventHandler PropertyChanged;
		public event SocketController.SocketTransmissionRequest TransmissionRequest;

		//---------------------------------------------------------------------
		// Members
		//---------------------------------------------------------------------
		private bool _systemTimeEnabled = true;	// Indicated the radio button status
		private DateTime _latestRelayTime;		// The latest relay time received

		//---------------------------------------------------------------------
		// Properties
		//---------------------------------------------------------------------
		/// <summary>
		/// Get the status of the radio button
		/// </summary>
		public bool SystemTimeEnabled {
			get { return _systemTimeEnabled; }
			set { _systemTimeEnabled = value; }
		}

		/// <summary>
		/// Get the latest read time from the relay
		/// </summary>
		public string RelayTime {
			get {
				if(_latestRelayTime != null) return _latestRelayTime.ToString("yyyy-MM-dd HH:mm:ss");
				else return "0000-00-00 00:00:00";
			}
		}

		/// <summary>
		/// Get the current computer time
		/// </summary>
		public string Now {
			get { return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); }
		}

		#region Methods
		//---------------------------------------------------------------------
		// Constructor
		//---------------------------------------------------------------------
		/// <summary>
		/// Initialize controls and populate with current time data
		/// </summary>
		public RelayTimeDialog() {
			// Initialize controls
			InitializeComponent();

			// INITIALIZE DATA IN CONTROLS
			///////////////////////////////////////////////////////////////////
			// Set the radio button status to checked for system time
			this.CurrentTimeRadio.IsChecked = true;
			this.CustomTimeBox.IsEnabled = false;

			// SETUP TIMER FOR UPDATING THE CURRENT TIME
			///////////////////////////////////////////////////////////////////
			// Setup Timer class
			System.Timers.Timer timer = new System.Timers.Timer(1000);
			timer.Elapsed += Timer_Elapsed;
			timer.Start();
		}

		//---------------------------------------------------------------------
		// updateTimeToCurrent
		//---------------------------------------------------------------------
		/// <summary>
		/// Send transmission to radio to set time to the computer time
		/// </summary>
		private void updateTimeToCurrent() {
			if(this.TransmissionRequest != null) this.TransmissionRequest(this, new TimeTxArgs(DateTime.Now));
			else Debug.Assert(false);	// Shouldn't be here without programming error
		}

		//---------------------------------------------------------------------
		// updateTimeToUser
		//---------------------------------------------------------------------
		/// <summary>
		/// Send transmission to the radio to set time to the user-specified time
		/// </summary>
		private void updateTimeToUser() {
			DateTime? selectedTime = CustomTimeBox.Value;
			if((this.TransmissionRequest != null) && (selectedTime != null)) this.TransmissionRequest(this, new TimeTxArgs(selectedTime.Value));
			else Debug.Assert(false);	// Shouldn't be here without programming error
		}
		#endregion Methods

		#region Events
		//---------------------------------------------------------------------
		// ReloadTimeBtn_Click
		//---------------------------------------------------------------------
		/// <summary>
		/// Send the request for the relay time.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ReloadTimeBtn_Click(object sender, RoutedEventArgs e) {
			// Create the time get argument and issue the command
			if(this.TransmissionRequest != null) this.TransmissionRequest(this, new TimeTxArgs());
			else Debug.Assert(false);	// Shouldn't be here without programming error
		}

		//---------------------------------------------------------------------
		// SetCurrentTimeBtn_Click
		//---------------------------------------------------------------------
		/// <summary>
		/// The user requested to update the time on the relay - either based on 
		/// computer time or user-specified time, depending on radio button status.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SetCurrentTimeBtn_Click(object sender, RoutedEventArgs e) {
			if(SystemTimeEnabled) updateTimeToCurrent();
			else updateTimeToUser();
		}

		//---------------------------------------------------------------------
		// HandleSocketTime
		//---------------------------------------------------------------------
		/// <summary>
		/// Takes the time transmission data from the socket and updates as needed
		/// </summary>
		/// <param name="TimeArgs">The socket time data</param>
		public void HandleSocketTime(object sender, BasicRxArgs TimeArgs) {
			// Convert the object time
			Debug.Assert(TimeArgs is TimeRxArgs);
			TimeRxArgs timeRxData = TimeArgs as TimeRxArgs;

			switch(timeRxData.Operation) {
				case TimeRxArgs.STATUS_GET: // The updated time was passed through the socket
					_latestRelayTime = timeRxData.Time; // Update the time
					if(this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs("RelayTime")); // Flag that the time changed
					break;
				case TimeRxArgs.STATUS_SET: // The time was set on the relay
					if(!timeRxData.IsAcknowledgement) MessageBox.Show("The SET command was not acknowledged.", "Incomplete Transmission", MessageBoxButton.OK, MessageBoxImage.Warning);
					break;
				case TimeRxArgs.CMD_NACK:   // An error occurred and the relay did not acknowledge the command
					MessageBox.Show("The command was not recognized.  Please check your system and try again.", "Command Unacknowledged", MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				default:
					MessageBox.Show("Operation '" + timeRxData.Operation + "' unknown.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
					break;
			}
		}

		//---------------------------------------------------------------------
		// exitButton_Click
		//---------------------------------------------------------------------
		/// <summary>
		/// Close the window
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void exitButton_Click(object sender, RoutedEventArgs e) {
			this.Close();	// Close the window
		}

		//---------------------------------------------------------------------
		// CurrentTimeRadio_Click
		//---------------------------------------------------------------------
		/// <summary>
		/// Flags and disables the user time input so any relay time update is
		/// based on the current computer time.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CurrentTimeRadio_Checked(object sender, RoutedEventArgs e) {
			_systemTimeEnabled = true;			// Indicate that current time is set
			CustomTimeBox.IsEnabled = false;	// Disable the user time input control
		}

		//---------------------------------------------------------------------
		// UserTimeRadio_Click
		//---------------------------------------------------------------------
		/// <summary>
		/// Flags the controls the inputs so any relay time update is based on the
		/// user-input time.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UserTimeRadio_Checked(object sender, RoutedEventArgs e) {
			_systemTimeEnabled = false;         // Indicate the current time is not set
			CustomTimeBox.IsEnabled = true;		// Enable the user time input control
			CurrentTimeBox.IsEnabled = false;	// Disable the current time input control
		}

		//---------------------------------------------------------------------
		// Timer_Elapsed
		//---------------------------------------------------------------------
		/// <summary>
		/// Update the time displayed in the window
		/// </summary>
		/// <param name="sender">The object calling this event</param>
		/// <param name="e">The timer parameters for the event</param>
		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			if(this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs("Now"));	// Update the time
		}
		#endregion Events
	}
}
