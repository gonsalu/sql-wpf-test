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

		void StartConnView(Task<SqlConnection> getConn) {

			var conn = getConn.Result;

			var dockPanel = new DockPanel();
			var control = new MainControl(conn);
			dockPanel.Children.Add(control);
			this.Content = dockPanel;

			_win = new ObjectFinder(conn.ConnectionString);
			_win.Show();
			_win._sel
        }
		ObjectFinder _win;

		void Gon(Task<ObjectRow> task) {

		}
	}
}
