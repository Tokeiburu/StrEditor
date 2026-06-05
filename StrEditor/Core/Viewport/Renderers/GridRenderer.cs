using OpenTK;
using OpenTK.Graphics.OpenGL;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.OpenGLComponents;
using System.Collections.Generic;

namespace StrEditor.Core.Viewport.Renderers {
	public class GridRenderer : Renderer {
		private RenderInfo _ri = new RenderInfo();
		public Matrix4 Model = Matrix4.Identity;

		public override void Load(FrameViewer viewport) {
			IsLoaded = true;
			Shader = new Shader("simple_color.vert", "simple_color.frag");

			if (!_ri.VaoCreated()) {
				_ri.CreateVao();
			
				_ri.Vbo = new Vbo();

				_ri.Vertices = new List<Vertex>();
				_ri.Vertices.Add(new Vertex(new Vector3(0, 0, 0)));
				_ri.Vertices.Add(new Vertex(new Vector3(0, 0, 0)));
				_ri.Vbo.SetData(_ri.Vertices, BufferUsageHint.StaticDraw);
				
				GL.EnableVertexAttribArray(0);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
			}
		}

		public override void Render(FrameViewer viewport) {
			if (!IsLoaded) {
				Load(viewport);
			}

			if (viewport.Controller.GifData.IsSaving)
				return;

			Shader.Use();
			Shader.SetMatrix4("m", ref Model);
			Shader.SetMatrix4("vp", ref viewport.ViewProjection);

			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			_ri.BindVao();

			_drawHorizontal(viewport);
			_drawVertical(viewport);
		}

		private void _drawVertical(FrameViewer viewport) {
			float viewHeight = (float)(viewport._primary.Height / viewport.ZoomEngine.Scale);
			float minY = (float)(viewport.RelativeCenter.Y * viewHeight);
			float maxY = minY - viewHeight;

			Shader.SetVector4("color", StrEditorConfiguration.StrEditorGridLineVerticalQuick.ToVector4());
			_ri.Vertices[0] = new Vertex(new Vector3(0, minY, 0));
			_ri.Vertices[1] = new Vertex(new Vector3(0, maxY, 0));
			_ri.Vbo.SetData(_ri.Vertices, BufferUsageHint.StaticDraw);
			GL.DrawArrays(PrimitiveType.Lines, 0, _ri.Vbo.Length);
		}

		private void _drawHorizontal(FrameViewer viewport) {
			float viewWidth = (float)(viewport._primary.Width / viewport.ZoomEngine.Scale);
			float minX = -(float)(viewport.RelativeCenter.X * viewWidth);
			float maxX = minX + viewWidth;

			Shader.SetVector4("color", StrEditorConfiguration.StrEditorGridLineHorizontalQuick.ToVector4());
			_ri.Vertices[0] = new Vertex(new Vector3(minX, 0, 0));
			_ri.Vertices[1] = new Vertex(new Vector3(maxX, 0, 0));
			_ri.Vbo.SetData(_ri.Vertices, BufferUsageHint.StaticDraw);
			GL.DrawArrays(PrimitiveType.Lines, 0, _ri.Vbo.Length);
		}

		public override void Unload() {
			IsUnloaded = true;
			_ri.Unload();
		}
	}
}
