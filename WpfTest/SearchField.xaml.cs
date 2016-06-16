using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace WpfTest {
	/// <summary>
	/// Interaction logic for SearchField.xaml
	/// </summary>
	public partial class SearchField : UserControl {
		Matcher _matcher;
		public SearchField() {
			InitializeComponent();
		}

		public void InitConnection(string connStr) {
			_matcher = new Matcher(connStr);
		}


		void _textBox_FocusableChanged(object sender, DependencyPropertyChangedEventArgs e) {

		}

		void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			UpdateLabelVis();
		}

		void UpdateLabelVis() {
			if (!String.IsNullOrWhiteSpace(_textBox.Text)) {
				_label.Visibility = Visibility.Hidden;
			} else {
				_label.Visibility = Visibility.Visible;
			}
		}

		void _textBox_KeyDown(object sender, KeyEventArgs e) {

		}

		public delegate void MatchesChangesHandler(object sender, MatchResult[] matches);

		public event MatchesChangesHandler MatchesChanged;

		async void _textBox_KeyUp(object sender, KeyEventArgs e) {
			if (_matcher != null) {
				var matches = await _matcher.GetMatches(_textBox.Text, 10, new CancellationToken());
				MatchesChanged?.Invoke(this, matches);
				//foreach (var m in matches) {
				//	System.Diagnostics.Trace.WriteLine($"Match: {m.obj.name} matches!");
				//}
			}
		}
	}

	public enum ObjectType { Table, ScalarFn, TableFn, StoredProc, View }

	public class ObjectRow {
		public int object_id;
		public string schema;
		public string name;
		public string fullname;
		public ObjectType typ;
	}

	public class MatchResult {
		public ObjectRow obj;
		public int score;
		public List<MatchRange> matches;
	}

	public class MatchRange {
		public int start;
		public int end;
		public bool isSchemaMatch;
	}

	public class Matcher : IDisposable {
		SqlConnection _conn;
		List<ObjectRow> _results;
		Task _getData;

		public Matcher(string connStr) {
			_conn = new SqlConnection(connStr);
			_results = new List<ObjectRow>(100);
			_getData = Task.Run(delegate { GetData(); });
		}

		const int SCHEMA_CHAR_SCORE = 3;
		const int NAME_CHAR_SCORE = 10;
		const float CHAR_OFFSET_PENALTY = 0.1f;

		public async Task<MatchResult[]> GetMatches(string searchTerm, int num, CancellationToken tok) {
			await _getData;

			if (tok.IsCancellationRequested) { return null; }

			var matches = new List<MatchResult>(num * 2);

			var dotIdx = searchTerm.IndexOf('.');
			var schemaSearch = searchTerm;
			var nameSearch = searchTerm;
			if (dotIdx > 0) {
				schemaSearch = searchTerm.Substring(0, dotIdx);
				if (dotIdx < searchTerm.Length) {
					nameSearch = searchTerm.Substring(dotIdx + 1);
				}
			}

			foreach (ObjectRow row in _results) {
				MatchResult match = null;
				int idx;
				if ((idx = row.schema.IndexOf(schemaSearch, StringComparison.OrdinalIgnoreCase)) != -1) {
					var range = new MatchRange { start = idx, end = idx + schemaSearch.Length, isSchemaMatch = true };
					match = new MatchResult {
						obj = row,
						matches = new List<MatchRange> { range }, score = schemaSearch.Length * SCHEMA_CHAR_SCORE
					};
				}

				if ((idx = row.name.IndexOf(nameSearch, StringComparison.OrdinalIgnoreCase)) != -1) {
					var range = new MatchRange { start = idx, end = idx + nameSearch.Length, isSchemaMatch = false };
					if (match == null) {
						match = new MatchResult {
							obj = row,
							matches = new List<MatchRange> { range },
							score = nameSearch.Length * NAME_CHAR_SCORE
						};
					} else {
						match.matches.Add(range);
					}
				}

				if (match != null) {
					match.score *= ObjectTypeSearchWeight(match.obj.typ);
					matches.Add(match);
				}
			}

			return matches
				.OrderByDescending(m => m.score)
				.ThenBy(m => m.obj.fullname)
				.Take(num)
				.ToArray();
		}

		static ObjectType ObjectTypeFromStr(string str) {
			switch (str) {
				case "U ": return ObjectType.Table;
				case "FN": return ObjectType.ScalarFn;
				case "TF": return ObjectType.TableFn;
				case "V ": return ObjectType.View;
				default:
					throw new ArgumentOutOfRangeException(nameof(str));
			}
		}

		static int ObjectTypeSearchWeight(ObjectType typ) {
			switch (typ) {
				case ObjectType.Table: return 10;
				case ObjectType.ScalarFn: return 6;
				case ObjectType.TableFn: return 6;
				case ObjectType.StoredProc: return 4;
				case ObjectType.View: return 8;
				default:
					throw new ArgumentOutOfRangeException(nameof(typ));
			}
		}

		static readonly string OBJECT_QUERY = @"
			select o.object_id, sc.name, o.name, o.type
			from sys.objects o
			join sys.schemas sc on o.schema_id = sc.schema_id
			where o.type in ('U', 'FN', 'TF', 'V')
		";

		void GetData() {
			_conn.Open();
			using (var cmd = _conn.CreateCommand()) {
				cmd.CommandText = OBJECT_QUERY;
				var rdr = cmd.ExecuteReader();
				while (rdr.Read()) {
					var schema = rdr.GetString(1);
					var name = rdr.GetString(2);
					_results.Add(new ObjectRow {
						object_id = rdr.GetInt32(0),
						schema = schema,
						name = name,
						fullname = schema + "." + name,
						typ = ObjectTypeFromStr(rdr.GetString(3))
					});
				}
			}
		}

		public void Dispose() {
			_conn.Close();
		}
	}
}
