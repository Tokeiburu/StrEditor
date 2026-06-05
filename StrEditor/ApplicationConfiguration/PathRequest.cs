using TokeiLibrary.Paths;
using Utilities;

namespace StrEditor.ApplicationConfiguration {
	/// <summary>
	/// Class imported from GrfEditor
	/// </summary>
	public static class PathRequest {
		public static Setting ExtractSetting => new Setting(null, typeof(StrEditorConfiguration).GetProperty("ExtractingServiceLastPath"));
		public static Setting SaveAdvancedSetting => new Setting(null, typeof(StrEditorConfiguration).GetProperty("SaveAdvancedLastPath"));
		public static string SaveFileEditor(params string[] extra) => TkPathRequest.SaveFile(new Setting(null, typeof(StrEditorConfiguration).GetProperty("AppLastPath")), extra);
		public static string SaveFileExtract(params string[] extra) => TkPathRequest.SaveFile(ExtractSetting, extra);
		public static string OpenFileEditor(params string[] extra) => TkPathRequest.OpenFile(new Setting(null, typeof(StrEditorConfiguration).GetProperty("AppLastPath")), extra);
		public static string[] OpenFileStr(params string[] extra) => TkPathRequest.OpenFiles(new Setting(null, typeof(StrEditorConfiguration).GetProperty("AppLastStrFolder")), extra);
		public static string OpenGrfFile(params string[] extra) => TkPathRequest.OpenFile(new Setting(null, typeof(StrEditorConfiguration).GetProperty("AppLastGrfPath")), extra);
		public static string OpenFileExtract(params string[] extra) => TkPathRequest.OpenFile(ExtractSetting, extra);
		public static string[] OpenFilesExtract(params string[] extra) => TkPathRequest.OpenFiles(ExtractSetting, extra);
		public static string FolderEditor(params string[] extra) => TkPathRequest.Folder(new Setting(null, typeof(StrEditorConfiguration).GetProperty("AppLastPath")), extra);
		public static string FolderExtract(params string[] extra) => TkPathRequest.Folder(ExtractSetting);
		public static string FolderSaveAdvanced(params string[] extra) => TkPathRequest.Folder(SaveAdvancedSetting);
	}
}