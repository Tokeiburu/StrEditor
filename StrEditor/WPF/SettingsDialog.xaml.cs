using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GRF.Image;
using GrfToWpfBridge;
using OpenTK;
using StrEditor.ApplicationConfiguration;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using Configuration = TokeiLibrary.Configuration;

namespace StrEditor.WPF {
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class SettingsDialog : TkWindow {
		private readonly MainWindow _strEditor;

		public SettingsDialog() {
			InitializeComponent();
		}

		public SettingsDialog(MainWindow strEditor)
			: base("Settings", "settings.png") {
			_strEditor = strEditor;
			InitializeComponent();
			StrEditorConfiguration.ConfigAsker.AdvancedSettingEnabled = true;
			
			_set(_colorGridLH, () => StrEditorConfiguration.StrEditorGridLineHorizontal, v => StrEditorConfiguration.StrEditorGridLineHorizontalQuick = v);
			_set(_colorGridLV, () => StrEditorConfiguration.StrEditorGridLineVertical, v => StrEditorConfiguration.StrEditorGridLineVerticalQuick = v);
			_set(_colorSpriteBorder, () => StrEditorConfiguration.StrEditorSpriteSelectionBorder, v => StrEditorConfiguration.StrEditorSpriteSelectionBorderQuick = v);

			_setColorFrameViewer2(_colorPreviewPanelBakground, () => StrEditorConfiguration.StrEditorBackgroundColor, StrEditorConfiguration.StrEditorBackgroundColorQuick);
			_setColorFrameViewer(_colorPointTranslationCornerSelected, () => StrEditorConfiguration.StrEditorVertexSelectedColor, v => StrEditorConfiguration.StrEditorVertexSelectedColorQuick = v);
			_setColorFrameViewer(_colorPointTranslationCorner, () => StrEditorConfiguration.StrEditorVertexColor, v => StrEditorConfiguration.StrEditorVertexColorQuick = v);
			_setColorFrameViewer2(_colorPathLine, () => StrEditorConfiguration.StrEditorPathLineColor, StrEditorConfiguration.StrEditorPathLineColorQuick);
			_setColorFrameViewer2(_colorPathNode, () => StrEditorConfiguration.StrEditorPathNodeColor, StrEditorConfiguration.StrEditorPathNodeColorQuick);
			_setColorFrameViewer2(_colorBezierNode1, () => StrEditorConfiguration.BezierNode1, StrEditorConfiguration.BezierNode1Quick);
			_setColorFrameViewer2(_colorBezierNode2, () => StrEditorConfiguration.BezierNode2, StrEditorConfiguration.BezierNode2Quick);
			_setColorFrameViewer2(_colorBezierPath, () => StrEditorConfiguration.BezierLine, StrEditorConfiguration.BezierLineQuick);
			_setColorFrameViewer2(_colorPathCurrentNode, () => StrEditorConfiguration.StrEditorPathNodeCurrentColor, StrEditorConfiguration.StrEditorPathNodeCurrentColorQuick);
			_setColorLayerEditor(_colorLEEase, () => StrEditorConfiguration.LayerEditorEaseColor, StrEditorConfiguration.LayerEditorEaseColorQuick);
			_setColorLayerEditor(_colorLEError, () => StrEditorConfiguration.LayerEditorErrorColor, StrEditorConfiguration.LayerEditorErrorColorQuick);
			_setColorLayerEditor(_colorLEAnimation, () => StrEditorConfiguration.LayerEditorAnimationColor, StrEditorConfiguration.LayerEditorAnimationColorQuick);
			_setColorLayerEditor(_colorLEBezier, () => StrEditorConfiguration.LayerEditorBezierColor, StrEditorConfiguration.LayerEditorBezierColorQuick);
			_setColorFrameViewer2(_colorSelectNode, () => StrEditorConfiguration.StrEditorSelectNodeColor, StrEditorConfiguration.StrEditorSelectNodeColorQuick);

			_mz1.SelectedIndex = StrEditorConfiguration.StrEditorZoomInMultiplier > 0 ? 0 : 1;
			_mz2.SelectedIndex = StrEditorConfiguration.StrEditorZoomInMultiplier > 0 ? 1 : 0;
			
			bool enableEvents = true;
			
			_mz1.SelectionChanged += delegate {
				if (!enableEvents) return;
			
				StrEditorConfiguration.StrEditorZoomInMultiplier = _mz1.SelectedIndex == 0 ? 1 : -1;
			
				enableEvents = false;
				_mz2.SelectedIndex = _mz1.SelectedIndex == 0 ? 1 : 0;
				enableEvents = true;
			};
			
			_mz2.SelectionChanged += delegate {
				if (!enableEvents) return;
			
				StrEditorConfiguration.StrEditorZoomInMultiplier = _mz2.SelectedIndex == 0 ? -1 : 1;
			
				enableEvents = false;
				_mz1.SelectedIndex = _mz2.SelectedIndex == 0 ? 1 : 0;
				enableEvents = true;
			};

			_comboBoxEncoding.Init(null, new TypeSetting<int>(v => StrEditorConfiguration.EncodingCodepage = v, () => StrEditorConfiguration.EncodingCodepage), new TypeSetting<Encoding>(v => EncodingService.DisplayEncoding = v, () => EncodingService.DisplayEncoding));
			LoadShortcuts();
		}

		private void _stateUpdated(object sender, RoutedEventArgs e) {
			_strEditor.Controller.TimelineEditor.Renderer.Reload();
			_strEditor.Controller.KeyFrameEditor.SetUvVisible();
		}

		private void _setColorLayerEditor(QuickColorSelector qcs, Func<object> get, StrEditorConfiguration.QuickSolidColorBrushSetting quick) {
			qcs.Color = (Color)get();
			qcs.Init(StrEditorConfiguration.ConfigAsker.RetrieveSetting(get));

			qcs.ColorChanged += delegate(object sender, Color value) {
				quick.Set(value);
				_strEditor.Controller.TimelineEditor.Renderer.Reload();
			};

			qcs.PreviewColorChanged += delegate(object sender, Color value) {
				quick.Set(value);
			};
		}

		private void _setColorFrameViewer(QuickColorSelector qcs, Func<object> get, Action<Vector4> quick) {
			qcs.Color = (Color)get();
			qcs.Init(StrEditorConfiguration.ConfigAsker.RetrieveSetting(get));

			qcs.ColorChanged += delegate(object sender, Color value) {
				quick(new Vector4(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f));
				_strEditor.Controller.FrameViewer.QuickUpdate();
			};

			qcs.PreviewColorChanged += delegate(object sender, Color value) {
				quick(new Vector4(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f));
				_strEditor.Controller.FrameViewer.QuickUpdate();
			};
		}

		private void _setColorFrameViewer2(QuickColorSelector qcs, Func<object> get, StrEditorConfiguration.QuickColorSetting quick) {
			qcs.Color = (Color)get();
			qcs.Init(StrEditorConfiguration.ConfigAsker.RetrieveSetting(get));

			qcs.ColorChanged += delegate(object sender, Color value) {
				quick.Set(new Vector4(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f));
				_strEditor.Controller.FrameViewer.QuickUpdate();
			};

			qcs.PreviewColorChanged += delegate(object sender, Color value) {
				quick.Set(new Vector4(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f));
				_strEditor.Controller.FrameViewer.QuickUpdate();
			};
		}

		private void _set(QuickColorSelector qcs, Func<GrfColor> get, Action<GrfColor> set) {
			qcs.Color = get().ToColor();
			qcs.Init(StrEditorConfiguration.ConfigAsker.RetrieveSetting(() => get()));
			
			qcs.ColorChanged += delegate(object sender, Color value) {
				set(value.ToGrfColor());
				_strEditor.Controller.FrameViewer.QuickUpdate();
			};
			
			qcs.PreviewColorChanged += delegate(object sender, Color value) {
				StrEditorConfiguration.ConfigAsker.IsAutomaticSaveEnabled = false;
				set(value.ToGrfColor());
				StrEditorConfiguration.ConfigAsker.IsAutomaticSaveEnabled = true;
				_strEditor.Controller.FrameViewer.QuickUpdate();
			};
		}

		private Dictionary<string, SettingsShortcutGenerator.ShortcutVisual> _shortcuts;

		public void LoadShortcuts() {
			_gridShortcuts.Children.Clear();
			_shortcuts = SettingsShortcutGenerator.CreateGrid(StrEditorConfiguration.Remapper, _gridShortcuts);
		}

		private void _fbResetShortcuts_Click(object sender, RoutedEventArgs e) {
			StrEditorConfiguration.Remapper.Clear();
			ApplicationShortcut.ResetBindings();
			ApplicationShortcut.OverrideBindings(StrEditorConfiguration.Remapper);
			LoadShortcuts();
		}

		private void _fbRefreshhortcuts_Click(object sender, RoutedEventArgs e) {
			LoadShortcuts();
		}

		private bool _setEncoding(int encoding) {
			if (EncodingService.SetDisplayEncoding(encoding)) {
				StrEditorConfiguration.EncodingCodepage = encoding;
				return true;
			}
			
			return false;
		}

		protected override void OnClosing(CancelEventArgs e) {
			_strEditor.Activate();
			base.OnClosing(e);
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _tbSearch_TextChanged(object sender, TextChangedEventArgs e) {
			if (_tbSearch.Text == "") {
				foreach (var key in _shortcuts.Values) {
					key.Grid.Visibility = Visibility.Visible;
					key.Label.Visibility = Visibility.Visible;
				}
			}
			else {
				foreach (var key in _shortcuts) {
					if (key.Key.IndexOf(_tbSearch.Text, StringComparison.OrdinalIgnoreCase) > -1) {
						key.Value.Grid.Visibility = Visibility.Visible;
						key.Value.Label.Visibility = Visibility.Visible;
					}
					else {
						key.Value.Grid.Visibility = Visibility.Collapsed;
						key.Value.Label.Visibility = Visibility.Collapsed;
					}
				}
			}
		}

		private void _comboBoxEncoding_EncodingChanged(object sender, GrfToWpfBridge.Application.EncodingArgs enc) {
			if (!_setEncoding(enc.Encoding.CodePage)) {
				enc.Cancel = true;
			}
		}
	}
}