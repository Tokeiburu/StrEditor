using System;
using System.Drawing;
using GRF.Image;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace StrEditor.Core.OpenGLComponents {
	public static class GLHelper {
		public static bool LogEnabled { get; set; }
		public delegate void GLHelperEventHandler(object sender, string message);

		public static event GLHelperEventHandler Log;

		public static void OnLog(Func<string> message) {
			if (!LogEnabled)
				return;

			Log?.Invoke(null, message());
		}

		public static BlendingFactor GetOpenGlBlendFromDirectXSrc(int dxBlend) {
			switch (dxBlend) {
				case 0:	// ??
					return BlendingFactor.Zero;
				case 1:	// D3DBLEND_ZERO
					return BlendingFactor.Zero;
				case 2:	// D3DBLEND_ONE
					return BlendingFactor.One;
				case 3:	// D3DBLEND_SRCCOLOR
					return BlendingFactor.SrcColor;
				case 4:	// D3DBLEND_INVSRCCOLOR
					return BlendingFactor.OneMinusSrcColor;
				case 5:	// D3DBLEND_SRCALPHA
					return BlendingFactor.SrcAlpha;
				case 6:	// D3DBLEND_INVSRCALPHA
					return BlendingFactor.OneMinusSrcAlpha;
				case 7:	// D3DBLEND_DESTALPHA
					return BlendingFactor.DstAlpha;
				case 8:	// D3DBLEND_INVDESTALPHA
					return BlendingFactor.OneMinusDstAlpha;
				case 9:	// D3DBLEND_DESTCOLOR
					return BlendingFactor.DstColor;
				case 10: // D3DBLEND_INVDESTCOLOR
					return BlendingFactor.OneMinusDstColor;
				case 11: // D3DBLEND_SRCALPHASAT
					return BlendingFactor.SrcAlphaSaturate;
				case 12: // D3DBLEND_BOTHSRCALPHA
					return BlendingFactor.Src1Alpha;
				case 13: // D3DBLEND_BOTHINVSRCALPHA
					return BlendingFactor.OneMinusSrcAlpha;
			}

			return BlendingFactor.SrcAlpha;
		}

		public static BlendingFactor GetOpenGlBlendFromDirectXDest(int dxBlend) {
			switch (dxBlend) {
				case 0:	// ??
					return BlendingFactor.Zero;
				case 1:	// D3DBLEND_ZERO
					return BlendingFactor.Zero;
				case 2:	// D3DBLEND_ONE
					return BlendingFactor.One;
				case 3:	// D3DBLEND_SRCCOLOR
					return BlendingFactor.SrcColor;
				case 4:	// D3DBLEND_INVSRCCOLOR
					return BlendingFactor.OneMinusSrcColor;
				case 5:	// D3DBLEND_SRCALPHA
					return BlendingFactor.SrcAlpha;
				case 6:	// D3DBLEND_INVSRCALPHA
					return BlendingFactor.OneMinusSrcAlpha;
				case 7:	// D3DBLEND_DESTALPHA
					return BlendingFactor.One;
					//return BlendingFactor.DstAlpha;
				case 8:	// D3DBLEND_INVDESTALPHA
					return BlendingFactor.OneMinusDstAlpha;
				case 9:	// D3DBLEND_DESTCOLOR
					return BlendingFactor.DstColor;
				case 10: // D3DBLEND_INVDESTCOLOR
					return BlendingFactor.OneMinusDstColor;
				case 11: // D3DBLEND_SRCALPHASAT
					return BlendingFactor.SrcAlphaSaturate;
				case 12: // D3DBLEND_BOTHSRCALPHA
					return BlendingFactor.Src1Alpha;
				case 13: // D3DBLEND_BOTHINVSRCALPHA
					return BlendingFactor.OneMinusSrcAlpha;
			}

			return BlendingFactor.SrcAlpha;
		}

		public static Vector4 ToVector4(this GrfColor color) {
			return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
		}

		public static Bitmap TakeScreenshot(GLControl glControl) {
			int width = glControl.Width;
			int height = glControl.Height;

			Bitmap dump = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			System.Drawing.Imaging.BitmapData bData =
				dump.LockBits(new Rectangle(0, 0, dump.Width, dump.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, dump.PixelFormat);

			GL.ReadPixels(0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, bData.Scan0);

			dump.UnlockBits(bData);
			dump.RotateFlip(RotateFlipType.RotateNoneFlipY);

			return dump;
		}

		public static int NextPowerOfTwo(int value) {
			if (value <= 0) return 1;

			value--;

			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;

			return value + 1;
		}
	}
}
