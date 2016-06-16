using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using static WpfTest.UIHelpers;

namespace WpfTest {
	/// <summary>
	/// Interaction logic for ConnectionStatusBar.xaml
	/// </summary>
	public partial class ConnectionStatusBar : UserControl {
		SqlConnection _conn;

		public ConnectionStatusBar(SqlConnection conn) {
			_conn = conn;
			InitializeComponent();
		}

		async void _comboBox_Loaded(object sender, RoutedEventArgs e) {
			var dbs = new List<string>();

			using (var conn = new SqlConnection(_conn.ConnectionString))
			using (var cmd = conn.CreateCommand()) {
				await conn.OpenAsync();
				cmd.CommandText = "select name from master.sys.databases";
				var rdr = await cmd.ExecuteReaderAsync();
				while (await rdr.ReadAsync()) {
					var dbname = rdr.GetString(0);
					dbs.Add(dbname);
				}
			}
			var comboBox = sender as ComboBox;
			comboBox.ItemsSource = dbs;
			comboBox.SelectedIndex = 0;
		}

		public string ChosenInitialCatalog { get; private set; }
		void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			ChosenInitialCatalog = (string)_comboBox.SelectedValue;
		}

		public void ToggleLoading(bool loading) {
			Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => {
				_progressBar.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
			}));
		}
	}
}
