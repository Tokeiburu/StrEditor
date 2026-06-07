using GRF.Image;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.OpenGLComponents;
using System.Collections.Generic;
using TokeiLibrary;
using Utilities.Extension;

namespace StrEditor.Core.Viewport.Renderers {
	public class BackgroundRenderer : Renderer {
		private Texture _backTex;
		private Texture _backImageTexture;

		public enum BackgroundImageTextureState {
			NotLoaded,
			Failed,
			Success
		};

		private BackgroundImageTextureState _imageTextureState = BackgroundImageTextureState.NotLoaded;
		private readonly RenderInfo _ri = new RenderInfo();

		private int _previousWidth = 0;
		private int _previousHeight = 0;

		public override void Load(FrameViewer viewport) {
			IsLoaded = true;
			_backTex = new Texture("_APP_background", new GrfImage(ApplicationManager.GetResource("background.png")));
			// Remove it from the memory manager, we'll handle this one ourselves
			OpenGLMemoryManager.DelTextureId(_backTex.Id);

			_backTex.Bind();
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			Shader = new Shader("background.vert", "background.frag");

			_ri.CreateVao();

			Resize(viewport);
		}

		public override void Resize(FrameViewer viewport) {
			if (!IsLoaded || IsUnloaded) {
				return;
			}

			List<Vertex> positions = new List<Vertex>();

			positions.Add(new Vertex(new Vector3(-1.0f, -1.0f, 0.0f), new Vector2(0.0f, 0.0f)));
			positions.Add(new Vertex(new Vector3(1.0f, -1.0f, 0.0f), new Vector2(1.0f, 0.0f)));
			positions.Add(new Vertex(new Vector3(1.0f, 1.0f, 0.0f), new Vector2(1.0f, 1.0f)));
			positions.Add(new Vertex(new Vector3(1.0f, 1.0f, 0.0f), new Vector2(1.0f, 1.0f)));
			positions.Add(new Vertex(new Vector3(-1.0f, 1.0f, 0.0f), new Vector2(0.0f, 1.0f)));
			positions.Add(new Vertex(new Vector3(-1.0f, -1.0f, 0.0f), new Vector2(0.0f, 0.0f)));

			_ri.BindVao();
			_ri.Vertices = positions;
			if (_ri.Vbo == null) {
				_ri.Vbo = new Vbo();
			}
			_ri.Vbo.SetData(_ri.Vertices, BufferUsageHint.StaticDraw);
			_ri.RawVertices = null;
			_ri.Vertices.Clear();

			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

			_previousWidth = viewport._primary.Width;
			_previousHeight = viewport._primary.Height;
		}

		public override void Render(FrameViewer viewport) {
			if (IsUnloaded)
				return;

			if (!IsLoaded) {
				Load(viewport);

				// Force resize
				_previousWidth = -1;
			}

			Shader.Use();

			if (_previousWidth != viewport._primary.Width || _previousHeight != viewport._primary.Height) {
				Resize(viewport);
				Shader.SetVector2("uTexSize", new Vector2(16f, 16f));
				Shader.SetVector2("uViewportSize", new Vector2(viewport._primary.Width, viewport._primary.Height));
			}

			Shader.SetVector2("uRelativeCenter", new Vector2((float)viewport.RelativeCenter.X, (float)viewport.RelativeCenter.Y));

			float scale = (float)viewport.ZoomEngine.Scale;

			if (scale < 0.45)
				scale = 1;

			Shader.SetFloat("uZoom", scale);

			GL.Disable(EnableCap.DepthTest);
			GL.Disable(EnableCap.Blend);
			GL.Enable(EnableCap.Texture2D);

			Texture texture = null;
			var gifData = viewport.Controller.GifData;

			if (gifData.IsGifMode || (gifData.IsPngMode && gifData.IsSaving)) {
				Shader.SetVector4("color", StrEditorConfiguration.GifBackgroundQuick.Color);
				texture = _backTex;
			}
			else if (StrEditorConfiguration.BackgroundPath.IsExtension(".bmp", ".tga", ".png", ".jpg")) {
				string path = StrEditorConfiguration.BackgroundPath;
				// _backImageTexture == null || _backImageTexture.Resource != StrEditorConfiguration.BackgroundPath
				if (_imageTextureState == BackgroundImageTextureState.NotLoaded) {
					_backImageTexture = _tryLoadImageTexture(viewport, path);
					_imageTextureState = _backImageTexture == null ? BackgroundImageTextureState.Failed : BackgroundImageTextureState.Success;
				}
				
				if (_imageTextureState == BackgroundImageTextureState.Success) {
					texture = _backImageTexture;
					Shader.SetVector4("color", new Vector4(1, 1, 1, 0));
					Shader.SetVector2("uTexSize", new Vector2(_backImageTexture.Width, _backImageTexture.Height));
				}
			}
			
			// Uses default background if nothing was previously set
			if (texture == null) {
				if (_imageTextureState == BackgroundImageTextureState.Success) {
					_backImageTexture?.Unload(viewport);
					Shader.SetVector2("uTexSize", new Vector2(16f, 16f));
					_imageTextureState = BackgroundImageTextureState.NotLoaded;
				}

				texture = _backTex;
				Shader.SetVector4("color", StrEditorConfiguration.StrEditorBackgroundColorQuick.Color);
			}

			GL.Enable(EnableCap.Texture2D);
			texture.Bind();

			_ri.BindVao();
			GL.DrawArrays(PrimitiveType.Triangles, 0, _ri.Vbo.Length);
#if DEBUG
			viewport.Stats.DrawArrays_Calls++;
			viewport.Stats.DrawArrays_Calls_VertexLength += _ri.Vbo.Length;
#endif
			GL.Enable(EnableCap.Blend);
		}

		private Texture _tryLoadImageTexture(FrameViewer viewport, string path) {
			Texture texture = null;

			try {
				var data = ResourceManager.GetData(path);

				if (data != null) {
					texture = new Texture(path, new GrfImage(data), true, TextureRenderMode.RepeatBackgroundTexture);
				}
			}
			catch {

			}

			return texture;
		}

		public override void Unload() {
			IsUnloaded = true;

			_ri.Unload();

			_backTex?.Unload();
			_backImageTexture?.Unload();
		}
	}
}
