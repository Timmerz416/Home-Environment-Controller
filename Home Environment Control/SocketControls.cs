using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Home_Environment_Control {
	//=========================================================================
	// SocketListener class
	//=========================================================================
	/// <summary>
	/// This is a class that starts a thread which will listen for any information
	/// coming through a socket specified in the object.
	/// </summary>
	public class SocketController : IDisposable {
		//---------------------------------------------------------------------
		// Delegetes
		//---------------------------------------------------------------------
		public delegate void SocketDataHandler(object sender, byte[] socketStr);			// Delegate for handling an event where raw data is transfered through the socket
		public delegate void SocketProcessedData(object sender, BasicRxArgs e);				// Delegate for hadnling an event with processed command data
		public delegate void SocketTransmissionRequest(object sender, BasicSocketArgs e);	// Delegate for handling requests to send a transmission

		//---------------------------------------------------------------------
		// Events
		//---------------------------------------------------------------------
		public event SocketDataHandler DataReceived;		// Called when the socket receives data
		public event SocketProcessedData TimeDataReceived;	// Called when a time command has been received

		//---------------------------------------------------------------------
		// Local constants
		//---------------------------------------------------------------------
		private const int maxRequestSize = 1024;    // The maximum size of the data transfered through the socket at one time

		//---------------------------------------------------------------------
		// Members
		//---------------------------------------------------------------------
		readonly int _portNumber;				// The socket port to listen to
		private Socket _listeningSocket = null;	// The socket object
		private Thread _worker = null;          // The thread that will listen to the socket

		//---------------------------------------------------------------------
		// Constructor
		//---------------------------------------------------------------------
		/// <summary>
		/// Set the socket port to listen to and start the listening thread
		/// </summary>
		/// <param name="PortNumber">The port to listen to for data</param>
		public SocketController(int PortNumber) {
			// Setup the socket and initialize it for listening
			_portNumber = PortNumber;
			_listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_listeningSocket.Bind(new IPEndPoint(IPAddress.Any, _portNumber));
			_listeningSocket.Listen(10);

			// Listen for connections in another thread
			_worker = new Thread(StartListening);
			_worker.IsBackground = true;    // This way the thread ends with the main program
			_worker.Start();
		}

		//---------------------------------------------------------------------
		// Destructor
		//---------------------------------------------------------------------
		/// <summary>
		/// Dispose of the IDisposable objects and this object
		/// </summary>
		~SocketController() {
			Dispose(false);
		}

		//---------------------------------------------------------------------
		// Listening thread
		//---------------------------------------------------------------------
		/// <summary>
		/// The worker thread that will listen for incoming data on the port
		/// </summary>
		public void StartListening() {
			// Infinite loop looking for connections
			while(true) {
				using(Socket clientSocket = _listeningSocket.Accept()) {
					// Get the client IP
					IPEndPoint clientIP = clientSocket.RemoteEndPoint as IPEndPoint;

					// Determine the size of the transmission
					Thread.Sleep(10);   // Delay a bit to receive the transmission
					int availableBytes = clientSocket.Available;
					int bytesReceived = (availableBytes > maxRequestSize ? maxRequestSize : availableBytes);

					// Process the request and get the data
					byte[] buffer = new byte[bytesReceived];
					int readByteCount = clientSocket.Receive(buffer, bytesReceived, SocketFlags.None);
					//string message = new string(Encoding.UTF8.GetChars(buffer));

					// Determine the type of transmission
					BasicRxArgs response = new BasicRxArgs(buffer);

					// Send a special event based on the type of data received
					switch(response.Type) {
						case BasicRxArgs.CMD_TIME_REQUEST:
							TimeRxArgs args = new TimeRxArgs(buffer);
							if(this.TimeDataReceived != null) this.TimeDataReceived(this, args);
							break;
						default:
							if(this.DataReceived != null) this.DataReceived(this, buffer);
							break;
					}
				}
				Thread.Sleep(10);   // Provide some delay to help prevent lock-ups
			}
		}

		//---------------------------------------------------------------------
		// SendTransmission
		//---------------------------------------------------------------------
		/// <summary>
		/// Sends a transmission through a socked to the radio server.
		/// </summary>
		/// <param name="sender">The object that sent this event</param>
		/// <param name="args">Oject that contains the command to send</param>
		public void SendTransmission(object sender, BasicSocketArgs args) {
			using(Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
				// Send the command to get the status of relay
				server.Connect(new IPEndPoint(IPAddress.Parse("192.168.2.100"), 5267));
				byte[] cmd = Encoding.UTF8.GetBytes(args.Command);
				int sentBytes = server.Send(cmd, SocketFlags.None);

				// Output result known from this side
				if(sentBytes != cmd.Length) throw new Exception("Error trying to send command '" + args.Command + "': return status is " + sentBytes);
			}

		}

		#region IDisposable Members
		//---------------------------------------------------------------------
		// Dispose
		//---------------------------------------------------------------------
		/// <summary>
		/// Closes the listening socket
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes of managed and native disposable ojects separately
		/// </summary>
		/// <param name="disposing">Indicates if managed objects should be disposed</param>
		protected virtual void Dispose(bool disposing) {
			// Handle the managed objects
			if(disposing) {
				_listeningSocket.Close();
			}
		}
		#endregion
	}

	//=========================================================================
	// BasicSocketArgs base class
	//=========================================================================
	public class BasicSocketArgs : EventArgs {
		#region Constants
		//---------------------------------------------------------------------
		// Constants
		//---------------------------------------------------------------------
		// Transmission command codes
		public const byte CMD_UNKNOWN = 0;
		public const byte CMD_THERMO_POWER = 1;
		public const byte CMD_OVERRIDE = 2;
		public const byte CMD_RULE_CHANGE = 3;
		public const byte CMD_SENSOR_DATA = 4;
		public const byte CMD_TIME_REQUEST = 5;
		public const byte CMD_STATUS = 6;

		// Transmission command strings
		public const string CMD_THERMO_POWER_STR = "TS";
		public const string CMD_OVERRIDE_STR = "PO";
		public const string CMD_RULE_CHANGE_STR = "TR";
		public const string CMD_SENSOR_DATA_STR = "DR";
		public const string CMD_TIME_REQUEST_STR = "CR";
		public const string CMD_STATUS_STR = "ST";

		// Transmission subcommand codes
		public const byte CMD_NACK = 0;
		public const byte CMD_ACK = 1;
		public const byte STATUS_OFF = 2;
		public const byte STATUS_ON = 3;
		public const byte STATUS_GET = 4;
		public const byte STATUS_ADD = 5;
		public const byte STATUS_DELETE = 6;
		public const byte STATUS_MOVE = 7;
		public const byte STATUS_UPDATE = 8;
		public const byte STATUS_SET = 9;

		// Transmission subcommand strings
		public const string CMD_NACK_STR = "NACK";
		public const string CMD_ACK_STR = "ACK";
		public const string STATUS_OFF_STR = "OFF";
		public const string STATUS_ON_STR = "ON";
		public const string STATUS_GET_STR = "GET";
		public const string STATUS_ADD_STR = "ADD";
		public const string STATUS_DELETE_STR = "DELETE";
		public const string STATUS_MOVE_STR = "MOVE";
		public const string STATUS_UPDATE_STR = "UPDATE";
		public const string STATUS_SET_STR = "SET";

		public const char DELIMITER = ':';
		#endregion Constants

		//---------------------------------------------------------------------
		// Members
		//---------------------------------------------------------------------
		protected string _command;	// The command as a string

		//---------------------------------------------------------------------
		// Properties
		//---------------------------------------------------------------------
		/// <summary>
		/// Returns the command
		/// </summary>
		public string Command {
			get { return _command; }
		}

		//---------------------------------------------------------------------
		// Constructor
		//---------------------------------------------------------------------
		/// <summary>
		/// Create an empty command
		/// </summary>
		public BasicSocketArgs() {
			_command = "";
		}

		/// <summary>
		/// Create the command from a byte array
		/// </summary>
		/// <param name="Command">The byte array with the command characters</param>
		public BasicSocketArgs(byte[] Command) {
			_command = Encoding.UTF8.GetString(Command);
		}

		/// <summary>
		/// Create the command from a string
		/// </summary>
		/// <param name="Command">The command in a string object</param>
		public BasicSocketArgs(string Command) {
			_command = Command;
		}
	}

	//=========================================================================
	// TimeTxArgs
	//=========================================================================
	/// <summary>
	/// Arguments class for handling time requests
	/// </summary>
	public class TimeTxArgs : BasicSocketArgs {
		//---------------------------------------------------------------------
		// Constructor
		//---------------------------------------------------------------------
		/// <summary>
		/// Creates a time request command
		/// </summary>
		public TimeTxArgs() : base(CMD_TIME_REQUEST_STR + DELIMITER + STATUS_GET_STR) { }

		/// <summary>
		/// Creates a set time command
		/// </summary>
		/// <param name="SetTime">The datetime to be set</param>
		public TimeTxArgs(DateTime SetTime) : base() {
			// Initialize the command string
			_command = CMD_TIME_REQUEST_STR + DELIMITER + STATUS_SET_STR + DELIMITER;

			// Determine some calculated values
			int year = SetTime.Year - 2000; // Just get the last two digits of the year
			int weekday = (int) SetTime.DayOfWeek;

			// Finish the command string
			_command += SetTime.ToString("s" + DELIMITER + "m" + DELIMITER + "H");
			_command += DELIMITER + weekday.ToString() + DELIMITER;
			_command += SetTime.ToString("d" + DELIMITER + "M");
			_command += DELIMITER + year.ToString();
		}
	}

	//=========================================================================
	// BasicRxArgs base class
	//=========================================================================
	/// <summary>
	/// The base class type for a command received from the server through the socket
	/// </summary>
	public class BasicRxArgs : BasicSocketArgs {
		//---------------------------------------------------------------------
		// Members
		//---------------------------------------------------------------------
		// Private
		protected string[] _args;	// String array of the received command
		private byte _cmdType;		// The type of the command

		//---------------------------------------------------------------------
		// Properties
		//---------------------------------------------------------------------
		/// <summary>
		/// Simple check to see if an ACK or NACK was sent, indicating a response transmission
		/// </summary>
		public virtual bool IsResponse {
			get {
				if((_args[1] == CMD_ACK_STR) || (_args[1] == CMD_NACK_STR)) return true;
				else return false;
			}
		}

		/// <summary>
		/// Simple check to see if a response and transmission was acknowledged
		/// </summary>
		public virtual bool IsAcknowledgement {
			get {
				if(IsResponse && (_args[1] == CMD_ACK_STR)) return true;
				else return false;
			}
		}

		/// <summary>
		/// Checks to see if there was an error in the transmission, not just a NACK
		/// </summary>
		public virtual bool IsError {
			get { return false; }
		}

		/// <summary>
		/// Returns the command type
		/// </summary>
		public byte Type {
			get { return _cmdType; }
		}

		//---------------------------------------------------------------------
		// Constructors
		//---------------------------------------------------------------------
		/// <summary>
		/// Initialize the data based on a byte array of the received command
		/// </summary>
		/// <param name="CommandArray">Command as a byte array</param>
		public BasicRxArgs(byte[] CommandArray) : base(CommandArray) {
			ProcessData();
		}

		/// <summary>
		/// Initialize the data based on a string of the received command
		/// </summary>
		/// <param name="Command">Command as a string</param>
		public BasicRxArgs(string Command) : base(Command) {
			ProcessData();
		}

		//---------------------------------------------------------------------
		// ProcessData
		//---------------------------------------------------------------------
		/// <summary>
		/// Method to break the command into arguments and define command type.
		/// </summary>
		protected virtual void ProcessData() {
			// Split the command
			_args = _command.Split(DELIMITER);

			// Determine the command type
			switch(_args[0]) {
				case CMD_THERMO_POWER_STR:
					_cmdType = CMD_THERMO_POWER;
					break;
				case CMD_OVERRIDE_STR:
					_cmdType = CMD_OVERRIDE;
					break;
				case CMD_RULE_CHANGE_STR:
					_cmdType = CMD_RULE_CHANGE;
					break;
				case CMD_SENSOR_DATA_STR:
					_cmdType = CMD_SENSOR_DATA;
					break;
				case CMD_TIME_REQUEST_STR:
					_cmdType = CMD_TIME_REQUEST;
					break;
				case CMD_STATUS_STR:
					_cmdType = CMD_STATUS;
					break;
				default:
					_cmdType = CMD_UNKNOWN;
					break;
			}
		}
	}

	//=========================================================================
	// TimeRxArgs
	//=========================================================================
	public class TimeRxArgs : BasicRxArgs {
		//---------------------------------------------------------------------
		// Members
		//---------------------------------------------------------------------
		private byte _operation;	// Contains the type of time command
		private DateTime _time;		// Contains the date and time associated with the transaction, if applicable
		
		//---------------------------------------------------------------------
		// Constructors
		//---------------------------------------------------------------------
		/// <summary>
		/// Initialize the data based on a byte array of the received command
		/// </summary>
		/// <param name="CommandArray">Command as a byte array</param>
		public TimeRxArgs(byte[] CommandArray) : base(CommandArray) { }

		/// <summary>
		/// Initialize the data based on a string of the received command
		/// </summary>
		/// <param name="Command">Command as a string</param>
		public TimeRxArgs(string Command) : base(Command) { }

		//---------------------------------------------------------------------
		// Properties
		//---------------------------------------------------------------------
		/// <summary>
		/// Checks to see if there was an error in the transmission, not just a NACK
		/// </summary>
		public override bool IsError {
			get {
				return (_operation != STATUS_GET) && (_operation != STATUS_SET);
			}
		}

		/// <summary>
		/// Simple check to see if a response and transmission was acknowledged
		/// </summary>
		public override bool IsAcknowledgement {
			get {
				if(_args.Length > 2) return _args[2] == CMD_ACK_STR;
				else return false;
			}
		}

		/// <summary>
		/// Simple check to see if an ACK or NACK was sent, indicating a response transmission
		/// </summary>
		public override bool IsResponse {
			get {
				return (_operation == STATUS_GET) || (_operation == STATUS_SET) || (_operation == CMD_NACK);
			}
		}

		/// <summary>
		/// Get the operation (GET, SET)
		/// </summary>
		public byte Operation {
			get { return _operation; }
		}

		/// <summary>
		/// Get the transmitted time
		/// </summary>
		public DateTime Time {
			get { return _time; }
		}

		//---------------------------------------------------------------------
		// Members
		//---------------------------------------------------------------------
		/// <summary>
		/// Read the command to get the requested operation and time
		/// </summary>
		protected override void ProcessData() {
			base.ProcessData(); // Run base code

			// Determine the subcommand
			switch(_args[1]) {
				case CMD_NACK_STR:
					_operation = CMD_NACK;
					break;
				case STATUS_GET_STR:
					_operation = STATUS_GET;
					break;
				case STATUS_SET_STR:
					_operation = STATUS_SET;
					break;
				default:
					throw new Exception("Time transmission of unrecognized/unexpected type: " + _args[1]);
			}

			// Get the sent time
			if((_operation == STATUS_GET) && (_args.Length == 9)) { // TODO - Error handling if the args are incorrect
				// Translate the information
				int seconds = int.Parse(_args[2]);
				int minutes = int.Parse(_args[3]);
				int hours = int.Parse(_args[4]);
				int day = int.Parse(_args[6]);
				int month = int.Parse(_args[7]);
				int year = 2000 + int.Parse(_args[8]);

				_time = new DateTime(year, month, day, hours, minutes, seconds);
			}
		}
	}
}
