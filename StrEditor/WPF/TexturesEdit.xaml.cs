using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ErrorManager;
using GRF.FileFormats.StrFormat;
using StrEditor.Core.Avalon;
using TokeiLibrary.WPF.Styles;
using Utilities;

namespace StrEditor.WPF {
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class TexturesEdit : TkWindow {
		private readonly Str _str;
		private readonly int _layerIdx;

		public TexturesEdit() {
			InitializeComponent();
		}

		public TexturesEdit(Str str, int layerIdx) : base("Textures edit", "app.ico") {
			InitializeComponent();
			_str = str;
			_layerIdx = layerIdx;
			AvalonHelper.Load(_textEditor);
			_textEditor.Text = string.Join("\r\n", str[_layerIdx].TextureNames.ToArray());
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		protected override void OnClosing(CancelEventArgs e) {
			if (DialogResult == true) {
				List<string> textures = _textEditor.Text.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
				while (textures.Count > 0 && textures.Last() == "") {
					textures.RemoveAt(textures.Count - 1);
				}

				for (int i = 0; i < textures.Count; i++) {
					if (textures[i].Length > 127) {
						ErrorHandler.HandleException("The texture file at " + (i + 1) + " has a name too long. It must be below 128 characters.");
						DialogResult = false;
						e.Cancel = true;
						return;
					}
				}

				if (textures.Count == _str[_layerIdx].TextureNames.Count) {
					if (Methods.ListToString(textures) == Methods.ListToString(_str[_layerIdx].TextureNames))
						return;
				}

				try {
					// Yikes, if textures are deleted, we have to adjust them...
					_str.Commands.ChangeTextures(_layerIdx, textures);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}

			base.OnClosing(e);
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			DialogResult = true;
			Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}
	}
}
