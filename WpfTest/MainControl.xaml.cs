using System;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
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
using ICSharpCode;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows.Controls.Primitives;
using static WpfTest.UIHelpers;

namespace WpfTest {
	/// <summary>
	/// Interaction logic for MainControl.xaml
	/// </summary>
	public partial class MainControl : UserControl {
		SqlConnection _conn;

		public MainControl(SqlConnection conn) {
			_conn = conn;
			_conn.InfoMessage += OnConnInfo;
			_conn.StateChange += OnConnStateChanged;
			InitializeComponent();

			//_timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
			//_timer.Interval = TimeSpan.FromSeconds(5);
			//_timer.Start();
			//_timer.Tick += _timer_Tick;

            AddNewTab();
		}

		//DispatcherTimer _timer;

	

		void _timer_Tick(object sender, EventArgs e) {
			AddNewTab();
		}

		void AddNewTab() {
			var tabItem = new TabItem { Header = $"Tab {_tabControl.Items.Count + 1}" };
			tabItem.Content = new TabContentView(_conn);

			_tabControl.Items.Add(tabItem);

			var style = _tabControl.Items.Count > 1 ? VISIBLE_STYLE : COLLAPSE_STYLE;
			_tabControl.ItemContainerStyle = style;
		}

		void OnConnStateChanged(object sender, StateChangeEventArgs e) {
			throw new NotImplementedException();
		}

		void OnConnInfo(object sender, SqlInfoMessageEventArgs e) {
			throw new NotImplementedException();
		}

		void _tabControl_Loaded(object sender, RoutedEventArgs e) {
		}
	}
}
