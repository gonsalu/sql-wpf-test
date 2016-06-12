using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfTest {
	public static class UIHelpers {
		public static Style COLLAPSE_STYLE, VISIBLE_STYLE;

		static UIHelpers() {
			COLLAPSE_STYLE = new Style();
			COLLAPSE_STYLE.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));

			VISIBLE_STYLE = new Style();
			VISIBLE_STYLE.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible));
		}
	}
}
