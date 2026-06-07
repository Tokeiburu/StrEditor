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
												keyFrame.AnimationType = (AnimationType)int.Parse(data[1]);
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
												keyFrame.UVs[0] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.UVs[1] = FormatConverters.SingleConverter(subSplit[1]);
												break;
											case "uvs":
												subSplit = data[1].Split(',');
												keyFrame.UVs[2] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.UVs[3] = FormatConverters.SingleConverter(subSplit[1]);
												break;
											case "uv2":
												subSplit = data[1].Split(',');
												keyFrame.UVs[4] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.UVs[5] = FormatConverters.SingleConverter(subSplit[1]);
												break;
											case "uvs2":
												subSplit = data[1].Split(',');
												keyFrame.UVs[6] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.UVs[7] = FormatConverters.SingleConverter(subSplit[1]);
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
												keyFrame.BlendSrc = int.Parse(subSplit[4]);
												keyFrame.BlendDst = int.Parse(subSplit[5]);
												break;
											case "points":
												subSplit = data[1].ReplaceAll("  ", " ").ReplaceAll(" ", ",").Split(',');
												keyFrame.Positions[0] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.Positions[4] = FormatConverters.SingleConverter(subSplit[1]);
												keyFrame.Positions[1] = FormatConverters.SingleConverter(subSplit[2]);
												keyFrame.Positions[5] = FormatConverters.SingleConverter(subSplit[3]);
												keyFrame.Positions[2] = FormatConverters.SingleConverter(subSplit[4]);
												keyFrame.Positions[6] = FormatConverters.SingleConverter(subSplit[5]);
												keyFrame.Positions[3] = FormatConverters.SingleConverter(subSplit[6]);
												keyFrame.Positions[7] = FormatConverters.SingleConverter(subSplit[7]);
												break;
											case "bezier":
												subSplit = data[1].ReplaceAll("  ", " ").ReplaceAll(" ", ",").Split(',');
												keyFrame.BezierPositions[0] = FormatConverters.SingleConverter(subSplit[0]);
												keyFrame.BezierPositions[1] = FormatConverters.SingleConverter(subSplit[1]);
												keyFrame.BezierPositions[2] = FormatConverters.SingleConverter(subSplit[2]);
												keyFrame.BezierPositions[3] = FormatConverters.SingleConverter(subSplit[3]);
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
					b.AppendLine("        uv=" + string.Format("{0},{1}", keyFrame.UVs[0].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.UVs[1].ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uvs=" + string.Format("{0},{1}", keyFrame.UVs[2].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.UVs[3].ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uv2=" + string.Format("{0},{1}", keyFrame.UVs[4].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.UVs[5].ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        uvs2=" + string.Format("{0},{1}", keyFrame.UVs[6].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.UVs[7].ToString("0.0000", CultureInfo.InvariantCulture)));

					float maxX = keyFrame.Positions[0];
					float minX = keyFrame.Positions[0];

					for (int i = 1; i < 4; i++) {
						if (keyFrame.Positions[i] > maxX)
							maxX = keyFrame.Positions[i];
						if (keyFrame.Positions[i] < minX)
							minX = keyFrame.Positions[i];
					}

					float maxY = keyFrame.Positions[4];
					float minY = keyFrame.Positions[4];

					for (int i = 5; i < 8; i++) {
						if (keyFrame.Positions[i] > maxY)
							maxY = keyFrame.Positions[i];
						if (keyFrame.Positions[i] < minY)
							minY = keyFrame.Positions[i];
					}

					b.AppendLine("        scale=" + string.Format("{0},{1}", (maxX - minX).ToString("0.0000", CultureInfo.InvariantCulture), (maxY - minY).ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        angle=0.0000,0.0000," + string.Format("{0}", (keyFrame.Angle * (1024f / 360f)).ToString("0.0000", CultureInfo.InvariantCulture)));
					b.AppendLine("        color=" + string.Format("{0},{1},{2},{3}, {4},{5}",
						keyFrame.Color[0].ToString("0.0", CultureInfo.InvariantCulture), keyFrame.Color[1].ToString("0.0", CultureInfo.InvariantCulture), keyFrame.Color[2].ToString("0.0", CultureInfo.InvariantCulture), keyFrame.Color[3].ToString("0.0", CultureInfo.InvariantCulture),
						keyFrame.BlendSrc, keyFrame.BlendDst));

					b.AppendLine("        mtpreset=0");

					b.AppendLine("        points=" + string.Format("{0},{1}  {2},{3}  {4},{5}  {6},{7}",
						keyFrame.Positions[0].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Positions[4].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Positions[1].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Positions[5].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Positions[2].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Positions[6].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Positions[3].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Positions[7].ToString("0.0000", CultureInfo.InvariantCulture)));

					b.AppendLine("        rpoints=" + string.Format("{0},{1}  {2},{3}  {4},{5}  {6},{7}",
						keyFrame.Positions[0].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Positions[4].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Positions[1].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Positions[5].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Positions[2].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Positions[6].ToString("0.0000", CultureInfo.InvariantCulture),
						keyFrame.Positions[3].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.Positions[7].ToString("0.0000", CultureInfo.InvariantCulture)));

					b.AppendLine("        bezier=" + string.Format("{0},{1}  {2},{3}", keyFrame.BezierPositions[0].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.BezierPositions[1].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.BezierPositions[2].ToString("0.0000", CultureInfo.InvariantCulture), keyFrame.BezierPositions[3].ToString("0.0000", CultureInfo.InvariantCulture)));
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
