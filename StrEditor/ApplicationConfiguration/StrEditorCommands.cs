using TokeiLibrary.Shortcuts;

namespace StrEditor.ApplicationConfiguration {
	public static class StrEditorCommands {
		// Duplicate default commands, not particular use, it's just cleaner
		public static TkCommand Save = ApplicationShortcut.Save;
		public static TkCommand New = ApplicationShortcut.New;
		public static TkCommand Open = ApplicationShortcut.Open;
		public static TkCommand Copy = ApplicationShortcut.Copy;
		public static TkCommand Paste = ApplicationShortcut.Paste;
		public static TkCommand Cut = ApplicationShortcut.Cut;
		public static TkCommand Delete = ApplicationShortcut.Delete;
		public static TkCommand Undo = ApplicationShortcut.Undo;
		public static TkCommand Redo = ApplicationShortcut.Redo;
		public static TkCommand SaveAs = ApplicationShortcut.FromString("Ctrl-Shift-S", "Application.SaveAs");
		public static TkCommand MoveAt = ApplicationShortcut.FromString("Ctrl-T", "ListData.MoveAt");

		// Script runner commands
		public static TkCommand ScriptRunnerRunScript = ApplicationShortcut.FromString("Ctrl-R", "ScriptRunner.RunScript");

		// Keyframe editor commands
		public static TkCommand KeyFrameEditorCreateBezier = ApplicationShortcut.FromString("B", "KeyFrameEditor.CreateBezier");
		public static TkCommand KeyFrameEditorDeleteBezier = ApplicationShortcut.FromString("Shift-B", "KeyFrameEditor.DeleteBezier");
		public static TkCommand KeyFrameEditorCenterOrigin = ApplicationShortcut.FromString("Ctrl-Shift-O", "KeyFrameEditor.CenterOrigin");
		public static TkCommand KeyFrameEditorCopy = ApplicationShortcut.FromString("Ctrl-C", "KeyFrameEditor.Copy");
		public static TkCommand KeyFrameEditorPaste = ApplicationShortcut.FromString("Ctrl-V", "KeyFrameEditor.Paste");
		public static TkCommand KeyFrameEditorDelete = ApplicationShortcut.FromString("Delete", "KeyFrameEditor.Delete");
		public static TkCommand KeyFrameEditorDeleteAll = ApplicationShortcut.FromString("Shift-Delete", "KeyFrameEditor.DeleteAll");
		public static TkCommand KeyFrameEditorInterpolate = ApplicationShortcut.FromString("I", "KeyFrameEditor.Interpolate");
		public static TkCommand KeyFrameEditorDeleteInterpolate = ApplicationShortcut.FromString("Ctrl-W", "KeyFrameEditor.DeleteInterpolate");
		public static TkCommand ViewportTranslateLeft = ApplicationShortcut.FromString("Left", "Viewport.TranslateLeft");
		public static TkCommand ViewportTranslateRight = ApplicationShortcut.FromString("Right", "Viewport.TranslateRight");
		public static TkCommand ViewportTranslateUp = ApplicationShortcut.FromString("Up", "Viewport.TranslateUp");
		public static TkCommand ViewportTranslateDown = ApplicationShortcut.FromString("Down", "Viewport.TranslateDown");
		public static TkCommand LayerEditorInsertUp = ApplicationShortcut.FromString("Ctrl-U", "LayerEditor.InsertUp");
		public static TkCommand LayerEditorInsertDown = ApplicationShortcut.FromString("Ctrl-D", "LayerEditor.InsertDown");
		public static TkCommand KeyFrameEditorNewKey = ApplicationShortcut.FromString("N", "KeyFrameEditor.NewKey");
		public static TkCommand KeyFrameEditorEndKey = ApplicationShortcut.FromString("E", "KeyFrameEditor.EndKey");
		public static TkCommand KeyFrameEditorSetFromPrevious = ApplicationShortcut.FromString("F", "KeyFrameEditor.SetFromPrevious");
		public static TkCommand KeyFrameEditorSelectAll = ApplicationShortcut.FromString("Ctrl-A", "KeyFrameEditor.SelectAll");
		public static TkCommand LayerEditorTextureCopy = ApplicationShortcut.FromString("Alt-C", "LayerEditor.TextureCopy");
		public static TkCommand LayerEditorTexturePaste = ApplicationShortcut.FromString("Alt-V", "LayerEditor.TexturePaste");
		public static TkCommand LayerEditorDuplicate = ApplicationShortcut.FromString("Alt-D", "LayerEditor.Duplicate");
		public static TkCommand LayerEditorDelete = ApplicationShortcut.FromString("Alt-Delete", "LayerEditor.Delete");
		public static TkCommand LayerEditorZoomIn = ApplicationShortcut.FromString("Add", "LayerEditor.ZoomIn");
		public static TkCommand LayerEditorZoomOut = ApplicationShortcut.FromString("Subtract", "LayerEditor.ZoomOut");
		public static TkCommand LayerEditorCopy = ApplicationShortcut.FromString("Ctrl-C", "LayerEditor.Copy");
		public static TkCommand LayerEditorPaste = ApplicationShortcut.FromString("Ctrl-V", "LayerEditor.Paste");
		public static TkCommand KeyFrameEditorMagnify = ApplicationShortcut.FromString("Ctrl-Shift-M", "KeyFrameEditor.Magnify");
		public static TkCommand StrEditorSaveAsGif = ApplicationShortcut.FromString("Ctrl-G", "StrEditor.SaveAsGif");
		public static TkCommand LayerEditorPasteColor = ApplicationShortcut.FromString("Ctrl-Shift-1", "LayerEditor.PasteColor");
		public static TkCommand LayerEditorPasteBlend = ApplicationShortcut.FromString("Ctrl-Shift-2", "LayerEditor.PasteBlend");
		public static TkCommand LayerEditorPasteOffset = ApplicationShortcut.FromString("Ctrl-Shift-3", "LayerEditor.PasteOffset");
		public static TkCommand LayerEditorPasteAngle = ApplicationShortcut.FromString("Ctrl-Shift-4", "LayerEditor.PasteAngle");
		public static TkCommand LayerEditorPastePositions = ApplicationShortcut.FromString("Ctrl-Shift-5", "LayerEditor.PastePositions");
		public static TkCommand LayerEditorPasteTexture = ApplicationShortcut.FromString("Ctrl-Shift-6", "LayerEditor.PasteTexture");
		public static TkCommand LayerEditorPasteAnimation = ApplicationShortcut.FromString("Ctrl-Shift-7", "LayerEditor.PasteAnimation");
		public static TkCommand LayerEditorPasteBias = ApplicationShortcut.FromString("Ctrl-Shift-8", "LayerEditor.PasteBias");
		public static TkCommand LayerEditorPasteBezier = ApplicationShortcut.FromString("Ctrl-Shift-9", "LayerEditor.PasteBezier");
		public static TkCommand FrameViewerFlipHorizontal = ApplicationShortcut.FromString("Ctrl-Shift-H", "FrameViewer.FlipHorizontal");
		public static TkCommand FrameViewerFlipVertical = ApplicationShortcut.FromString("Ctrl-Shift-V", "FrameViewer.FlipVertical");
		public static TkCommand FrameViewerGroupEdit = ApplicationShortcut.FromString("Ctrl-E", "FrameViewer.GroupEdit");

		// Editor commands
		public static TkCommand SaveAsGif = ApplicationShortcut.FromString("Ctrl-G", "Editor.SaveAsGif");
		public static TkCommand ExportAsPng = ApplicationShortcut.FromString("Ctrl-T", "Editor.ExportAsPng");
		public static TkCommand AddBezierCurve = ApplicationShortcut.FromString("Ctrl-B", "Editor.AddBezierCurve");
		public static TkCommand RemoveBezierCurve = ApplicationShortcut.FromString("Shift-B", "Editor.RemoveBezierCurve");
		public static TkCommand CenterOrigin = ApplicationShortcut.FromString("Ctrl-Shift-O", "Editor.CenterOrigin");
		public static TkCommand ScaleCenterKeyFrame = ApplicationShortcut.FromString("Ctrl-Shift-M", "Editor.ScaleCenterKeyFrame");
		public static TkCommand ScaleCenterLayer = ApplicationShortcut.FromString(null, "Editor.ScaleCenterLayer");
		public static TkCommand ScaleCenterStr = ApplicationShortcut.FromString(null, "Editor.ScaleCenterStr");
		public static TkCommand ScaleOriginKeyFrame = ApplicationShortcut.FromString(null, "Editor.ScaleOriginKeyFrame");
		public static TkCommand ScaleOriginLayer = ApplicationShortcut.FromString(null, "Editor.ScaleOriginLayer");
		public static TkCommand ScaleOriginStr = ApplicationShortcut.FromString(null, "Editor.ScaleOriginStr");
		public static TkCommand ScaleWorldKeyFrame = ApplicationShortcut.FromString(null, "Editor.ScaleWorldKeyFrame");
		public static TkCommand ScaleWorldLayer = ApplicationShortcut.FromString(null, "Editor.ScaleWorldLayer");
		public static TkCommand ScaleWorldStr = ApplicationShortcut.FromString(null, "Editor.ScaleWorldStr");
		public static TkCommand FlipHorizontal = ApplicationShortcut.FromString("H", "Editor.FlipHorizontalWorld");
		public static TkCommand FlipVertical = ApplicationShortcut.FromString("V", "Editor.FlipVerticalWorld");
		public static TkCommand FlipHorizontal2 = ApplicationShortcut.FromString("Ctrl-Alt-H", "Editor.FlipHorizontalKeyFrameOrigin");
		public static TkCommand FlipVertical2 = ApplicationShortcut.FromString("Ctrl-Alt-V", "Editor.FlipVerticalKeyFrameOrigin");
		public static TkCommand FlipTextureHorizontal = ApplicationShortcut.FromString("Ctrl-Shift-H", "Editor.TextureFlipHorizontal");
		public static TkCommand FlipTextureVertical = ApplicationShortcut.FromString("Ctrl-Shift-V", "Editor.TextureFlipVertical");
		public static TkCommand Merge = ApplicationShortcut.FromString("Ctrl-M", "Editor.Merge");
	}
}
