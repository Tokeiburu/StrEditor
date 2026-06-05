using GRF.Image;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrEditor.Core.StrConverters {
	public class CustomPaletteGifMuxer : IDisposable {
		private readonly Stream _stream;
		private bool _createdHeader;

		public string FilePath { get; }
		public int Delay { get; }
		public int Repeat { get; }
		public int FrameCount { get; private set; }

		public void Dispose() {
			Finish();
		}

		public CustomPaletteGifMuxer(Stream stream, int delay = 33, int repeat = 0) {
			_stream = stream;
			Repeat = repeat;
			Delay = delay;
		}

		public void AddFrame(Image image, int delay = -1) {
			var gif = new GifClass();
			gif.LoadGifPicture(image);

			if (!_createdHeader) {
				AppendToStream(CreateHeaderBlock());
				AppendToStream(gif.ScreenDescriptor.ToArray());
				AppendToStream(CreateApplicationExtensionBlock(Repeat));
				_createdHeader = true;
			}

			AppendToStream(CreateGraphicsControlExtensionBlock(delay > -1 ? delay : Delay));
			AppendToStream(gif.ImageDescriptor.ToArray());
			AppendToStream(gif.ColorTable.ToArray());
			AppendToStream(gif.ImageData.ToArray());

			FrameCount++;
		}

		private void AppendToStream(byte[] data) {
			_stream.Write(data, 0, data.Length);
		}

		/// <summary>
		///     Finish creating the GIF and start flushing
		/// </summary>
		private void Finish() {
			if (_stream == null)
				return;
			_stream.WriteByte(0x3B); // Image terminator
			if (_stream.GetType() == typeof(FileStream))
				_stream.Dispose();
		}

		/// <summary>
		///     Create the GIFs header block (GIF89a)
		/// </summary>
		private static byte[] CreateHeaderBlock() {
			return new[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' };
		}

		private static byte[] CreateApplicationExtensionBlock(int repeat) {
			byte[] buffer = new byte[19];
			buffer[0] = 0x21; // Extension introducer
			buffer[1] = 0xFF; // Application extension
			buffer[2] = 0x0B; // Size of block
			buffer[3] = (byte)'N'; // NETSCAPE2.0
			buffer[4] = (byte)'E';
			buffer[5] = (byte)'T';
			buffer[6] = (byte)'S';
			buffer[7] = (byte)'C';
			buffer[8] = (byte)'A';
			buffer[9] = (byte)'P';
			buffer[10] = (byte)'E';
			buffer[11] = (byte)'2';
			buffer[12] = (byte)'.';
			buffer[13] = (byte)'0';
			buffer[14] = 0x03; // Size of block
			buffer[15] = 0x01; // Loop indicator
			buffer[16] = (byte)(repeat % 0x100); // Number of repetitions
			buffer[17] = (byte)(repeat / 0x100); // 0 for endless loop
			buffer[18] = 0x00; // Block terminator
			return buffer;
		}

		private static byte[] CreateGraphicsControlExtensionBlock(int delay) {
			byte[] buffer = new byte[8];
			buffer[0] = 0x21; // Extension introducer
			buffer[1] = 0xF9; // Graphic control extension
			buffer[2] = 0x04; // Size of block
			buffer[3] = 0x09; // Flags: reserved, disposal method, user input, transparent color

			int delayInCentiseconds = (int)Math.Round(delay / 10.0);
			buffer[4] = (byte)(delayInCentiseconds & 0xFF);         // Low byte
			buffer[5] = (byte)((delayInCentiseconds >> 8) & 0xFF);  // High byte

			buffer[6] = 0xFF; // Transparent color index
			buffer[7] = 0x00; // Block terminator
			return buffer;
		}
	}

	public class GifClass {
		public enum GifBlockType {
			ImageDescriptor = 0x2C,
			Extension = 0x21,
			Trailer = 0x3B
		}

		public enum GifVersion {
			GIF87a,
			GIF89a
		}

		public List<byte> ColorTable = new List<byte>();
		public List<byte> GifSignature = new List<byte>();
		public List<byte> ImageData = new List<byte>();
		public List<byte> ImageDescriptor = new List<byte>();
		public List<byte> ScreenDescriptor = new List<byte>();

		public GifVersion Version = GifVersion.GIF87a;

		public void LoadGifPicture(Image img) {
			List<byte> dataList;

			using (var ms = new MemoryStream()) {
				img.Save(ms, ImageFormat.Gif);
				dataList = new List<byte>(ms.ToArray());
			}

			if (!AnalyzeGifSignature(dataList)) throw new Exception("File is not a gif!");

			AnalyzeScreenDescriptor(dataList);

			var blockType = GetTypeOfNextBlock(dataList);

			while (blockType != GifBlockType.Trailer) {
				switch (blockType) {
					case GifBlockType.ImageDescriptor:
						AnalyzeImageDescriptor(dataList);
						break;
					case GifBlockType.Extension:
						ThrowAwayExtensionBlock(dataList);
						break;
				}

				blockType = GetTypeOfNextBlock(dataList);
			}
		}

		private bool AnalyzeGifSignature(List<byte> gifData) {
			for (int i = 0; i < 6; i++) GifSignature.Add(gifData[i]);

			gifData.RemoveRange(0, 6);

			List<char> chars = GifSignature.ConvertAll(ByteToChar);

			string s = new string(chars.ToArray());

			if (s == GifVersion.GIF89a.ToString()) Version = GifVersion.GIF89a;
			else if (s == GifVersion.GIF87a.ToString()) Version = GifVersion.GIF87a;
			else return false;

			return true;
		}

		private char ByteToChar(byte b) {
			return (char)b;
		}

		private void AnalyzeScreenDescriptor(List<byte> gifData) {
			for (int i = 0; i < 7; i++) ScreenDescriptor.Add(gifData[i]);

			gifData.RemoveRange(0, 7);

			// if the first bit of the fifth byte is set the GlobelColorTable follows this block

			bool globalColorTableFollows = (ScreenDescriptor[4] & 0x80) != 0;

			if (globalColorTableFollows) {
				int pixel = ScreenDescriptor[4] & 0x07;

				int lengthOfColorTableInByte = 3 * (int)Math.Pow(2, pixel + 1);

				for (int i = 0; i < lengthOfColorTableInByte; i++) ColorTable.Add(gifData[i]);

				gifData.RemoveRange(0, lengthOfColorTableInByte);
			}

			ScreenDescriptor[4] = (byte)(ScreenDescriptor[4] & 0x7F);
		}

		private GifBlockType GetTypeOfNextBlock(List<byte> gifData) {
			var blockType = (GifBlockType)gifData[0];

			return blockType;
		}

		private void AnalyzeImageDescriptor(List<byte> gifData) {
			for (int i = 0; i < 10; i++) ImageDescriptor.Add(gifData[i]);

			gifData.RemoveRange(0, 10);

			// get ColorTable if exists

			bool localColorMapFollows = (ImageDescriptor[9] & 0x80) != 0;

			if (localColorMapFollows) {
				int pixel = ImageDescriptor[9] & 0x07;

				int lengthOfColorTableInByte = 3 * (int)Math.Pow(2, pixel + 1);

				ColorTable.Clear();

				for (int i = 0; i < lengthOfColorTableInByte; i++) ColorTable.Add(gifData[i]);

				gifData.RemoveRange(0, lengthOfColorTableInByte);
			}
			else {
				int lastThreeBitsOfGlobalTableDescription = ScreenDescriptor[4] & 0x07;

				ImageDescriptor[9] = (byte)(ImageDescriptor[9] & 0xF8);

				ImageDescriptor[9] = (byte)(ImageDescriptor[9] | lastThreeBitsOfGlobalTableDescription);
			}

			ImageDescriptor[9] = (byte)(ImageDescriptor[9] | 0x80);

			GetImageData(gifData);
		}

		private void GetImageData(List<byte> gifData) {
			ImageData.Add(gifData[0]);

			gifData.RemoveAt(0);

			while (gifData[0] != 0x00) {
				int countOfFollowingDataBytes = gifData[0];

				for (int i = 0; i <= countOfFollowingDataBytes; i++) ImageData.Add(gifData[i]);

				gifData.RemoveRange(0, countOfFollowingDataBytes + 1);
			}

			ImageData.Add(gifData[0]);

			gifData.RemoveAt(0);
		}

		private void ThrowAwayExtensionBlock(List<byte> gifData) {
			gifData.RemoveRange(0, 2); // Delete ExtensionBlockIndicator and ExtensionDetermination

			while (gifData[0] != 0) gifData.RemoveRange(0, gifData[0] + 1);

			gifData.RemoveAt(0);
		}
	}
}
