using StrEditor.Core.TimelineEditor.Controls;
using StrEditor.Core.TimelineEditor.Rendering;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace StrEditor.Core.TimelineEditor {
	public class TimelineIndicatorRenderer : FrameworkElement {
		private List<int> _indicators;
		private Editor _editor;

		public void Init(Editor editor) {
			_editor = editor;
		}

		protected override void OnRender(DrawingContext dc) {
			if (_indicators == null)
				return;

			for (int i = 0; i < _indicators.Count; i++) {
				var text = GetFormattedText(_indicators[i] + "");
				dc.DrawText(text, new Point(_editor.KeyFrameWidth * _indicators[i], (ActualHeight - text.Height) / 2.0));
			}
		}

		public void Set(List<int> indicators) {
			_indicators = indicators;
			this.InvalidateVisual();
		}

		public FormattedText GetFormattedText(string text) {
			if (_cachedStrings.TryGetValue(text, out FormattedText formattedText))
				return formattedText;

			int fontSize = 12;

			formattedText = new FormattedText(text,
				CultureInfo.CurrentUICulture,
				FlowDirection.LeftToRight,
				new Typeface(SystemFonts.MessageFontFamily,
							 FontStyles.Normal,
							 FontWeights.Normal,
							 FontStretches.Normal),
				fontSize,
				Brushes.Black,
				VisualTreeHelper.GetDpi(this).PixelsPerDip);

			_cachedStrings[text] = formattedText;
			return formattedText;
		}

		private static Dictionary<string, FormattedText> _cachedStrings = new Dictionary<string, FormattedText>();
	}
}
