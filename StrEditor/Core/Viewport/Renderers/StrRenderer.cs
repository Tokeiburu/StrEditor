using GRF.Image;
using OpenTK;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.OpenGLComponents;
using System.Collections.Generic;
using TokeiLibrary;

namespace StrEditor.Core.Viewport.Renderers {
	public class StrRenderer : Renderer {
		public List<LayerRenderer> LayerRenderers = new List<LayerRenderer>();
		private Texture _referenceTexture;

		public override void Load(FrameViewer viewport) {
			IsLoaded = true;
			Shader = new Shader("str.vert", "str.frag");
		}

		public override void Render(FrameViewer viewport) {
			if (!IsLoaded) {
				Load(viewport);
			}

			Shader.Use();
			Shader.SetMatrix4("vp", ref viewport.ViewProjection);

			var gifData = viewport.Controller.GifData;

			if (StrEditorConfiguration.DrawReferenceSprite && !gifData.IsGifMode && !gifData.IsPngMode) {
				if (_referenceTexture == null) {
					_referenceTexture = new Texture("APP_reference_sprite", new GrfImage(ApplicationManager.GetResource("actor_dummy.png")), true, TextureRenderMode.NearestNeighbor);
				}

				ShapeRenderer.DrawImage(viewport, Shader, -1, 40, _referenceTexture, _referenceTexture.Width, _referenceTexture.Height, new Vector4(1, 1, 1, 1));
			}

			var controller = viewport.Controller;
			int layerCount = controller.Str.Layers.Count;

			if (LayerRenderers.Count < layerCount) {
				for (int i = LayerRenderers.Count; i < layerCount; i++) {
					LayerRenderers.Add(new LayerRenderer(i, controller, viewport, Shader));
				}
			}

			var frameIndex = viewport.Controller.TimelineEditor.TimelineFrameIndex;

			for (int lidx = 0; lidx < layerCount; lidx++) {
				LayerRenderers[lidx].RenderSub(viewport, frameIndex, lidx, true);
			}

			for (int lidx = layerCount; lidx < LayerRenderers.Count; lidx++) {
				LayerRenderers[lidx].Unload();
				LayerRenderers.RemoveAt(lidx);
				lidx--;
			}
		}

		public override void Unload() {
			IsUnloaded = true;

			foreach (var renderer in LayerRenderers)
				renderer.Unload();

			_referenceTexture?.Unload();
		}
	}
}
