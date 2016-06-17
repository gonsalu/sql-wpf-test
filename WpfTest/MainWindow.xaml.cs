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
using System.Windows.Threading;

namespace WpfTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			_connectionChooser
				.GetValidConnTask()
				.ContinueWith(StartConnView, TaskScheduler.FromCurrentSynchronizationContext());
		}
		SqlConnection _conn;
		void StartConnView(Task<SqlConnection> getConn) {
			_conn = getConn.Result;

			var dockPanel = new DockPanel();
			var control = new MainControl(_conn);
			dockPanel.Children.Add(control);
			this.Content = dockPanel;

			_win = new ObjectFinder(_conn.ConnectionString);
			_win.SelectObject.ContinueWith(ShowObject);
			_win.Show();
        }
		ObjectFinder _win;

		string GetConnectionString() {
			var connBuilder = new SqlConnectionStringBuilder();
			connBuilder.ConnectionString = _conn.ConnectionString;
			connBuilder.InitialCatalog = "AdventureWorks2014";
			return connBuilder.ConnectionString;
		} 

		async void ShowObject(Task<ObjectRow> task) {
			var obj = task.Result;
			string sql;
			switch (obj.typ)
			{
				case ObjectType.Table:
					sql = $"SELECT TOP 10 * FROM {obj.schema}.{obj.name}";
					break;
				case ObjectType.ScalarFn:
				case ObjectType.TableFn:
				case ObjectType.StoredProc:
				case ObjectType.View:
					
					using (var conn = new SqlConnection(GetConnectionString()))
					using (var cmd = conn.CreateCommand())
					{
						cmd.CommandText = $"select OBJECT_DEFINITION({obj.object_id})"; 
                        await conn.OpenAsync();
						var rdr = await cmd.ExecuteReaderAsync();
						if (rdr.Read()) {
							var v = rdr.GetValue(0);
							sql = Convert.IsDBNull(v) ? "" : (string)v;
						} else {
							sql = "";
						}
					}
					break;
				default:
					throw new ArgumentOutOfRangeException("object type");
			}
			MessageBox.Show(sql);
		}
	}
}
