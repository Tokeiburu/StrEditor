using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;
using GRF.Graphics;

namespace StrEditor.Core.GifExporter {
	public class GifData {
		private bool _isPngMode;
		private bool _isGifMode;

		public bool IsGifMode {
			get { return _isGifMode; }
			set {
				_isGifMode = _isPngMode = false;
				_isGifMode = value;
			}
		}

		public bool IsPngMode {
			get { return _isPngMode; }
			set {
				_isGifMode = _isPngMode = false;
				_isPngMode = value;
			}
		}

		public List<TkVector2> Points;
		public List<TkVector2> PointsCopy = new List<TkVector2>();
		public Image Bitmap;
		public bool IsSaving;
		public int Fps;
		public string PngPath { get; set; }
		public int FrameIndexStart;
		public int FrameIndexEnd;
		private StrController _controller;

		public delegate void PointsChangedEventHandler(object sender);
		public event PointsChangedEventHandler PointsChanged;
		public void OnPointsChanged() => PointsChanged?.Invoke(this);

		public GifData(StrController controller) {
			_controller = controller;

			Points = new List<TkVector2>();

			for (int i = 0; i < 8; i++)
				Points.Add(new TkVector2());
		}

		public List<TkVector2> CalculatePoints() {
			var str = _controller.Str;
			var layerRenderers = _controller.FrameViewer.StrRenderer.LayerRenderers;

			List<TkVector2> allPoints = new List<TkVector2>();

			for (int frameIndex = 0; frameIndex < str.KeyFrameCount; frameIndex++) {
				for (int layerIndex = 0; layerIndex < str.Layers.Count && layerIndex < layerRenderers.Count; layerIndex++) {
					var renderer = layerRenderers[layerIndex];
					renderer.RenderSub(_controller.FrameViewer, frameIndex, layerIndex, false);

					if (renderer.Inter == null)
						continue;

					for (int i = 0; i < 4; i++) {
						TkVector2 p = new TkVector2(renderer.VertexData[5 * i + 0], renderer.VertexData[5 * i + 1]);
						p.RotateZ(renderer.Inter.Angle);
						p += new TkVector2(renderer.Model[3, 0], renderer.Model[3, 1]);
						allPoints.Add(p);
					}
				}
			}

			TkVector2 topLeft = allPoints[0];
			TkVector2 bottomRight = allPoints[0];
			List<TkVector2> vertex = new List<TkVector2>(4);

			foreach (var p in allPoints) {
				if (p.X < topLeft.X)
					topLeft.X = p.X;
				if (p.Y > topLeft.Y)
					topLeft.Y = p.Y;
				if (p.X > bottomRight.X)
					bottomRight.X = p.X;
				if (p.Y < bottomRight.Y)
					bottomRight.Y = p.Y;
			}

			vertex.Add(new TkVector2(topLeft.X, topLeft.Y));    // top left
			vertex.Add(new TkVector2((topLeft.X + bottomRight.X) / 2, topLeft.Y));  // mid top
			vertex.Add(new TkVector2(bottomRight.X, topLeft.Y));    // top right
			vertex.Add(new TkVector2(bottomRight.X, (topLeft.Y + bottomRight.Y) / 2));  // mid right
			vertex.Add(new TkVector2(bottomRight.X, bottomRight.Y));    // bottom right
			vertex.Add(new TkVector2((topLeft.X + bottomRight.X) / 2, bottomRight.Y));  // mid bottom
			vertex.Add(new TkVector2(topLeft.X, bottomRight.Y));    // bottom left
			vertex.Add(new TkVector2(topLeft.X, (topLeft.Y + bottomRight.Y) / 2));  // mid left

			Points = vertex;
			return vertex;
		}

		public TkVector2 CenterOffset {
			get {
				var midPoint = new TkVector2();

				midPoint += Points[0];
				midPoint += Points[2];
				midPoint += Points[4];
				midPoint += Points[6];

				return midPoint / 4;
			}
		}

		public int BoundsWidth => (int)(Points[2].X - Points[0].X);
		public int BoundsHeight => (int)(Points[0].Y - Points[4].Y);
	}
}
