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
using System.Data.Common;

using Ruminations.Database.MySQL;

namespace Home_Environment_Control {
	/// <summary>
	/// Interaction logic for LoginWindow.xaml
	/// </summary>
	public partial class LoginWindow : Window {
		private MySQLConnection _dbConnection;
		private int _userID;

		public LoginWindow(MySQLConnection DBConnection) {
			InitializeComponent();

			_dbConnection = DBConnection;   // Set the DB connection member
			_userID = 0;
		}

		public int UserID {
			get { return _userID; }
		}

		//=====================================================================
		// LoginButton_Click event handler
		//=====================================================================
		/// <summary>
		/// The event handler for when the login button is clicked.  The code
		/// searches for the specified user and returns their db id.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LoginButton_Click(object sender, RoutedEventArgs e) {
			// Check for contents in the inputs
			if(UsernameBox.Text.Length == 0) MessageBox.Show("Please enter Username", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
			else {
				// Create the string to search for the user
				string query = "SELECT id FROM users WHERE username='" + UsernameBox.Text + "' AND password";
				if(PasswordBox.Text == "") query += " IS NULL";
				else query += "='" + PasswordBox.Text + "'";

				// Search to see if the username/password combo exists
				DbDataReader db_result = _dbConnection.RunQuery(query);
				if(db_result.HasRows) {
					// Read the record and set the user id
					if(db_result.Read()) {
						_userID = db_result.GetInt32(0);    // Get the returned user id
						this.DialogResult = true;			// Identify success finding the user, and close the dialog
					} else throw new Exception("Error reading from the database");
				} else {
					MessageBox.Show("Account with the entered username/password was not found.", "Cannot Find Account", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				}
				db_result.Close();	// Free up the results
			}
		}
	}
}
