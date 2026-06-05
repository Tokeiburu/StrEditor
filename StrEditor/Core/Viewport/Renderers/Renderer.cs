namespace StrEditor.Core.Viewport.Renderers {
	public abstract class Renderer {
		public Shader Shader { get; set; }
		public bool Permanent { get; set; }
		public bool IsLoaded { get; set; }
		public bool IsUnloaded { get; set; }
		protected int _subPass = 0;

		public abstract void Load(FrameViewer viewport);
		public abstract void Render(FrameViewer viewport);
		public virtual void Resize(FrameViewer viewport) {

		}

		public abstract void Unload();
	}
}
