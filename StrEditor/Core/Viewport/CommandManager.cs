using ErrorManager;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.Viewport.Renderers;
using System;

namespace StrEditor.Core.Viewport {
	public class CommandManager {
		private static TkVector2 StrOffsetCenter = new TkVector2(Str.OffsetX, Str.OffsetY);
		private FrameViewer _viewport;

		public CommandManager(FrameViewer viewport) {
			_viewport = viewport;
		}

		public void FlipH() => Execute(v => _flipH(v, StrOffsetCenter));
		public void FlipV() => Execute(v => _flipV(v, StrOffsetCenter));
		public void FlipHSelf() => Execute(v => _flipH(v, v.Inter == null ? StrOffsetCenter : v.Inter.Offset));
		public void FlipVSelf() => Execute(v => _flipV(v, v.Inter == null ? StrOffsetCenter : v.Inter.Offset));
		public void FlipHTexture() => Execute(v => _flipTexture(v, mirrorX: true, mirrorY: false));
		public void FlipVTexture() => Execute(v => _flipTexture(v, mirrorX: false, mirrorY: true));

		private void _flipH(LayerRenderer renderer, in TkVector2 origin) {
			var str = _viewport.Controller.Str;

			if (StrEditorConfiguration.GroupEdit) {
				str.Commands.Begin();

				for (int keyIndex = 0; keyIndex < str.Layers[renderer.LayerIndex].KeyFrames.Count; keyIndex++) {
					str.Commands.FlipH(renderer.LayerIndex, keyIndex, origin);
				}

				str.Commands.End();
			}
			else {
				InterpolatedKeyFrame.ConvertToFrame(renderer.Inter, str);
				str.Commands.Begin();
				str.Commands.FlipH(renderer.LayerIndex, renderer.Inter.KeyIndex, renderer.Inter.Offset);
				str.Commands.End();
			}

			str.InvalidateVisualRedraw();
		}


		private void _flipTexture(LayerRenderer renderer, bool mirrorX, bool mirrorY) {
			var str = _viewport.Controller.Str;

			try {
				str.Commands.Begin();

				for (int keyIndex = 0; keyIndex < str.Layers[renderer.LayerIndex].KeyFrames.Count; keyIndex++) {
					if (mirrorX)
						str.Commands.FlipTextureH(renderer.LayerIndex, keyIndex);
					if (mirrorY)
						str.Commands.FlipTextureV(renderer.LayerIndex, keyIndex);
				}
			}
			finally {
				str.Commands.End();
			}
		}

		private void _flipV(LayerRenderer renderer, in TkVector2 origin) {
			var str = _viewport.Controller.Str;

			if (StrEditorConfiguration.GroupEdit) {
				str.Commands.Begin();

				for (int keyIndex = 0; keyIndex < str.Layers[renderer.LayerIndex].KeyFrames.Count; keyIndex++) {
					str.Commands.FlipV(renderer.LayerIndex, keyIndex, origin);
				}

				str.Commands.End();
			}
			else {
				InterpolatedKeyFrame.ConvertToFrame(renderer.Inter, str);
				str.Commands.Begin();
				str.Commands.FlipV(renderer.LayerIndex, renderer.Inter.KeyIndex, renderer.Inter.Offset);
				str.Commands.End();
			}

			str.InvalidateVisualRedraw();
		}


		private void Execute(Action<LayerRenderer> action) {
			var layerSelected = _viewport.GetSelectedRenderer();

			if (layerSelected == null)
				return;

			try {
				action(layerSelected);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
