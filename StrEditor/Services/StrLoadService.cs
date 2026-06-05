using GRF.Core;
using GRF.FileFormats.StrFormat;
using GRF.GrfSystem;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.StrConverters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Utilities.Extension;

namespace StrEditor.Services {
	public class StrLoadService {
		public class LoadResult {
			public bool AddToRecentFiles;
			public bool RemoveToRecentFiles;
			public string FilePath;
			public bool Success;
			public string ErrorMessage;
			public bool IsNew;
			public Str LoadedStr;

			public static LoadResult Fail(string message, string path) {
				LoadResult result = new LoadResult();
				result.Success = false;
				result.ErrorMessage = message;
				result.FilePath = path;
				result.RemoveToRecentFiles = true;
				return result;
			}
		}

		public LoadResult Load(TkPath file) {
			if (file.FilePath.IsExtension(".gif"))
				return _loadFromGif(file.FilePath);

			if (file.FilePath.IsExtension(".str", ".ezv") || String.IsNullOrEmpty(file.RelativePath))
				return _loadFromFileSystem(file.FilePath);
			
			return _loadFromGrf(file);
		}

		private LoadResult _loadFromFileSystem(string file) {
			if (!file.IsExtension(".str", ".ezv"))
				return LoadResult.Fail("Invalid file extension; only .str or .ezv files are allowed.", file);

			if (!File.Exists(file))
				return LoadResult.Fail("File not found while trying to open the Str.\r\n\r\n" + file, file);

			LoadResult result = new LoadResult();
			result.FilePath = file;
			result.AddToRecentFiles = true;
			
			var str = file.IsExtension(".ezv") ? EzvToStrConverter.EzvToStr(file) : new Str(file);
			str.ConvertInterpolatedFrames();
			
			if (StrEditorConfiguration.AttemptReconstrustBias)
				str.DetectInterpolatedFrames();

			result.LoadedStr = str;
			result.LoadedStr.LoadedPath = file;
			result.Success = true;
			return result;
		}

		private LoadResult _loadFromGrf(TkPath file) {
			if (!File.Exists(file.FilePath))
				return LoadResult.Fail("GRF path not found.", file);

			LoadResult result = new LoadResult();
			result.FilePath = file.GetFullPath();
			result.AddToRecentFiles = true;

			TkPath sprPath = new TkPath(file);
			sprPath.RelativePath = sprPath.RelativePath.ReplaceExtension(".spr");

			byte[] data = null;

			using (GrfHolder grf = new GrfHolder(file.FilePath)) {
				if (grf.FileTable.ContainsFile(file.RelativePath))
					data = grf.FileTable[file.RelativePath].GetDecompressedData();
			}

			if (data == null)
				return LoadResult.Fail("File not found: " + file, file);

			Str str;
			if (file.RelativePath.IsExtension(".ezv")) {
				var systemPath = TemporaryFilesManager.GetTemporaryFilePath("ezv2str{0:0000}.ezv");
				File.WriteAllBytes(systemPath, data);
				str = EzvToStrConverter.EzvToStr(systemPath);
			}
			else {
				str = new Str(data);
			}

			str.ConvertInterpolatedFrames();

			if (StrEditorConfiguration.AttemptReconstrustBias)
				str.DetectInterpolatedFrames();

			result.LoadedStr = new Str(data);
			result.LoadedStr.LoadedPath = file.GetFullPath();
			result.Success = true;
			return result;
		}

		private LoadResult _loadFromGif(string file) {
			if (!file.IsExtension(".gif"))
				return LoadResult.Fail("Invalid file extension; only .gif files are allowed.", file);

			if (!File.Exists(file))
				return LoadResult.Fail("File not found while trying to open the Gif.\r\n\r\n" + file, file);

			LoadResult result = new LoadResult();
			result.FilePath = file;
			result.AddToRecentFiles = false;

			var str = GifToStrConverter.GifToStr(file);

			result.LoadedStr = str;
			result.LoadedStr.LoadedPath = file;
			result.Success = true;
			result.IsNew = true;
			return result;
		}
	}
}
