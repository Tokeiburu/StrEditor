using GRF.Core;
using GRF.FileFormats.StrFormat;
using GRF.GrfSystem;
using GRF.IO;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.OpenGLComponents;
using StrEditor.Core.StrConverters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokeiLibrary.Paths;
using Utilities;
using Utilities.Extension;

namespace StrEditor.Services {
	public class StrSaveService {
		public class SaveFormat {
			public string Name;
			public string Filter;
			public SaveMode Mode;
			public string RequiredExtension;
			public string AnyExtension;

			public string[] GetRequiredExtensions() {
				return RequiredExtension.Replace("*", "").Split(';');
			}

			public string[] GetAnyExtensions() {
				return AnyExtension.Replace("*", "").Split(';');
			}
		}

		public class SaveContext {
			public Str Str;
			public string FilePath;
			public SaveMode Mode;
		}

		public class SaveResult {
			public string NewFilePath;
			public bool AddToRecentFiles;
			public bool IsNewCleared;
			public bool SaveCommandIndex;
		}

		public enum SaveMode {
			Str,
			Ezv,
		}

		//public string ResolveInitialPath(Str str) {
		//	var fileName = StrEditorConfiguration.AppLastPath;
		//
		//	if (Path.GetFileNameWithoutExtension(fileName) != Path.GetFileNameWithoutExtension(tab.Act.LoadedPath)) {
		//		fileName = tab.Act.LoadedPath;
		//	}
		//
		//	return fileName;
		//}

		private List<SaveFormat> _saveFormats = new List<SaveFormat> {
			new SaveFormat { Name = "Animation files", Filter = "*.str", Mode = SaveMode.Str, RequiredExtension = "*.str" },
			new SaveFormat { Name = "EzVisual files", Filter = "*.ezv", Mode = SaveMode.Ezv, RequiredExtension = "*.ezv" },
		};

		public SaveContext CreateSaveContext(Str str) {
			if (str == null)
				return null;

			string fileName = str.LoadedPath;

			string file = PathRequest.SaveFileEditor("fileName", fileName, "filter", "Animation files|*.str|EzVisual files|*.ezv");

			if (file == null) return null;

			var dialog = TkPathRequest.LatestSaveFileDialog;

			return new SaveContext {
				Str = str,
				FilePath = file,
				Mode = ResolveSaveMode(file, dialog.FilterIndex - 1)
			};
		}

		public SaveMode ResolveSaveMode(string file, int filterIndex) {
			if (filterIndex < 0 || filterIndex >= _saveFormats.Count) {
				throw new Exception("Unable to find a matching save mode.");
			}

			var format = _saveFormats[filterIndex];

			if (file.IsExtension(format.GetRequiredExtensions())) {
				return format.Mode;
			}

			// If not a direct match, fallback to extension rather than the selected filter index
			for (int i = 0; i < _saveFormats.Count; i++) {
				format = _saveFormats[i];

				if (string.IsNullOrEmpty(format.AnyExtension))
					continue;
				if (file.IsExtension(format.GetAnyExtensions()))
					return format.Mode;
			}

			throw new Exception("File extension does not match the save mode.");
		}

		public SaveResult ExecuteSave(SaveContext sc) {
			switch (sc.Mode) {
				case SaveMode.Str:
					return _saveStr(sc);
				case SaveMode.Ezv:
					return _saveEzv(sc);
				default:
					throw new InvalidOperationException("Unknown save mode.");
			}
		}

		private SaveResult _saveStr(SaveContext sc) {
			var str = sc.Str;
			var path = sc.FilePath;

			var tempStr = _fixInterpolate(str);
			tempStr.Save(path);

			if (StrEditorConfiguration.AlwaysSaveTexturesWithStr)
				_saveTextures(path, str);

			str.LoadedPath = path;
			str.Commands.SaveCommandIndex();

			return new SaveResult {
				AddToRecentFiles = true,
				IsNewCleared = true,
				NewFilePath = path,
				SaveCommandIndex = true,
			};
		}

		private SaveResult _saveEzv(SaveContext sc) {
			var str = sc.Str;
			var path = sc.FilePath;

			var tempStr = _fixInterpolate(str, true);
			EzvToStrConverter.SaveAsEzv(tempStr, path);

			if (StrEditorConfiguration.AlwaysSaveTexturesWithStr)
				_saveTextures(path, str);

			str.LoadedPath = path;
			str.Commands.SaveCommandIndex();

			return new SaveResult {
				AddToRecentFiles = true,
				IsNewCleared = true,
				NewFilePath = path,
				SaveCommandIndex = true,
			};
		}

		private void _saveTextures(string file, Str str) {
			string dir = GrfPath.GetDirectoryName(file);

			foreach (var texture in str.Textures) {
				string imagePath = dir + "\\" + texture;
				var data = ResourceManager.GetData(texture) ?? ResourceManager.GetData(imagePath);

				if (data != null) {
					Debug.Ignore(() => File.WriteAllBytes(imagePath, data));
				}
			}
		}

		private Str _fixInterpolate(Str str, bool ezv = false) {
			Str newStr = new Str(str);

			if (newStr.Layers.Count > 0 && newStr.Layers[0].KeyFrames.Count != 0) {
				newStr.Layers.Insert(0, new StrLayer(str));
			}

			for (int layerIndex = 0; layerIndex < str.Layers.Count; layerIndex++) {
				var layer = newStr[layerIndex];

				for (int keyIndex = layer.KeyFrames.Count - 1; keyIndex >= 0; keyIndex--) {
					if (layer[keyIndex].IsInterpolated && layer[keyIndex + 1] != null && layer[keyIndex + 1].FrameIndex == layer[keyIndex].FrameIndex + 1) {
						layer[keyIndex].IsInterpolated = false;
					}
					else if (layer[keyIndex].IsInterpolated && layer[keyIndex + 1] == null && layer[keyIndex].FrameIndex == str.MaxKeyFrame) {
						layer[keyIndex].IsInterpolated = false;
					}
					else if (layer[keyIndex].IsInterpolated) {
						StrKeyFrame keyFrame = layer[keyIndex + 1];

						if (keyFrame == null) {
							keyFrame = new StrKeyFrame(layer[keyIndex]);
							keyFrame.FrameIndex = str.KeyFrameCount;
						}

						if (!ezv && (
							// ReSharper disable CompareOfFloatsByEqualityOperator
							layer[keyIndex].AngleBias != 0 || layer[keyIndex].OffsetBias != 0 || layer[keyIndex].ScaleBias != 0 ||
							layer[keyIndex].Bezier[2] != 0 || layer[keyIndex].Bezier[3] != 0 || keyFrame.Bezier[0] != 0 || keyFrame.Bezier[1] != 0)) {
							// ReSharper restore CompareOfFloatsByEqualityOperator
							// Create all frames!
							layer[keyIndex].IsInterpolated = false;

							for (int frameIndex = layer[keyIndex + 1].FrameIndex - 1; frameIndex > layer[keyIndex].FrameIndex; frameIndex--) {
								var interpolateF = InterpolatedKeyFrame.InterpolateSub(str, layerIndex, frameIndex, layer[keyIndex], keyFrame, false);
								var interpolateFKeyFrame = interpolateF.ToKeyFrame(2);
								interpolateFKeyFrame.FrameIndex = frameIndex;
								interpolateFKeyFrame.Type = 0;
								layer.KeyFrames.Insert(keyIndex + 1, interpolateFKeyFrame);
							}

							continue;
						}

						// Interpolate
						var interpolate = InterpolatedKeyFrame.InterpolateSub(str, layerIndex, layer[keyIndex].FrameIndex + 1, layer[keyIndex], keyFrame, true);
						var interpolateKeyFrame = interpolate.ToKeyFrame(1);
						interpolateKeyFrame.FrameIndex = layer[keyIndex].FrameIndex;
						interpolateKeyFrame.Type = 1;
						layer.KeyFrames.Insert(keyIndex + 1, interpolateKeyFrame);
					}
				}
			}

			return newStr;
		}

		public SaveResult Save(Str str) {
			SaveResult result;

			if (_isInGrf(str)) {
				result = _saveToGrf(str);
			}
			else {
				result = _saveToFileSystem(str);
			}

			return result;
		}

		private SaveResult _saveToFileSystem(Str str) {
			if (str.LoadedPath.IsExtension(".ezv")) {
				EzvToStrConverter.SaveAsEzv(_fixInterpolate(str, true), str.LoadedPath);
			}
			else {
				_fixInterpolate(str).Save();
			}

			if (StrEditorConfiguration.AlwaysSaveTexturesWithStr)
				_saveTextures(str.LoadedPath, str);

			return new SaveResult {
				IsNewCleared = true,
				NewFilePath = str.LoadedPath,
				SaveCommandIndex = true,
			};
		}

		private SaveResult _saveToGrf(Str str) {
			TkPath path = new TkPath(str.LoadedPath);

			if (Methods.IsFileLocked(path.FilePath)) {
				throw new Exception("The file " + path.FilePath + " is locked by another process. Try closing other GRF applicactions or use the 'Save as...' option.");
			}

			using (GrfHolder grf = new GrfHolder(path.FilePath)) {
				string temp = TemporaryFilesManager.GetTemporaryFilePath("to_grf_{0:0000}.str");

				str.Save(temp);

				grf.Commands.AddFile(path.RelativePath.ReplaceExtension(".str"), File.ReadAllBytes(temp));
				grf.Save();
				grf.ProcessSaveResult();
			}

			return new SaveResult {
				IsNewCleared = true,
				NewFilePath = str.LoadedPath,
				SaveCommandIndex = true,
			};
		}

		private bool _isInGrf(Str str) {
			TkPath path = new TkPath(str.LoadedPath);

			return !string.IsNullOrEmpty(path.RelativePath);
		}
	}
}
