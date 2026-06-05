using GRF.FileFormats.StrFormat;
using StrEditor.WPF;
using StrEditor.Core.Viewport;
using StrEditor.Core.TimelineEditor.Controls;
using StrEditor.Core.KeyframeEditor;
using StrEditor.Core.GifExporter;

namespace StrEditor.Core {
	public class StrController {
		public Str Str { get; set; }
		public KeyFrameEditor KeyFrameEditor { get; set; }
		public Editor TimelineEditor { get; set; }
		public FrameViewer FrameViewer { get; set; }
		public MainWindow StrEditorWindow;
		public GifEditControl GifEditControl { get; set; }
		public PlayAnimation PlayAnimation { get; set; }
		public InteractionManager InteractionManager { get; set; }
		public GifData GifData;
	}
}
