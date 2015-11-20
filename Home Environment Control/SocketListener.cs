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
	public class SocketListener : IDisposable {
		//=====================================================================
		// Delegetes
		//=====================================================================
		public delegate void SocketDataHandler(byte[] socketStr);	// Delegate for handling an event where data is transfered through the socket

		//=====================================================================
		// Events
		//=====================================================================
		public event SocketDataHandler DataReceived;	// Called when the socket receives data

		//=====================================================================
		// Local constants
		//=====================================================================
		const int maxRequestSize = 1024;	// The maximum size of the data transfered through the socket at one time

		//=====================================================================
		// Members
		//=====================================================================
		readonly int _portNumber;				// The socket port to listen to
		private Socket _listeningSocket = null;	// The socket object
		private Thread _worker = null;			// The thread that will listen to the socket

		//=====================================================================
		// Constructor
		//=====================================================================
		/// <summary>
		/// Set the socket port to listen to and start the listening thread
		/// </summary>
		/// <param name="PortNumber">The port to listen to for data</param>
		public SocketListener(int PortNumber) {
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

		//=====================================================================
		// Destructor
		//=====================================================================
		/// <summary>
		/// Dispose of the IDisposable objects and this object
		/// </summary>
		~SocketListener() {
			Dispose();
		}

		//=====================================================================
		// Listening thread
		//=====================================================================
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

					// Send the data through an event
					if(this.DataReceived != null) this.DataReceived(buffer);
				}
				Thread.Sleep(10);   // Provide some delay to help prevent lock-ups
			}
		}

		#region IDisposable Members
		//=====================================================================
		// Dispose
		//=====================================================================
		/// <summary>
		/// Closes the listening socket
		/// </summary>
		public void Dispose() {
			if(_listeningSocket != null) _listeningSocket.Close();
		}
		#endregion
	}
}
