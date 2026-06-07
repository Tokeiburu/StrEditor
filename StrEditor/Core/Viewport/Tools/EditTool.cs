using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.KeyframeEditor;
using StrEditor.Core.Viewport.Renderers;
using System.Windows;

namespace StrEditor.Core.Viewport.Tools {
	public class ToolHandle {
		public Rect MouseArea;
		public int Id;
		public EditTool Tool;

		public ToolHandle(int id) {
			Id = id;
		}

		public ToolHandle(in Rect mouseArea, int id = 0, EditTool tool = null) {
			MouseArea = mouseArea;
			Id = id;
			Tool = tool;
		}
	}

	public abstract class EditTool {
		protected LayerRenderer _renderer;
		protected Str _str;
		protected FrameViewer _viewport;
		protected KeyFrameEditor _kfe;
		protected InterpolatedKeyFrame _keyFrameCopy;
		protected StrLayer _layerCopy;

		public virtual void BeginEvent(FrameViewer viewport, LayerRenderer renderer) {
			_renderer = renderer;
			_str = viewport.Controller.Str;
			_viewport = viewport;
			_kfe = viewport.Controller.KeyFrameEditor;

			SaveInitialData(viewport);
		}

		public void SaveInitialData(FrameViewer viewport) {
			if (_renderer.Inter == null)
				return;

			_str = viewport.Controller.Str;
			_keyFrameCopy = new InterpolatedKeyFrame(_renderer.Inter);
			_keyFrameCopy.Scale = new TkVector2(1, 1);

			for (int i = 0; i < 8; i++) {
				_keyFrameCopy.Positions[i] = _renderer.Inter.Positions[i];
			}

			for (int i = 0; i < 4; i++) {
				_keyFrameCopy.BezierPositions[i] = _renderer.Inter.BezierPositions[i];
			}

			for (int i = 0; i < 8; i++) {
				_keyFrameCopy.UVs[i] = _renderer.Inter.UVs[i];
			}

			if (StrEditorConfiguration.GroupEdit) {
				_layerCopy = new StrLayer(_str[_renderer.LayerIndex]);
			}
		}

		public abstract bool EventController(FrameViewer viewport, FrameViewerEventArgs args);
	}
}
