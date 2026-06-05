using StrEditor.ApplicationConfiguration;
using System.Collections.Generic;

namespace StrEditor.Core.OpenGLComponents {
	public static class ResourceManager {
		public static Dictionary<string, byte[]> TemporaryResources = new Dictionary<string, byte[]>();

		public static byte[] GetData(string path) {
			if (TemporaryResources.TryGetValue(path, out byte[] data))
				return data;

			return StrEditorConfiguration.Resources.MultiGrf.GetData(path);
		}

		public static byte[] GetDataBuffered(string path) {
			return StrEditorConfiguration.Resources.MultiGrf.GetDataBuffered(path);
		}

		public static void AddImageResource(string path, byte[] resource) {
			TemporaryResources[path] = resource;
		}
	}
}
