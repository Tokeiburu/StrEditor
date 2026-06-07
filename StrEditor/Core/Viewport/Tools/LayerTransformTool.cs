using StrEditor.Core.Viewport.Renderers;
using System.Windows;
using System.Windows.Input;

namespace StrEditor.Core.Viewport.Tools {
	public partial class LayerTransformTool : EditTool {
		private bool _hasTranslated;
		private bool _hasRotated;
		private bool _hasScaled;
		private ScaleDirection? _favoriteOrientation;

		public enum ScaleDirection {
			Horizontal,
			Vertical,
			Both
		}

		public override void BeginEvent(FrameViewer viewport, LayerRenderer renderer) {
			base.BeginEvent(viewport, renderer);

			_favoriteOrientation = null;
			_hasTranslated = _hasRotated = _hasScaled = false;
		}

		public override bool EventController(FrameViewer viewport, FrameViewerEventArgs args) {
			Point w = viewport.ViewportToWorld(args.MouseArgs.Location);

			switch (args.MouseEventState) {
				case MouseEventState.MouseDown:
					var renderer = viewport.GetSelectedRenderer();

					if (renderer == null || !renderer.IsMouseUnder(w) || args.MouseArgs.Button != System.Windows.Forms.MouseButtons.Left)
						return false;

					viewport.MouseEventCapture();
					BeginEvent(viewport, renderer);
					return true;
				case MouseEventState.MouseMove:
					if (args.MouseArgs.Button != System.Windows.Forms.MouseButtons.Left)
						return false;

					if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control || _hasScaled) {
						DoScale(viewport, args);
					}
					else if (Keyboard.Modifiers == ModifierKeys.Shift || _hasRotated) {
						DoRotate(viewport, args);
					}
					else {
						if (!_hasTranslated && !_hasScaled && !_hasRotated) {
							if (!viewport.InteractionManager.IsSelecedLayerUnderMouse(w)) {
								return false;
							}
						}

						DoTranslate(viewport, args);
					}

					if (_hasTranslated || _hasScaled || _hasRotated) {
						viewport.QuickUpdate();
					}

					return true;
				case MouseEventState.MouseUp:
					if (_hasScaled)
						EndScale();

					if (_hasRotated)
						EndRotate();

					if (_hasTranslated)
						EndTranslate();

					viewport.MouseEventRelease();
					viewport.Update();
					return true;
			}

			return false;
		}
	}
}
