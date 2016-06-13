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

namespace WpfTest {
	public class TabContentView : UserControl {
		static IHighlightingDefinition HIGHLIGHTING_DEF;

		static TabContentView() {
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WpfTest.Sql.xshd"))
			using (var reader = new System.Xml.XmlTextReader(stream)) {
				HIGHLIGHTING_DEF = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
			}
		}

		static readonly Brush RED_ERROR_FOREGROUND = new SolidColorBrush(Color.FromRgb(183, 28, 28));

		SqlConnection _conn;
		bool _haveResults;
		GridSplitter _splitter;
		DataGrid _dataGrid;
		TextEditor _avEdit;
		Label _errorLabel;
		Exception _lastEx;
		DockPanel _panel;
		ConnectionStatusBar _statusBar;
		bool _querying;
		RowDefinition _bottomRowDef;

		public TabContentView(SqlConnection conn) {
			_conn = conn;

			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
			grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

			var subGrid = new Grid();
			subGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			subGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) });
			_bottomRowDef = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            subGrid.RowDefinitions.Add(_bottomRowDef);

			_avEdit = new TextEditor() {
				FontFamily = new FontFamily("Consalas"),
				FontSize = 16,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
				WordWrap = true,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto
			};
			_avEdit.SetValue(Grid.RowProperty, 0);
			_avEdit.Loaded += _avEdit_Loaded;
			_avEdit.SyntaxHighlighting = HIGHLIGHTING_DEF;
			this.KeyUp += TabContentView_KeyUp;
			subGrid.Children.Add(_avEdit);

			_splitter = new GridSplitter { HorizontalAlignment = HorizontalAlignment.Stretch, Style = COLLAPSE_STYLE };
			_splitter.SetValue(Grid.RowProperty, 1);
			subGrid.Children.Add(_splitter);

			_panel = new DockPanel();
			_panel.SetValue(Grid.RowProperty, 2);
			_dataGrid = new DataGrid {
				//MaxHeight = 500,
				Style = COLLAPSE_STYLE,
				EnableColumnVirtualization = true,
				EnableRowVirtualization = true, 
				AutoGenerateColumns = false
			};
			_dataGrid.IsReadOnly = true;
			_panel.Children.Add(_dataGrid);

			_errorLabel = new Label();
			_errorLabel.Style = COLLAPSE_STYLE;
			_panel.Children.Add(_errorLabel);
			subGrid.Children.Add(_panel);
			grid.Children.Add(subGrid);

			_statusBar = new ConnectionStatusBar(_conn);
			_statusBar.SetValue(Grid.RowProperty, 1);
			grid.Children.Add(_statusBar);

			this.Content = grid;

			InitializeTextMarkerService();
		}

		private void _avEdit_Loaded(object sender, RoutedEventArgs e) {
			_avEdit.Focus();
		}

		ITextMarkerService _textMarkerService;

		void InitializeTextMarkerService() {
			var textMarkerService = new TextMarkerService(_avEdit.Document);
			_avEdit.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
			_avEdit.TextArea.TextView.LineTransformers.Add(textMarkerService);
			
			var services = (IServiceContainer)_avEdit.Document.ServiceProvider.GetService(typeof(IServiceContainer));
			if (services != null)
				services.AddService(typeof(ITextMarkerService), textMarkerService);
			_textMarkerService = textMarkerService;
		}

		public string GetActiveConnectionString() {
			var connBuilder = new SqlConnectionStringBuilder();
			connBuilder.ConnectionString = _conn.ConnectionString;
			connBuilder.InitialCatalog = _statusBar.ChosenInitialCatalog;
			return connBuilder.ConnectionString;
		}

		volatile Task _queryTask;
		CancellationToken _queryCancellationToken;

		void TabContentView_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
			System.Diagnostics.Trace.WriteLine($"Key: {e.Key.ToString()}");
			_errorLabel.Content = "";
			if (e.Key == System.Windows.Input.Key.F5 && !_querying) {
				_querying = true;
				_lastEx = null;
				_haveResults = false;
				ClearErrors();
				UpdateGridVis();
				_statusBar.ToggleLoading(true);

				var txt = _avEdit.Text;
				var connStr = GetActiveConnectionString();

				_queryCancellationToken = new CancellationToken();

				var queryTask = Task.Run(() => {
					RunQuery(connStr, txt);
				}, _queryCancellationToken);
			}
		}

		void RunQuery(string connStr, string queryTxt) {
			Thread.Sleep(10);
			using (var conn = new SqlConnection(connStr))
			using (var cmd = conn.CreateCommand()) {
				try {
					conn.Open();
					cmd.CommandText = queryTxt;
					var rdr = cmd.ExecuteReader();

					var buffer = new List<ExpandoObject>(500);
					do {
						var fieldCount = rdr.FieldCount;

						this.Dispatcher.Invoke(DispatcherPriority.DataBind, new Action(() => {
							_dataGrid.Items.Clear();
							_dataGrid.Columns.Clear();
							var dgCols = new DataGridTextColumn[fieldCount];
							for (int i = 0; i < fieldCount; i++) {
								var colname = rdr.GetName(i);
								if (String.IsNullOrEmpty(colname)) {
									colname = "Column " + (i + 1).ToString();
								}
								dgCols[i] = new DataGridTextColumn {
									Binding = new Binding(i.ToString()),
									Header = colname
								};
							}

							foreach (var col in dgCols) {
								_dataGrid.Columns.Add(col);
							}
							_haveResults = true;
							UpdateGridVis();
						}));

						var swSync = Stopwatch.StartNew();
						while (rdr.Read()) {
							_haveResults = true;
							var vals = new object[fieldCount];
							rdr.GetValues(vals);

							var newRow = new ExpandoObject(); // PERF - Should be more like this: http://stackoverflow.com/a/8890435
							for (int i = 0; i < fieldCount; i++) {
								((IDictionary<string, object>)newRow)[i.ToString()] = vals[i];
							}
							buffer.Add(newRow);

							if (buffer.Count == buffer.Capacity || swSync.ElapsedMilliseconds > 100) {
								Dispatcher.Invoke(DispatcherPriority.DataBind, new Action(() => {
									foreach (var r in buffer) { _dataGrid.Items.Add(r); }
								}));
								buffer.Clear();
								swSync.Restart();
							}
						}

						if (buffer.Count > 0) {
							Dispatcher.Invoke(DispatcherPriority.DataBind, new Action(() => {
								foreach (var row in buffer) { _dataGrid.Items.Add(row); }
							}));
							buffer.Clear();
						}
					} while (rdr.NextResult());
				} catch (SqlException ex) {
					Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() => {
						UpdateErrorMarkers(ex);
						_lastEx = ex;
						_errorLabel.Content = ex.Message;
						_errorLabel.Foreground = RED_ERROR_FOREGROUND;
						UpdateGridVis();
					}));
				} catch (Exception ex) {
					Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() => {
						_lastEx = ex;
					}));
				} finally {
					Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() => {
						_querying = false;
						_statusBar.ToggleLoading(false);
					}));
				}
			}
		}

		void ClearErrors() => _textMarkerService.RemoveAll(m => true);

		void UpdateErrorMarkers(SqlException ex) {
			var doc = _avEdit.Document;

			foreach (SqlError err in ex.Errors) {
				var line = doc.GetLineByNumber(err.LineNumber);
				var marker = _textMarkerService.Create(line.Offset, line.Length);
				marker.MarkerTypes = TextMarkerTypes.SquigglyUnderline;
				marker.MarkerColor = Colors.Red;
			}
		}

		void UpdateGridVis() {
			var erVis = _lastEx != null ? VISIBLE_STYLE : COLLAPSE_STYLE;
			var dgVis = _haveResults ? VISIBLE_STYLE : COLLAPSE_STYLE;
			_errorLabel.Style = erVis;
			_dataGrid.Style = dgVis;

			if (erVis == COLLAPSE_STYLE && dgVis == COLLAPSE_STYLE) {
				_panel.Style = _splitter.Style = COLLAPSE_STYLE;
				_bottomRowDef.Height = new GridLength(1, GridUnitType.Auto);
            } else {
				_panel.Style = _splitter.Style = VISIBLE_STYLE;
				_bottomRowDef.Height = new GridLength(1, GridUnitType.Star);
            }
		}
	}
}
