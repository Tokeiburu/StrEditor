using GRF.Graphics;
using GRF.Image;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using StrEditor.Core.OpenGLComponents;
using System;
using System.Windows;

namespace StrEditor.Core.Viewport.Renderers {
	public static class ShapeRenderer {
		private static Shader _shader;
		private static RenderInfo _image = new RenderInfo();
		private static RenderInfo _rectangle = new RenderInfo();
		private static RenderInfo _line = new RenderInfo();
		private static FrameViewer _viewport;

		public static void Init(Shader shader) {
			_shader = shader;
		}

		public static void Setup(FrameViewer viewport) {
			_shader.Use();
			_shader.SetMatrix4("vp", ref viewport.ViewProjection);
			_shader.SetMatrix4("m", Matrix4.Identity);
			_viewport = viewport;
		}

		public static void DrawRectangle(float x, float y, float scale, in GrfColor color) {
			DrawRectangle(x, y, scale, new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));
		}

		public static void DrawRectangle(float x, float y, float scale, Vector4 color) {
			_loadRectangleRI();

			_rectangle.BindVao();
			
			var model = Matrix4.Identity;
			model[0, 0] = (float)(scale / _viewport.ZoomEngine.Scale);
			model[1, 1] = (float)(scale / _viewport.ZoomEngine.Scale);
			model[3, 0] = x;
			model[3, 1] = y;

			_shader.Use();
			_shader.SetMatrix4("m", ref model);
			_shader.SetVector4("color", ref color);

			GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
		}

		public static void DrawImage(FrameViewer viewport, Shader shader, float x, float y, Texture texture, float scaleX, float scaleY, Vector4 color) {
			_loadImageRI();

			_image.BindVao();

			var model = Matrix4.Identity;
			//model[0, 0] = (float)(scaleY / _viewport.ZoomEngine.Scale);
			//model[1, 1] = (float)(scaleX / _viewport.ZoomEngine.Scale);
			model[0, 0] = scaleX;
			model[1, 1] = scaleY;
			model[3, 0] = x;
			model[3, 1] = y;

			shader.Use();
			shader.SetMatrix4("m", ref model);
			shader.SetMatrix4("vp", ref viewport.ViewProjection);
			shader.SetVector4("color", ref color);

			texture.Bind();

			GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
		}

		public static void DrawRectangle(in Point point, float scale, in Vector4 color) {
			DrawRectangle((float)point.X, (float)point.Y, scale, color);
		}

		public static void DrawRectangle(in TkVector2 point, float scale, in Vector4 color) {
			DrawRectangle(point.X, point.Y, scale, color);
		}

		public static void DrawRectangle(in TkVector2 point, float scale, in GrfColor color) {
			DrawRectangle(point.X, point.Y, scale, color);
		}

		private static void _loadImageRI() {
			if (!_image.VaoCreated()) {
				_image.CreateVao();
				_image.RawVertices = new float[] {
					 0.5f,  0.5f, 0.0f, 1.0f, 0.0f,
					 0.5f, -0.5f, 0.0f, 1.0f, 1.0f,
					-0.5f, -0.5f, 0.0f, 0.0f, 1.0f,
					-0.5f,  0.5f, 0.0f, 0.0f, 0.0f,
				};

				_image.Vbo = new Vbo();
				_image.Vbo.SetData(_image.RawVertices, BufferUsageHint.StaticDraw, 5);
				_image.Ebo = new Ebo();
				_image.Ebo.SetData(new uint[] { 0, 1, 2, 3, 0, 2 }, BufferUsageHint.StaticDraw);

				GL.EnableVertexAttribArray(0);
				GL.EnableVertexAttribArray(1);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
				GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
			}
		}

		private static void _loadRectangleRI() {
			if (!_rectangle.VaoCreated()) {
				_rectangle.CreateVao();
				_rectangle.RawVertices = new float[] {
					 0.5f,  0.5f, 0.0f,
					 0.5f, -0.5f, 0.0f,
					-0.5f, -0.5f, 0.0f,
					-0.5f,  0.5f, 0.0f,
				};

				_rectangle.Vbo = new Vbo();
				_rectangle.Vbo.SetData(_rectangle.RawVertices, BufferUsageHint.StaticDraw, 3);
				_rectangle.Ebo = new Ebo();
				_rectangle.Ebo.SetData(new uint[] { 0, 1, 2, 3, 0, 2 }, BufferUsageHint.StaticDraw);

				GL.EnableVertexAttribArray(0);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
			}
		}

		private static void _loadLineRI() {
			if (!_line.VaoCreated()) {
				_line.CreateVao();
				_line.RawVertices = new float[] {
					 -1f, 0f, 0f,
					  1f, 0f, 0f,
				};

				_line.Vbo = new Vbo();
				_line.Vbo.SetData(_line.RawVertices, BufferUsageHint.StreamDraw, 3);

				GL.EnableVertexAttribArray(0);
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
			}
		}

		public static void DrawLine(in Point p0, in Point p1, in Vector4 color) {
			_loadLineRI();
			
			_line.BindVao();
			
			var model = Matrix4.Identity;
			
			_line.RawVertices = new float[] { (float)p0.X, (float)p0.Y, 0f, (float)p1.X, (float)p1.Y, 0f };
			_line.Vbo.SetSubData(_line.RawVertices, IntPtr.Zero, _line.RawVertices.Length);
			
			_shader.Use();
			_shader.SetMatrix4("m", ref model);
			_shader.SetVector4("color", color);
			GL.DrawArrays(PrimitiveType.Lines, 0, 2);
		}
	}
}
