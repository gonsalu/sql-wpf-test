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
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;

namespace WpfTest {
	/// <summary>
	/// Interaction logic for ConnectionChooser.xaml
	/// </summary>
	public partial class ConnectionChooser : UserControl {
		public ConnectionChooser() {
			InitializeComponent();
			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) {
				//this.Background = Brushes.White;
			}
		}

		TaskCompletionSource<SqlConnection> _validConn = new TaskCompletionSource<SqlConnection>();

		public Task<SqlConnection> GetValidConnTask() => _validConn.Task;

		private async void connectBtn_Click(object sender, RoutedEventArgs e) {
			_connectBtn.IsEnabled = false;
			_spinner.Visibility = Visibility.Visible;

			var serverName = _serverName.Text;
			var connBuilder = new SqlConnectionStringBuilder();
			connBuilder.ApplicationName = "WpfTest";
			connBuilder.IntegratedSecurity = true;
			connBuilder.DataSource = serverName;
			var connStr = connBuilder.ConnectionString;

			Trace.WriteLine($"Trying connection \"{connStr}\"");

			new Thread(() => {
				Task.Run(() => {
					var conn = new SqlConnection(connStr);
					try {
						conn.Open();
						Application.Current.Dispatcher.BeginInvoke(
							DispatcherPriority.Normal, new Action(() => {
								_cancelBtn.IsEnabled = false;
								_connectBtn.IsEnabled = false;
							}));
						_validConn.SetResult(conn);
						Trace.WriteLine($"Successfully connected to \"{connStr}\"");
					} catch (Exception ex) {
						MessageBox.Show(ex.Message, "Connection Error",
							MessageBoxButton.OK,
							MessageBoxImage.Error,
							MessageBoxResult.OK);
					}
				});
			}).Start();
		}

		private void cancelBtn_Click(object sender, RoutedEventArgs e) {

		}
	}
}
