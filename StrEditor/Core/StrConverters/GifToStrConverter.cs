using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using GRF.Image;
using GrfToWpfBridge;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.OpenGLComponents;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace StrEditor.Core.StrConverters {
	public static class GifToStrConverter {
		public static Str GifToStr(string file) {
			var str = new Str();

			for (int i = 0; i < 3; i++) {
				StrLayer layerNew = new StrLayer(str);
				str.Layers.Add(layerNew);
			}

			var layer = str.Layers[1];

			Image image = Image.FromFile(file);
			FrameDimension dimension = new FrameDimension(image.FrameDimensionsList[0]);

			PropertyItem item = image.GetPropertyItem(0x5100); // FrameDelay in libgdiplus
															   // Time is in milliseconds
			int interval = (item.Value[0] + item.Value[1] * 256) * 10;

			int frameCount = image.GetFrameCount(dimension);
			long duration = interval * frameCount;
			float gifFps = 1000f / interval;
			int framesPerInterval = 1;

			// Convert to 60 FPS
			// Find a close-ish FPS near 60
			float fpsMult = (float)Math.Ceiling(60f / gifFps);

			if (fpsMult > 1) {
				gifFps = gifFps * fpsMult;
				framesPerInterval = (int)(fpsMult * framesPerInterval);
			}

			str.Fps = (int)Math.Round(gifFps);
			str.MaxKeyFrame = framesPerInterval * frameCount;

			List<(Bitmap Bitmap, string Name)> images = new List<(Bitmap Bitmap, string Name)>();

			for (int i = 0; i < frameCount; i++) {
				image.SelectActiveFrame(dimension, i);

				var bitmap = new Bitmap(image);

				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

				int bpp = data.Stride / bitmap.Width;
				byte[] bdata = new byte[Math.Abs(data.Stride * data.Height)];
				Marshal.Copy(data.Scan0, bdata, 0, bdata.Length);
				bitmap.UnlockBits(data);

				GrfImage grfImage = new GrfImage(bdata, data.Width, data.Height, bpp == 4 ? GrfImageType.Bgra32 : GrfImageType.Bgr24);
				string imageName = Path.GetFileNameWithoutExtension(file);
				string saveImageName = imageName + String.Format("_{0:D3}", i) + (grfImage.GrfImageType == GrfImageType.Bgra32 ? ".png" : ".bmp");

				using (MemoryStream stream = new MemoryStream()) {
					grfImage.Save(stream);
					ResourceManager.AddImageResource(saveImageName, stream.ToArray());
				}

				images.Add((bitmap, saveImageName));
			}

			var vertices = new float[] {
				-(image.Width / 2),
				(image.Width / 2) + (image.Width % 2),
				(image.Width / 2) + (image.Width % 2),
				-(image.Width / 2),
				-(image.Height / 2),
				-(image.Height / 2),
				(image.Height / 2) + (image.Height % 2),
				(image.Height / 2) + (image.Height % 2),
			};

			if (StrEditorConfiguration.UseCascadeForGifs) {
				for (int i = 0; i < frameCount; i++) {
					var name = images[i].Name;

					str.Layers.Add(new StrLayer(str));
					layer = str.Layers[i + 1];
					layer.TextureNames.Add(name);

					StrKeyFrame keyFrame0 = StrKeyFrame.CreateDefaultFrame(i * framesPerInterval);
					StrKeyFrame keyFrame1 = StrKeyFrame.CreateDefaultFrame(i * framesPerInterval + framesPerInterval);

					for (int j = 0; j < 8; j++) {
						keyFrame0.Xy[j] = vertices[j];
						keyFrame1.Xy[j] = vertices[j];
					}

					// Mark as the end frame
					keyFrame1.Color[3] = 0;

					layer.KeyFrames.Add(keyFrame0);
					layer.KeyFrames.Add(keyFrame1);
				}
			}
			else {
				var textureNames = images.Select(p => p.Name).ToList();
				layer.TextureNames.AddRange(textureNames);

				StrKeyFrame keyFrame0 = new StrKeyFrame();

				keyFrame0.AnimationType = 2;

				for (int j = 0; j < 8; j++)
					keyFrame0.Xy[j] = vertices[j];

				keyFrame0.Color[0] = 255;
				keyFrame0.Color[1] = 255;
				keyFrame0.Color[2] = 255;
				keyFrame0.Color[3] = 255;

				keyFrame0.Offset = new TkVector2(Str.OffsetX, Str.OffsetY);
				keyFrame0.Delay = 1f / framesPerInterval;

				if (float.IsNaN(keyFrame0.Delay) || float.IsInfinity(keyFrame0.Delay)) {
					keyFrame0.Delay = 0;
				}

				keyFrame0.SourceAlpha = 5;
				keyFrame0.DestinationAlpha = 7;
				keyFrame0.IsInterpolated = true;

				StrKeyFrame keyFrame1 = new StrKeyFrame(keyFrame0);
				keyFrame1.FrameIndex = str.KeyFrameCount - 1;
				keyFrame1.AnimationType = 0;
				keyFrame1.TextureIndex = frameCount - 1;
				keyFrame1.Color[3] = 255;
				keyFrame1.IsInterpolated = false;

				layer.KeyFrames.Add(keyFrame0);
				layer.KeyFrames.Add(keyFrame1);
			}

			return str;
		}
	}
}
