using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static WpfTest.UIHelpers;
using ICSharpCode.AvalonEdit.AddIn;
using System.ComponentModel.Design;
using System.Threading;
using System.Windows.Data;
using System.Dynamic;
using System.Diagnostics;
using System.Windows.Documents;

namespace WpfTest {
	/// <summary>
	/// Interaction logic for ObjectFinder.xaml
	/// </summary>
	public partial class ObjectFinder : Window {
		TaskCompletionSource<ObjectRow> _selectObj = new TaskCompletionSource<ObjectRow>();

		public ObjectFinder(string connectionString) {
			InitializeComponent();
			var connBuilder = new SqlConnectionStringBuilder();
			connBuilder.ConnectionString = connectionString;
			connBuilder.InitialCatalog = "AdventureWorks2014";
			_searchField.InitConnection(connBuilder.ConnectionString);
			_searchField.MatchesChanged += _searchField_MatchesChanged;
		}

		private void _searchField_MatchesChanged(object sender, MatchResult[] results) {
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
				_listBox.Items.Clear();

				for (int i = 0; i < results.Length; i++) {
					var res = results[i];
					var it = new ListBoxItem();
					it.SetValue(IndexProp, i);
					var tb = new TextBlock();
					it.Content = tb;

					AddParts(tb, res, true);
					AddParts(tb, res, false);

					_listBox.Items.Add(it);
				}
			}));

		}

		static readonly Brush PLACEHOLDER_BRUSH = new SolidColorBrush(Color.FromRgb(149, 165, 166));

		void InitPlaceholder() {
			//var style = new Style();
			//style.Setters.Add(new Setter(Control.ForegroundProperty, PLACEHOLDER_BRUSH));
			//_textBox.Style = style;
			//_textBox.Text = "Search";
		}

		void _listBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			System.Diagnostics.Trace.WriteLine("Changed!");
			foreach (var item in e.AddedItems) {
				var idx = (int)((ListBoxItem)item).GetValue(IndexProp);
				Trace.WriteLine($"Item {idx}");
			}
		}

		void _searchField_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
			if (e.Key == System.Windows.Input.Key.Down) {
				_listBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => {
					if (_listBox.Items.Count > 0) {
						_listBox.Focus();
						_listBox.SelectedIndex = 0;
						var item = ((ListBoxItem)_listBox.SelectedItem);
						item.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => {
							item.Focus();
						}));
						// A+ framework confirmed
					}
				}));
			} else if (e.Key == System.Windows.Input.Key.Enter) {
				var sel = _listBox.SelectedItem;
				if (sel != null) {
					var li = (ListBoxItem)sel;
					var or = li.GetValue(ObjectRowProp);
					if (or != null) {
						_selectObj.TrySetResult((ObjectRow)or);
					}
				}
			}
		}

		public Task<ObjectRow> SelectObject {
			get { return _selectObj.Task; }
		}

		void _listBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
			if (e.Key == System.Windows.Input.Key.Up) {
				var item = (ListBoxItem)_listBox.SelectedItem;
				var idx = (int)item.GetValue(IndexProp);
				if (idx == 0) {
					_listBox.SelectedItem = null;
					_searchField._textBox.Focus();
				}
			}
		}
		void AddParts(TextBlock tb, MatchResult result, bool isSchema) {
			var stopI = 0;
			var strI = 0;
			var matchI = 0;
			string s = null;

			string whole = isSchema ? result.obj.schema : result.obj.name;

			// Schema parts
			while (strI < whole.Length && matchI < result.matches.Count) {
				var match = result.matches[matchI];
				if (match.isSchemaMatch == isSchema && match.start == strI) {
					if (strI > stopI) {
						s = whole.Substring(stopI, strI - stopI);
						tb.Inlines.Add(new Run(s));
						stopI = strI;
					}
					s = whole.Substring(match.start, match.end - match.start);
					tb.Inlines.Add(new Bold(new Run(s)));
					stopI = strI = match.end;
					if (matchI < result.matches.Count - 1) {
						matchI += 1;
					}
				} else {
					strI += 1;
				}
			}
			if (stopI < whole.Length) {
				s = whole.Substring(stopI);
				if (isSchema) {
					tb.Inlines.Add(new Run(s + "."));
				} else {
					tb.Inlines.Add(new Run(s));
				}
			} else if (isSchema) {
				tb.Inlines.Add(new Run("."));
			}
		}

		public static readonly DependencyProperty IndexProp =
			DependencyProperty.RegisterAttached("Index",
				typeof(int),
				typeof(ObjectFinder),
				new PropertyMetadata(default(int)));

		public static readonly DependencyProperty ObjectRowProp =
			DependencyProperty.RegisterAttached("ObjectRowProp",
				typeof(ObjectRow),
				typeof(ObjectFinder),
				new PropertyMetadata(default(ObjectRow)));
	}
}
