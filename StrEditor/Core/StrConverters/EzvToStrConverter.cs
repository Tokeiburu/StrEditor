using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ErrorManager;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using GRF.IO;
using Utilities;
using Utilities.Extension;

namespace StrEditor.Core.StrConverters {
	public static class EzvToStrConverter {
		public static Str EzvToStr(string file) {
			Str str = new Str();

			try {
				var lines = LineTextReader.ReadAllLines(file, Encoding.Default).ToList();
				int layerIdx = -1;

				for (int index = 0; index < lines.Count; index++) {
					var line = lines[index];

					if (line.Contains("=")) {
						var data = line.Split('=');

						switch (data[0]) {
							case "fps":
								str.Fps = int.Parse(data[1]);
								break;
							case "maxkey":
								str.MaxKeyFrame = int.Parse(data[1]);
								break;
							case "":
								break;
						}
					}
					else if (line.Contains("layer:")) {
						layerIdx++;

						if (line.Contains("layer:Back"))
							continue;

						bool destroyLayer = false;
						var layer = new StrLayer(str);
						str.Layers.Add(layer);

						index++;

						for (; index < lines.Count; index++) {
							if (destroyLayer)
								break;

							line = lines[index];

							if (line.Contains("=")) {
								var data = line.Split('=');

								switch (data[0].Trim(' ')) {
									case "display":
										int display = int.Parse(data[1]);

										if (display == 0)
											destroyLayer = true;
										break;
									case "texname":
										layer.TextureNames.Add(data[1].Contains(".") ? data[1] : data[1] + ".bmp");
										break;
								}
							}
							else if (line.Contains("}")) {
								break;
							}
							else if (line.Contains("    {")) {
								index++;

								var keyFrame = new StrKeyFrame();
								keyFrame.IsInterpolated = true;

								for (; index < lines.Count; index++) {
									line = lines[index];

									if (line.Contains("=")) {
										var data = line.Split('=');
										string[] subSplit;

										switch (data[0].Trim(' ')) {
											case "frame":
												keyFrame.FrameIndex = int.Parse(data[1]);
												break;
											case "anitype":
												keyFrame.AnimationType = int.Parse(data[1]);
												break;
											case "delay":
												keyFrame.Delay = FormatConverters.SingleConverter(data[1]);
												break;
											case "pos":
												subSplit = data[1].Split(',');
												keyFrame.Offset = new TkVector2(FormatConverters.SingleConverter(subSplit[0]), FormatConverters.SingleConverter(subSplit[1]));
												break;
											case "uv":
												subSplit = data[1].Split(',');
												keyFrame.Uv[0] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.Uv[1] = FormatConverters.SingleConverter(subSplit[1]);
												break;
											case "uvs":
												subSplit = data[1].Split(',');
												keyFrame.Uv[2] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.Uv[3] = FormatConverters.SingleConverter(subSplit[1]);
												break;
											case "uv2":
												subSplit = data[1].Split(',');
												keyFrame.Uv[4] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.Uv[5] = FormatConverters.SingleConverter(subSplit[1]);
												break;
											case "uvs2":
												subSplit = data[1].Split(',');
												keyFrame.Uv[6] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.Uv[7] = FormatConverters.SingleConverter(subSplit[1]);
												break;
											case "angle":
												subSplit = data[1].Split(',');
												keyFrame.Angle = FormatConverters.SingleConverter(subSplit[2]) / (1024f / 360f);
												break;
											case "color":
												subSplit = data[1].Split(',');
												keyFrame.Color[0] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.Color[1] = FormatConverters.SingleConverter(subSplit[1]);
												keyFrame.Color[2] = FormatConverters.SingleConverter(subSplit[2]);
												keyFrame.Color[3] = FormatConverters.SingleConverter(subSplit[3]);
												keyFrame.SourceAlpha = int.Parse(subSplit[4]);
												keyFrame.DestinationAlpha = int.Parse(subSplit[5]);
												break;
											case "points":
												subSplit = data[1].ReplaceAll("  ", " ").ReplaceAll(" ", ",").Split(',');
												keyFrame.Xy[0] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.Xy[4] = FormatConverters.SingleConverter(subSplit[1]);
												keyFrame.Xy[1] = FormatConverters.SingleConverter(subSplit[2]);
												keyFrame.Xy[5] = FormatConverters.SingleConverter(subSplit[3]);
												keyFrame.Xy[2] = FormatConverters.SingleConverter(subSplit[4]);
												keyFrame.Xy[6] = FormatConverters.SingleConverter(subSplit[5]);
												keyFrame.Xy[3] = FormatConverters.SingleConverter(subSplit[6]);
												keyFrame.Xy[7] = FormatConverters.SingleConverter(subSplit[7]);
												break;
											case "bezier":
												subSplit = data[1].ReplaceAll("  ", " ").ReplaceAll(" ", ",").Split(',');
												keyFrame.Bezier[0] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.Bezier[1] = FormatConverters.SingleConverter(subSplit[1]);
												keyFrame.Bezier[2] = FormatConverters.SingleConverter(subSplit[2]);
												keyFrame.Bezier[3] = FormatConverters.SingleConverter(subSplit[3]);
												break;
											case "posbias":
												keyFrame.OffsetBias = FormatConverters.SingleConverter(data[1]);
												break;
											case "ptbias":
												keyFrame.ScaleBias = FormatConverters.SingleConverter(data[1]);
												break;
											case "angbias":
												keyFrame.AngleBias = FormatConverters.SingleConverter(data[1]);
												break;
										}
									}
									else if (line.Contains("    }"))
										break;
								}

								layer.KeyFrames.Add(keyFrame);
							}
						}

						if (destroyLayer) {
							destroyLayer = false;
							str.Layers.RemoveAt(str.Layers.Count - 1);
							continue;
						}

						layer.KeyFrames = layer.KeyFrames.OrderBy(p => p.FrameIndex).ToList();

						if (layer.KeyFrames.Count > 0)
							layer.KeyFrames.Last().IsInterpolated = false;
					}

				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			return str;
		}

		public static void SaveAsEzv(Str str, string path) {
			StringBuilder b = new StringBuilder();

			b.AppendLine("EVFF0.95");
			b.AppendLine();
			b.AppendLine("fps=" + str.Fps);
			b.AppendLine();
			b.AppendLine("maxkey=" + str.MaxKeyFrame);
			b.AppendLine();
			b.AppendLine("layernum=" + str.Layers.Count);
			b.AppendLine();

			var names = new HashSet<string>();

			for (int index = 0; index < str.Layers.Count; index++) {
				var layer = str.Layers[index];

				string name = index == 0 ? "Back" : str.Layers[index].TextureNames.FirstOrDefault();

				if (name == null) {
					name = "unknown";
				}

				if (name.Contains("."))
					name = name.ReplaceExtension("");

				while (names.Contains(name)) {
					name = name + "-";
				}

				names.Add(name);

				b.AppendLine("layer:" + name);
				b.AppendLine("{");
				b.AppendLine("    display=1");
				b.AppendLine("    group=0");
				b.AppendLine("    type=" + (index == 0 ? "1" : "0"));
				b.AppendLine("    texcnt=" + layer.TextureNames.Count);

				foreach (var text in layer.TextureNames) {
					b.AppendLine("    texname=" + text);
				}

				var keyFrames = layer.KeyFrames.Where(p => p.Type == 0).ToList();

				b.AppendLine();
				b.AppendLine("    anikeynum=" + keyFrames.Count);
				b.AppendLine();

				foreach (var keyFrame in keyFrames) {
					b.AppendLine("    {");
					b.AppendLine("        frame=" + keyFrame.FrameIndex);
					b.AppendLine("        aniframe=0");
					b.AppendLine("        anitype=" + keyFrame.AnimationType);
					b.AppendLine("        delay=" + string.Format("{0}", (float.IsNaN(keyFrame.Delay) || float.IsInfinity(keyFrame.Delay) ? 0 : keyFrame.Delay).ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        pos=" + string.Format("{0},{1}", keyFrame.Offset.X.ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Offset.Y.ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uv=" + string.Format("{0},{1}", keyFrame.Uv[0].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Uv[1].ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uvs=" + string.Format("{0},{1}", keyFrame.Uv[2].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Uv[3].ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uv2=" + string.Format("{0},{1}", keyFrame.Uv[4].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Uv[5].ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uvs2=" + string.Format("{0},{1}", keyFrame.Uv[6].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Uv[7].ToString("0.0000", CultureInfo.InvariantCulture)));

					float maxX = keyFrame.Xy[0];
					float minX = keyFrame.Xy[0];

					for (int i = 1; i < 4; i++) {
						if (keyFrame.Xy[i] > maxX)
							maxX = keyFrame.Xy[i];
						if (keyFrame.Xy[i] < minX)
							minX = keyFrame.Xy[i];
					}

					float maxY = keyFrame.Xy[4];
					float minY = keyFrame.Xy[4];

					for (int i = 5; i < 8; i++) {
						if (keyFrame.Xy[i] > maxY)
							maxY = keyFrame.Xy[i];
						if (keyFrame.Xy[i] < minY)
							minY = keyFrame.Xy[i];
					}

					b.AppendLine("        scale=" + string.Format("{0},{1}", (maxX - minX).ToString("0.0000", CultureInfo.InvariantCulture), (maxY - minY).ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        angle=0.0000,0.0000," + string.Format("{0}", (keyFrame.Angle * (1024f / 360f)).ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        color=" + string.Format("{0},{1},{2},{3}, {4},{5}",
						keyFrame.Color[0].ToString("0.0", CultureInfo.InvariantCulture), keyFrame.Color[1].ToString("0.0", CultureInfo.InvariantCulture), keyFrame.Color[2].ToString("0.0", CultureInfo.InvariantCulture), keyFrame.Color[3].ToString("0.0", CultureInfo.InvariantCulture),
						keyFrame.SourceAlpha, keyFrame.DestinationAlpha));

					b.AppendLine("        mtpreset=0");

					b.AppendLine("        points=" + string.Format("{0},{1}  {2},{3}  {4},{5}  {6},{7}",
						keyFrame.Xy[0].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Xy[4].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Xy[1].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Xy[5].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Xy[2].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Xy[6].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Xy[3].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Xy[7].ToString("0.0000", CultureInfo.InvariantCulture)));

					b.AppendLine("        rpoints=" + string.Format("{0},{1}  {2},{3}  {4},{5}  {6},{7}",
						keyFrame.Xy[0].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Xy[4].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Xy[1].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Xy[5].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Xy[2].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Xy[6].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Xy[3].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Xy[7].ToString("0.0000", CultureInfo.InvariantCulture)));

					b.AppendLine("        bezier=" + string.Format("{0},{1}  {2},{3}", keyFrame.Bezier[0].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Bezier[1].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Bezier[2].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Bezier[3].ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        afbias=" + string.Format("{0}", 0.ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        posbias=" + string.Format("{0}", keyFrame.OffsetBias.ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        ptbias=" + string.Format("{0}", keyFrame.ScaleBias.ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        angbias=" + string.Format("{0}", keyFrame.AngleBias.ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uvbias=" + string.Format("{0}", 0.ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uvsbias=" + string.Format("{0}", 0.ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uvbias2=" + string.Format("{0}", 0.ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uvsbias2=" + string.Format("{0}", 0.ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("    }");
				}

				b.AppendLine("}");
				b.AppendLine();
			}

			File.WriteAllText(path, b.ToString());
		}
	}
}
