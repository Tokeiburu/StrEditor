using ErrorManager;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.OpenGLComponents;
using StrEditor.Core.Viewport.Renderers;
using StrEditor.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Utilities;
using Utilities.Extension;
using Utilities.Tools;
using Matrix4 = OpenTK.Matrix4;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace StrEditor.Core.Viewport {
	/// <summary>
	/// Interaction logic for FrameViewer.xaml
	/// </summary>
	public partial class FrameViewer : UserControl {
		public ViewportStatistics Stats = new ViewportStatistics();
		private readonly List<Renderer> _renderers = new List<Renderer>();
		public Point RelativeCenter = new Point(0.5, 0.5);
		private StrController _controller = new StrController();
		private bool _crashState;
		private bool _glControlReady;

		public RendererLoadRequest Request;
		public StrController Controller => _controller;
		public InteractionManager InteractionManager;
		public CommandManager Commands;

		public Matrix4 View;
		public Matrix4 Projection;
		public Matrix4 ViewProjection;

		private StrRenderer _strRenderer = new StrRenderer();
		public StrRenderer StrRenderer => _strRenderer;

		public int CenterX => (int)(_primary.Width * RelativeCenter.X);
		public int CenterY => (int)(_primary.Height * RelativeCenter.Y);
		private ZoomEngine _zoomEngine = new ZoomEngine { ZoomInMultiplier = () => StrEditorConfiguration.StrEditorZoomInMultiplier };
		public ZoomEngine ZoomEngine => _zoomEngine;
		public List<LayerRenderer> LayerRenderers => _strRenderer.LayerRenderers;
		private int _previousViewportWidth = -1;
		private int _previousViewportHeight = -1;

		public delegate void ZoomChangedDelegate(object sender, double scale);
		public event ZoomChangedDelegate ZoomChanged;
		protected virtual void OnZoomChanged(double scale) => ZoomChanged?.Invoke(this, scale);

		public FrameViewer() {
			InitializeComponent();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			// These events are added manually because they would crash the designer otherwise
			_primary.Load += _primary_Load;
			_primary.Resize += _primary_Resize;
			_primary.Paint += _primary_Render;

			// Create a dummy load request, this will be used by textures
			Request = new RendererLoadRequest();
			Request.Context = this;
			Request.CancelRequired = () => false;

			ZoomEngine.ZoomFunction = ZoomEngine.DefaultLimitZoom;
		}

		public LayerRenderer GetSelectedRenderer() {
			int index = _controller.TimelineEditor.SelectedLayerIndex;

			if (index < 0 || index >= _controller.Str.Layers.Count || index >= _strRenderer.LayerRenderers.Count)
				return null;

			return _strRenderer.LayerRenderers[index];
		}

		private void _primary_Render(object sender, PaintEventArgs e) {
			if (_crashState || !_glControlReady || _primary.Width <= 0 || _primary.Height <= 0)
				return;

			try {
				GL.Clear(ClearBufferMask.ColorBufferBit);
				GL.Disable(EnableCap.Blend);

				View = Matrix4.Identity;
				Projection = Matrix4.CreateOrthographic((float)(_primary.Width * (1 / _zoomEngine.Scale)), (float)(_primary.Height * (1 / _zoomEngine.Scale)), -1, 2);

				View[3, 0] = (float)((RelativeCenter.X * _primary.Width - _primary.Width / 2d) * (1 / _zoomEngine.Scale));
				View[3, 1] = (float)((-RelativeCenter.Y * _primary.Height + _primary.Height / 2d) * (1 / _zoomEngine.Scale));

				ViewProjection = View * Projection;

				ShapeRenderer.Setup(this);

				foreach (var renderer in _renderers) {
					renderer.Render(this);
				}

				if (Controller.GifData.IsSaving) {
					var result = GLHelper.TakeScreenshot(_primary);
					Controller.GifData.Bitmap = result;
				}

				_primary.SwapBuffers();
			}
			catch (Exception err) {
				_crashState = true;
				try {
					_host.Visibility = Visibility.Collapsed;
					_crashGrid.Visibility = Visibility.Visible;
				}
				catch {
				}

				ErrorHandler.HandleException(err);
			}
		}

		private void _btnResume_Click(object sender, RoutedEventArgs e) {
			_crashGrid.Visibility = Visibility.Collapsed;
			_host.Visibility = Visibility.Visible;
			_crashState = false;
		}

		private void _primary_Resize(object sender, EventArgs e) {
			if (!_glControlReady)
				return;

			if (_previousViewportWidth != _primary.Width || _previousViewportHeight != _primary.Height) {
				GL.Viewport(0, 0, _primary.Width, _primary.Height);
				_previousViewportWidth = _primary.Width;
				_previousViewportHeight = _primary.Height;
			}
		}

		private void _primary_Load(object sender, EventArgs e) {
			OpenGLMemoryManager.CreateInstance(this);
			OpenGLMemoryManager.MakeCurrent(this);
			_primary.MakeCurrent();

			_renderers.Add(new BackgroundRenderer());
			_renderers.Add(new GridRenderer());
			_renderers.Add(_strRenderer);
			_renderers.Add(new GizmoRenderer());
			ShapeRenderer.Init(new Shader("simple_color.vert", "simple_color.frag"));

			GL.ClearColor(new Color4(1f, 0f, 0f, 1f));

			_glControlReady = true;
		}

		public void InitComponent(StrController controller) {
			_controller = controller;
			InteractionManager = new InteractionManager(this);
			_controller.InteractionManager = InteractionManager;

			Commands = new CommandManager(this);

			ZoomChanged += delegate {
				_controller.StrEditorWindow._cbZoom.SelectedIndex = -1;
				_controller.StrEditorWindow._cbZoom.Text = _controller.FrameViewer.ZoomEngine.ScaleText;
			};
		}

		private bool _updatePending = false;

		public void QuickUpdate() {
			Update(false);
		}

		private void _update(bool setDirty = true) {
			if (setDirty && _strRenderer != null) {
				foreach (var layer in _strRenderer.LayerRenderers) {
					if (layer.Inter != null) {
						layer.Inter.Dirty = true;
					}
				}
			}

			if (_controller.Str != null && _controller.TimelineEditor.TimelineFrameIndex >= _controller.Str.KeyFrameCount)
				return;

			_primary_Resize(this, null);
			_primary_Render(this, null);
		}

		public void Update(bool setDirty = true) {
			if (_updatePending) {
				return;
			}

			_updatePending = true;

			Dispatcher.BeginInvoke(new System.Action(delegate {
				_updatePending = false;
				_update(setDirty);
			}), DispatcherPriority.Render);
		}

		internal void ForceUpdate() {
			_update(true);
		}

		private void _framePreview_MouseDown(object sender, MouseEventArgs e) => InteractionManager.OnMouseDown(e);
		private void _framePreview_MouseUp(object sender, MouseEventArgs e) => InteractionManager.OnMouseUp(e);
		private void _framePreview_MouseMove(object sender, MouseEventArgs e) => InteractionManager.OnMouseMove(e);

		private void _framePreview_MouseWheel(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right) return;

			ZoomEngine.Zoom(e.Delta);

			Point mousePosition = new Point(e.Location.X, e.Location.Y);

			// The relative center must be moved as well!
			double diffX = mousePosition.X / _primary.Width - RelativeCenter.X;
			double diffY = mousePosition.Y / _primary.Height - RelativeCenter.Y;

			RelativeCenter.X = mousePosition.X / _primary.Width - diffX / ZoomEngine.OldScale * ZoomEngine.Scale;
			RelativeCenter.Y = mousePosition.Y / _primary.Height - diffY / ZoomEngine.OldScale * ZoomEngine.Scale;

			OnZoomChanged(ZoomEngine.Scale);

			_primary_Render(sender, null);
		}

		public Point ViewportToWorld(Point p) {
			return new Point((p.X - CenterX) * (1 / _zoomEngine.Scale), (p.Y - CenterY) * (1 / _zoomEngine.Scale));
		}

		public Point ViewportToWorld(System.Drawing.Point p) {
			return new Point((p.X - CenterX) * (1 / _zoomEngine.Scale), (p.Y - CenterY) * (1 / _zoomEngine.Scale));
		}

		public void ResetBackground() {
			if (StrEditorConfiguration.BackgroundPath.IsExtension(".bmp", ".tga", ".png", ".jpg")) {
				StrEditorConfiguration.BackgroundPath = StrEditorConfiguration.BackgroundPath.ReplaceExtension("");
				Update();
			}
		}

		public void LoadBackground(string path) {
			if (File.Exists(path)) {
				StrEditorConfiguration.BackgroundPath = path;
				Update();
			}
		}

		public void MouseEventRelease() => _primary.Capture = false;
		public void MouseEventCapture() => _primary.Capture = true;

		public LayerRenderer GetLayerRenderer(int index) {
			if (index < 0 || index >= _controller.Str.Layers.Count || index >= _strRenderer.LayerRenderers.Count)
				return null;

			return _strRenderer.LayerRenderers[index];
		}

		public bool IsLayerVisible(int index) {
			var renderer = GetLayerRenderer(index);
			return renderer != null && renderer.IsVisible;
		}
	}
}
