using System;
using System.Windows.Forms;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.Viewport.Renderers;
using StrEditor.Core.Viewport.Tools;

namespace StrEditor.Core.Viewport {
	// Dummy class
	public class BiasTool : EditTool {
		public override void BeginEvent(FrameViewer viewport, LayerRenderer renderer) {
			base.BeginEvent(viewport, renderer);
		}

		public override bool EventController(FrameViewer viewport, FrameViewerEventArgs args) {
			return false;
		}

		private void DoEvent(FrameViewer viewport, FrameViewerEventArgs args) {
			viewport.QuickUpdate();
		}

		public void End() {
		}
	}
}
