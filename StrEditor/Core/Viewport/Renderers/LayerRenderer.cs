using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using GRF.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.OpenGLComponents;
using System.Collections.Generic;
using System.IO;

namespace StrEditor.Core.Viewport.Renderers {
	public class LayerRenderer : Renderer {
		private RenderInfo _ri = new RenderInfo();
		private List<Texture> _textures = new List<Texture>();
		private int _layerIndex;
		public StrController Controller;
		private FrameViewer _viewport;
		private int _texturesHash;
		public Matrix4 Model;
		public float[] VertexData => _ri.RawVertices;
		public int LayerIndex => _layerIndex;
		public bool IsVisible { get; set; } = true;
		public List<Texture> Textures => _textures;

		public InterpolatedKeyFrame Inter { get; private set; }

		private int _previousLayerIndex;
		private int _previousFrameIndex;

		public LayerRenderer(int layerIdx, StrController controller, FrameViewer viewport, Shader shader) {
			Shader = shader;
			_layerIndex = layerIdx;
			Controller = controller;
			_viewport = viewport;
		}

		public override void Load(FrameViewer viewport) {
			IsLoaded = true;

			_loadTextures(viewport);

			_ri.CreateVao();
			_ri.Vbo = new Vbo();
			_ri.RawVertices = new float[5 * 4];
			_ri.Vbo.SetData(_ri.RawVertices, BufferUsageHint.StreamDraw, 5);
			_ri.Ebo = new Ebo();
			_ri.Ebo.SetData(new uint[] { 0, 1, 2, 3, 0, 2 }, BufferUsageHint.StaticDraw);

			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);

			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
		}

		private void _loadTextures(FrameViewer viewport) {
			foreach (var texture in _textures) {
				texture?.Unload(viewport);
			}

			_textures.Clear();

			foreach (var textureName in Controller.Str.Layers[_layerIndex].TextureNames) {
				var data = ResourceManager.GetData(textureName) ?? ResourceManager.GetData(GrfPath.Combine(Path.GetDirectoryName(Controller.Str.LoadedPath), textureName));

				if (data == null) {
					_textures.Add(null);
				}
				else {
					try {
						_textures.Add(TextureManager.LoadTexture(textureName, data, TextureRenderMode.StrTexture, viewport));
					}
					catch {
						_textures.Add(null);
					}
				}
			}

			_texturesHash = Controller.Str.Layers[_layerIndex].TexturesHash;
		}

		public void SetUV(int id, float u, float v) {
			_ri.RawVertices[5 * id + 3] = u;
			_ri.RawVertices[5 * id + 4] = v;
		}

		public void SetPosition(int id, float x, float y) {
			_ri.RawVertices[5 * id + 0] = x;
			_ri.RawVertices[5 * id + 1] = y;
		}

		public void UpdateVbo() {
			_ri.Vbo.SetData(_ri.RawVertices, BufferUsageHint.StreamDraw, 5);
		}

		public override void Render(FrameViewer viewport) {
			RenderSub(viewport, Controller.TimelineEditor.TimelineFrameIndex, _layerIndex, true);
		}

		public void RenderSub(FrameViewer viewport, int frameIndex, int layerIndex, bool render) {
			if (!IsLoaded) {
				Load(viewport);
			}

			// Is layer hidden? This condition is ignored if this function is called only to retrieve the data.
			if (render && !IsVisible)
				return;


			if (layerIndex >= Controller.Str.Layers.Count)
				return;

			if (_previousLayerIndex == layerIndex && _previousFrameIndex == frameIndex && Inter != null && !Inter.Dirty) {
			}
			else {
				Inter = InterpolatedKeyFrame.Interpolate(Controller.Str, layerIndex, frameIndex);
				_previousLayerIndex = layerIndex;
				_previousFrameIndex = frameIndex;
			}

			_layerIndex = layerIndex;

			if (Inter == null)
				return;

			Shader.Use();
			Shader.SetVector4("color", new Vector4(Inter.Color[0] / 255f, Inter.Color[1] / 255f, Inter.Color[2] / 255f, Inter.Color[3] / 255f));

			SetPosition(0, Inter.Positions[2], -Inter.Positions[6]);
			SetPosition(1, Inter.Positions[1], -Inter.Positions[5]);
			SetPosition(2, Inter.Positions[0], -Inter.Positions[4]);
			SetPosition(3, Inter.Positions[3], -Inter.Positions[7]);
			var uv0x = Inter.UVs[0];
			var uv0y = Inter.UVs[1];
			var uv1x = Inter.UVs[2];
			var uv1y = Inter.UVs[3];

			if (_texturesHash != Controller.Str.Layers[_layerIndex].TexturesHash) {
				_loadTextures(viewport);
			}

			Texture texture = null;

			if (Inter.TextureIndex >= 0 && Inter.TextureIndex < _textures.Count) {
				texture = _textures[Inter.TextureIndex];

				if (texture != null && texture.RequiresPoTAdjust) {
					// The client uses a power of two texture buffer, so we emulate that.
					var sx = texture.Width / (float)texture.PotWidth;
					var sy = texture.Height / (float)texture.PotHeight;
					uv0x *= sx;
					uv0y *= sy;
					uv1x *= sx;
					uv1y *= sy;
				}
			}
			
			SetUV(0, uv0x + uv1x, uv1y + uv0y);
			SetUV(1, uv0x + uv1x, uv0y);
			SetUV(2, uv0x, uv0y);
			SetUV(3, uv0x, uv1y + uv0y);

			// This is a custom property, it's not part of the STR structure.
			// It is used to preview scaling changes through the Viewport.
			// These values are 
			if (Inter.Scale.X != 0 || Inter.Scale.Y != 0) {
				UpdatePreviewScalingData();
			}

			UpdateVbo();

			Model = Matrix4.CreateRotationZ(GRF.Graphics.MathHelper.DegreesToRadians(-Inter.Angle));
			Model[3, 0] = Inter.Offset.X - Str.OffsetX;
			Model[3, 1] = -(Inter.Offset.Y - Str.OffsetY);

			if (!render)
				return;

			if (texture == null)
				return;

			if (texture.IsDithered) {
				Shader.SetFloat("cutoff", texture.Cutoff / 255f);
			}
			else {
				Shader.SetFloat("cutoff", 0f);
			}

			texture.Bind();

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(GLHelper.GetOpenGlBlendFromDirectXSrc(Inter.BlendSrc), GLHelper.GetOpenGlBlendFromDirectXDest(Inter.BlendDst));

			Shader.SetMatrix4("m", ref Model);
			_ri.BindVao();
			GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
		}

		public void UpdatePreviewScalingData() {
			float[] positions = new float[8];
			float x = 0;
			float y = 0;
			TkVector2[] points = new TkVector2[4];

			for (int i = 0; i < 4; i++) {
				x += Inter.Positions[i];
				y += Inter.Positions[i + 4];

				points[i] = new TkVector2(Inter.Positions[i], Inter.Positions[i + 4]);
			}

			x /= 4;
			y /= 4;

			TkVector2 m = new TkVector2(x, y);

			for (int i = 0; i < 4; i++) {
				TkVector2 p = (points[i] - m);
				p.X *= Inter.Scale.X;
				p.Y *= Inter.Scale.Y;

				p += m;

				positions[i] = p.X;
				positions[i + 4] = p.Y;
			}

			SetPosition(0, positions[2], -positions[6]);
			SetPosition(1, positions[1], -positions[5]);
			SetPosition(2, positions[0], -positions[4]);
			SetPosition(3, positions[3], -positions[7]);
		}

		public override void Unload() {
			IsUnloaded = true;
			_ri?.Unload();

			foreach (var texture in _textures)
				texture?.Unload(_viewport);
		}

		public void DrawSelection() {
			Shader.Use();
			Shader.SetBool("selection", true);

			_ri.BindVao();
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			Shader.SetMatrix4("m", ref Model);
			Shader.SetVector4("color", StrEditorConfiguration.StrEditorSpriteSelectionBorderQuick.ToVector4());
			GL.DrawElements(PrimitiveType.LineLoop, 4, DrawElementsType.UnsignedInt, 0);
			Shader.SetBool("selection", false);
		}

		public bool IsMouseUnder(System.Windows.Point point) {
			if (Inter == null)
				return false;
			
			Vector4[] points = new Vector4[4];

			points[0] = new Vector4(Inter.Positions[2], -Inter.Positions[6], 0, 0);
			points[1] = new Vector4(Inter.Positions[1], -Inter.Positions[5], 0, 0);
			points[2] = new Vector4(Inter.Positions[0], -Inter.Positions[4], 0, 0);
			points[3] = new Vector4(Inter.Positions[3], -Inter.Positions[7], 0, 0);

			Vector4 trans = new Vector4(Model[3, 0], Model[3, 1], 0, 0);

			for (int i = 0; i < 4; i++) {
				points[i] *= Model;
				points[i] += trans;
			}

			Vector4 m = new Vector4((float)point.X, -(float)point.Y, 0, 0);
			Vector4 a = points[0];
			Vector4 b = points[1];
			Vector4 c = points[2];

			bool b1 = _sign(m, a, b) < 0;
			bool b2 = _sign(m, b, c) < 0;
			bool b3 = _sign(m, c, a) < 0;

			if ((b1 == b2) && (b2 == b3))
				return true;

			a = points[2];
			b = points[3];
			c = points[0];

			b1 = _sign(m, a, b) < 0;
			b2 = _sign(m, b, c) < 0;
			b3 = _sign(m, c, a) < 0;

			if ((b1 == b2) && (b2 == b3))
				return true;

			return false;
		}

		private float _sign(Vector4 p1, Vector4 p2, Vector4 p3) {
			return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
		}
	}
}
