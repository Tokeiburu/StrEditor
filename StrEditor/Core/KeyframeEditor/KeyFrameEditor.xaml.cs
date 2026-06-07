using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ColorPicker.Sliders;
using ErrorManager;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core;
using StrEditor.Core.Viewport.Renderers;
using StrEditor.Core.Viewport;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using StrEditor.WPF;
using System.Security.RightsManagement;

namespace StrEditor.Core.KeyframeEditor {
	/// <summary>
	/// Interaction logic for KeyFrameEditor.xaml
	/// </summary>
	public partial class KeyFrameEditor : UserControl {
		private InterpolatedKeyFrame _currentFrame;
		private InterpolatedKeyFrame _copyFrame;
		private LayerRenderer _layerRenderer;
		private bool _isLoading;
		private bool _fieldEditing;
		private bool _beginBiasEdit;
		private Str _str;
		private TextBox[] _tbPositions;
		private TextBox[] _tbUVs;
		private TextBox[] _tbBezierPositions;
		private StrController _controller;
		private UpdateDispatcher _upKeyFrameLoad = new UpdateDispatcher(100);
		private UpdateDispatcher _upKeyRefreshField = new UpdateDispatcher(50);

		public bool CanEdit => _currentFrame != null;
		public bool HasCopyFrame => _copyFrame != null;
		public InterpolatedKeyFrame CopyFrame => _copyFrame;
		public InterpolatedKeyFrame CurrentFrame => _currentFrame;

		public KeyFrameEditor() {
			InitializeComponent();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			_initializeTextBoxReferences();

			_qcsKeyFrameColor.PreviewColorChanged += _qcsKeyFrameColor_PreviewColorChanged;
			SetUvVisible();
		}

		private void _initializeTextBoxReferences() {
			_tbPositions = new TextBox[8];
			_tbPositions[0] = _tbPositionsTLX;
			_tbPositions[4] = _tbPositionsTLY;
			_tbPositions[1] = _tbPositionsTRX;
			_tbPositions[5] = _tbPositionsTRY;
			_tbPositions[2] = _tbPositionsBRX;
			_tbPositions[6] = _tbPositionsBRY;
			_tbPositions[3] = _tbPositionsBLX;
			_tbPositions[7] = _tbPositionsBLY;

			_tbUVs = new TextBox[4];
			_tbUVs[0] = _tbUVTLX;
			_tbUVs[1] = _tbUVTLY;
			_tbUVs[2] = _tbUVTRX;
			_tbUVs[3] = _tbUVTRY;

			_tbBezierPositions = new TextBox[4];
			_tbBezierPositions[0] = _tbBezierP1X;
			_tbBezierPositions[1] = _tbBezierP1Y;
			_tbBezierPositions[2] = _tbBezierP2X;
			_tbBezierPositions[3] = _tbBezierP2Y;
		}

		private LayerRenderer _currentEditLayer;
		private InteractionManager _interactionManager;
		private List<UIElement> _layerUIElements;

		private void _buttonEditMaxFrames_Click(object sender, RoutedEventArgs e) {
			try {
				InputDialog dialog = new InputDialog("Change the max frame count", "Edit Max Frame", _tbMaxFrames.Text);
				dialog.Owner = WpfUtilities.TopWindow;

				if (dialog.ShowDialog() == true) {
					int maxFrames = FormatConverters.IntOrHexConverter(dialog.Input);

					if (maxFrames <= 0) {
						ErrorHandler.HandleException("You need at least 1 key frame.");
						return;
					}

					_controller.TimelineEditor.Commands.SetMaxKeyFrameCount(maxFrames);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _tbFps_TextChanged(object sender, TextChangedEventArgs e) {
			if (_isLoading || _fieldEditing)
				return;

			var fps = FormatConverters.IntOrHexConverter(_tbFps.Text);

			if (fps <= 0)
				return;

			try {
				_str.Commands.SetFps(fps);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void DisableEvents() => _isLoading = true;
		public void EnableEvents() => _isLoading = false;

		private void _qcsKeyFrameColor_PreviewColorChanged(object sender, Color value) {
			if (_isLoading || _fieldEditing)
				return;

			if (_currentFrame.Interpolated)
				return;

			var layer = _str[_currentFrame.LayerIdx, _currentFrame.KeyIndex];
			layer.Color[0] = value.R;
			layer.Color[1] = value.G;
			layer.Color[2] = value.B;
			layer.Color[3] = value.A;
			_str.InvalidateVisualRedraw();
		}

		public void Init(Str str) {
			_str = str;

			_tbMaxFrames.Text = _str.MaxKeyFrame.ToString(CultureInfo.InvariantCulture);

			bool old = _isLoading;
			_isLoading = true;
			_tbFps.Text = _str.Fps.ToString(CultureInfo.InvariantCulture);
			_isLoading = old;
		}

		public void AsyncUpdate(InterpolatedKeyFrame inter) {
			_controller.PlayAnimation.Stop();
			
			if (_fieldEditing)
				return;

			var layer = inter == null ? null : _str.Layers[inter.LayerIdx];
			_upKeyFrameLoad.Execute(() => Dispatcher.BeginInvoke(new Action(() => Update(layer, inter)), DispatcherPriority.Render));
		}

		public void Update(StrLayer layer, InterpolatedKeyFrame inter) {
			try {
				if (_controller == null)
					return;

				_controller.PlayAnimation.Stop();

				if (_isLoading)
					return;

				if (inter == null) {
					_disableUI();
					return;
				}

				_loadKeyFrame(layer, inter);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _loadKeyFrame(StrLayer layer, InterpolatedKeyFrame inter) {
			_currentFrame = inter;
			_layerRenderer = _controller.FrameViewer.GetSelectedRenderer();

			try {
				_isLoading = true;

				_layerUIElements?.ForEach(p => p.IsEnabled = true);

				SetAngle(inter.Angle);
				SetColor(inter.Color);
				SetOffsetX(inter.Offset.X);
				SetOffsetY(inter.Offset.Y);

				_cbSrc.SelectedIndex = inter.BlendSrc - 1;
				_cbDst.SelectedIndex = inter.BlendDst - 1;

				UpdateTextures(layer, true);

				SetPositions(inter.Positions);
				SetUVs(inter.UVs);
				SetBezier(inter.BezierPositions);

				var keyFrame = layer[inter.KeyIndex];
				SetScaleBias(keyFrame.ScaleBias);
				SetOffsetBias(keyFrame.OffsetBias);
				SetAngleBias(keyFrame.AngleBias);
				UpdateScale(_currentFrame.Positions);

				_isInterpolated.IsChecked = keyFrame.IsInterpolated;
				SetFrameIndex(inter.FrameIndex);

				_cbAnimations.SelectedIndex = (int)inter.AnimationType;

				if (inter.Delay == 0) {
					_tbDelay.Text = "";
				}
				else {
					_tbDelay.Text = (1f / inter.Delay).ToString("0.##");
				}
			}
			finally {
				_isLoading = false;
			}
		}

		private void _disableUI() {
			try {
				_isLoading = true;

				_layerUIElements?.ForEach(p => p.IsEnabled = false);

				SetAngle(0);
				SetColor(new float[4]);
				SetOffsetX(Str.OffsetX);
				SetOffsetY(Str.OffsetY);

				_cbSrc.SelectedIndex = 0;
				_cbDst.SelectedIndex = 0;

				_selectedTexture.SelectedIndex = -1;

				var positions = new float[8];

				SetPositions(positions);
				SetUVs(positions);
				SetBezier(positions);
				SetFrameIndex(_controller.TimelineEditor.TimelineFrameIndex);
				SetScale(0, 0);

				_cbAnimations.SelectedIndex = 0;
				_tbDelay.Text = "";

				SetScaleBias(0);
				SetOffsetBias(0);
				SetAngleBias(0);
			}
			finally {
				_isLoading = false;
			}
		}

		public void InitComponent(StrController controller) {
			_controller = controller;

			_interactionManager = _controller.FrameViewer.InteractionManager;

			_initLabelEvents();

			Loaded += delegate {
				var elements = FindChildren<UIElement>(this);
				elements.Remove(_buttonEditMaxFrames);
				elements.Remove(_buttonEditTextures);
				elements.Remove(_tbFps);
				elements.Remove(_tbMaxFrames);
				_layerUIElements = elements;
			};

			_controller.TimelineEditor.TimelineFrameIndexChanged += _timelineEditor_TimelineFrameIndexChanged;
			_controller.FrameViewer.KeyDown += _frameViewer_KeyDown;
			_controller.FrameViewer.KeyUp += _frameViewer_KeyUp; ;
		}

		private void _timelineEditor_TimelineFrameIndexChanged() {
			this.Dispatch(delegate {
				SetFrameIndex(_controller.TimelineEditor.TimelineFrameIndex);
			});
		}

		public List<T> FindChildren<T>(DependencyObject parent, List<T> children = null) where T : DependencyObject {
			if (children == null)
				children = new List<T>();

			if (parent == null)
				return null;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
				var child = VisualTreeHelper.GetChild(parent, i);

				if (child is T typed &&
					(child is SimpleSlider ||
					child is TextBox ||
					child is CheckBox ||
					child is QuickColorSelector ||
					child is ComboBox)) {
					children.Add(typed);
				}

				FindChildren(child, children);
			}

			return children;
		}

		public void SetUvVisible() {
			Visibility visibility = StrEditorConfiguration.ShowUvCoords ? Visibility.Visible : Visibility.Collapsed;

			_labelUvSpacer.Visibility = visibility;
			_labelUvP1.Visibility = visibility;
			_dockPanelUvT.Visibility = visibility;
		}

		#region Manual field setters
		public void SetOffsetX(float x) {
			_tbPosX.Text = (x - Str.OffsetX).ToString("0.##", CultureInfo.InvariantCulture);
		}

		public void SetOffsetY(float y) {
			_tbPosY.Text = (y - Str.OffsetY).ToString("0.##", CultureInfo.InvariantCulture);
		}

		public void SetOffsets(TkVector2 p) {
			SetOffsetX(p.X);
			SetOffsetY(p.Y);
		}

		public void SetAngle(float angle) {
			_tbAngle.Text = angle.ToString("0.##", CultureInfo.InvariantCulture);
		}

		public void SetColor(float[] color) {
			_tbColorR.Text = color[0].ToString("0.##", CultureInfo.InvariantCulture);
			_tbColorG.Text = color[1].ToString("0.##", CultureInfo.InvariantCulture);
			_tbColorB.Text = color[2].ToString("0.##", CultureInfo.InvariantCulture);
			_tbColorA.Text = color[3].ToString("0.##", CultureInfo.InvariantCulture);
			_qcsKeyFrameColor.Color = Color.FromArgb((byte)color[3], (byte)color[0], (byte)color[1], (byte)color[2]);
		}

		public void SetPositions(float[] positions) {
			for (int i = 0; i < 4; i++) {
				_tbPositions[i].Text = positions[i].ToString("0.##", CultureInfo.InvariantCulture);
				_tbPositions[i + 4].Text = (-positions[i + 4]).ToString("0.##", CultureInfo.InvariantCulture);
			}
		}

		public void SetUVs(float[] positions) {
			if (!StrEditorConfiguration.ShowUvCoords)
				return;

			for (int i = 0; i < 4; i++) {
				_tbUVs[i].Text = positions[i].ToString("0.##", CultureInfo.InvariantCulture);
			}
		}

		public void SetBezier(float[] bezierPositions) {
			for (int i = 0; i < 4; i++) {
				_tbBezierPositions[i].Text = bezierPositions[i].ToString("0.##", CultureInfo.InvariantCulture);
			}
		}

		public void SetFrameIndex(int index) {
			_tbFrameIndex.Text = index.ToString(CultureInfo.InvariantCulture);
		}

		public void SetScaleBias(float bias) {
			_tbScaleBias.Text = bias.ToString(CultureInfo.InvariantCulture);
			_sliderScale.Value = bias;
		}

		public void SetOffsetBias(float bias) {
			_tbOffsetBias.Text = bias.ToString(CultureInfo.InvariantCulture);
			_sliderOffset.Value = bias;
		}

		public void SetAngleBias(float bias) {
			_tbAngleBias.Text = bias.ToString(CultureInfo.InvariantCulture);
			_sliderAngle.Value = bias;
		}

		public void SetScale(float sx, float sy) {
			_tbScaleX.Text = sx.ToString("0.##", CultureInfo.InvariantCulture);
			_tbScaleY.Text = sy.ToString("0.##", CultureInfo.InvariantCulture);
		}

		public void UpdateScale(float[] positions) {
			var renderer = _controller.FrameViewer.GetSelectedRenderer();

			if (renderer == null) {
				SetScale(1, 1);
				return;
			}

			for (int i = 0; i < 8; i++)
				_originalPositions[i] = _currentFrame.Positions[i];

			var scale = _getFrameScale(renderer, true);
			SetScale(scale.X, scale.Y);
		}
		#endregion

		public void InvalidateKeyFrame() {
			var inter = InterpolatedKeyFrame.Interpolate(_controller.Str, _controller.TimelineEditor.SelectedLayerIndex, _controller.TimelineEditor.SelectedFrameIndex);

			AsyncUpdate(inter);
			_controller.FrameViewer.Update();
		}

		public void Copy() {
			if (_currentFrame == null) {
				_copyFrame = null;
				return;
			}

			_copyFrame = InterpolatedKeyFrame.Interpolate(_str, _currentFrame.LayerIdx, _controller.TimelineEditor.SelectedFrameIndex);
		}

		private void _slider_ValueDragStart(object sender, double value) {
			_layerRenderer.Inter.BezierPositions = _currentFrame.BezierPositions;
			_beginBiasEdit = true;
			_interactionManager.SetActiveTool(_interactionManager.BiasTool);
		}

		private void _slider_ValueDragEnd(object sender, double value) {
			_beginBiasEdit = false;
			_interactionManager.SetActiveTool(null);
		}
	}
}
