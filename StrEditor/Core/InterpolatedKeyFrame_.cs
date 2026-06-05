using System.Windows;
using GRF.FileFormats.StrFormat;

namespace StrEditor.Core {
	public class InterpolatedKeyFrame {
		public float Angle;
		public float Delay;
		public float DelayStrKeyFrame;
		public float[] Color = new float[4];
		public Point Offset;
		public Point Scale;
		public int TextureIndex;
		public int TextureIndexStrKeyFrame;
		public int AnimationType;
		public int SourceAlpha;
		public int DestinationAlpha;
		public int FrameIndex;
		public float[] Vertices = new float[8];
		public float[] TextCoords = new float[8];

		public bool Interpolated;
		public StrKeyFrame KeyFrame;
		public StrKeyFrame InterpolateBaseKeyFrame;
		public StrKeyFrame InterpolateMidKeyFrame;
		public StrKeyFrame InterpolateNextKeyFrame;
		public int LayerIdx;
		public int KeyIndex;

		public InterpolatedKeyFrame() {
		}

		public InterpolatedKeyFrame(InterpolatedKeyFrame keyFrame) {
			Offset = keyFrame.Offset;
			Angle = keyFrame.Angle;
			Scale = keyFrame.Scale;
		}

		public bool Dirty { get; set; }

		public StrKeyFrame ToKeyFrame(int type = 0) {
			StrKeyFrame frame = new StrKeyFrame();
			frame.Angle = Angle;
			frame.TextureIndex = TextureIndexStrKeyFrame;
			//frame.AnimationType

			for (int i = 0; i < 4; i++) {
				frame.Color[i] = Color[i];
			}

			for (int i = 0; i < 8; i++) {
				frame.Xy[i] = Vertices[i];
				frame.Uv[i] = TextCoords[i];
			}

			frame.AnimationType = AnimationType;
			frame.FrameIndex = FrameIndex;
			frame.Offset = new GRF.Graphics.Point(Offset.X, Offset.Y);

			if (type == 0) {
				frame.SourceAlpha = SourceAlpha;
				frame.DestinationAlpha = DestinationAlpha;	
			}

			frame.Type = 0;
			frame.Delay = DelayStrKeyFrame;

			return frame;
		}

		public static InterpolatedKeyFrame InterpolateSub(Str str, int layerIdx, int frameIdx, StrKeyFrame frame0, StrKeyFrame frame1, bool interpolationOnly = false) {
			StrKeyFrame[] frames = { frame0, frame1 };
			InterpolatedKeyFrame inter = new InterpolatedKeyFrame();
			
			inter.LayerIdx = layerIdx;
			inter.KeyIndex = -1;
			inter.FrameIndex = interpolationOnly ? frame0.FrameIndex : frameIdx;

			if (frame1 == null) {
				inter.Angle = frames[0].Angle;
				inter.Color[0] = frames[0].Color[0];
				inter.Color[1] = frames[0].Color[1];
				inter.Color[2] = frames[0].Color[2];
				inter.Color[3] = frames[0].Color[3];

				for (int i = 0; i < 8; i++) {
					inter.Vertices[i] = frames[0].Xy[i];
					inter.TextCoords[i] = frames[0].Uv[i];
				}

				inter.Delay = frames[0].Delay;
				inter.AnimationType = frames[0].AnimationType;
				inter.TextureIndex = (int)frames[0].TextureIndex;
				inter.SourceAlpha = frames[0].SourceAlpha;
				inter.DestinationAlpha = frames[0].DestinationAlpha;
				inter.Offset = new Point(frames[0].Offset.X, frames[0].Offset.Y);
				inter.KeyIndex = str.Layers[layerIdx].FrameIndex2KeyIndex[frameIdx];
				inter.KeyFrame = frames[0];
				inter.Interpolated = true;
				return inter;
			}

			var mult = 1d / (frames[1].FrameIndex - frames[0].FrameIndex) * (frameIdx - frames[0].FrameIndex);
			float subMult = 1f;

			if (interpolationOnly) {
				mult = 1d / (frames[1].FrameIndex - frames[0].FrameIndex);
				subMult = 0;
			}

			inter.Angle = (float)((frames[1].Angle - frames[0].Angle) * mult + frames[0].Angle * subMult);
			inter.Color[0] = (float)((frames[1].Color[0] - frames[0].Color[0]) * mult + frames[0].Color[0] * subMult);
			inter.Color[1] = (float)((frames[1].Color[1] - frames[0].Color[1]) * mult + frames[0].Color[1] * subMult);
			inter.Color[2] = (float)((frames[1].Color[2] - frames[0].Color[2]) * mult + frames[0].Color[2] * subMult);
			inter.Color[3] = (float)((frames[1].Color[3] - frames[0].Color[3]) * mult + frames[0].Color[3] * subMult);

			for (int i = 0; i < 8; i++) {
				inter.Vertices[i] = (float)((frames[1].Xy[i] - frames[0].Xy[i]) * mult + frames[0].Xy[i] * subMult);
				inter.TextCoords[i] = (float)((frames[1].Uv[i] - frames[0].Uv[i]) * mult + frames[0].Uv[i] * subMult);
			}

			inter.TextureIndex = (int)frames[0].TextureIndex;

			if (frames[0].AnimationType == 3 || frames[0].AnimationType == 2) {
				var rate = frames[0].Delay * (frameIdx - frames[0].FrameIndex) + frames[0].TextureIndex;
				inter.TextureIndex = (int)rate;

				if (frames[0].AnimationType == 2 && inter.TextureIndex >= str[layerIdx].TextureNames.Count) {
					inter.TextureIndex = str[layerIdx].TextureNames.Count - 1;
				}
				else {
					inter.TextureIndex = ((int)rate) % str[layerIdx].TextureNames.Count;
				}
			}

			inter.Delay = frames[0].Delay;
			inter.DelayStrKeyFrame = frames[0].Delay;
			inter.AnimationType = frames[0].AnimationType;
			inter.TextureIndexStrKeyFrame = 0;
			inter.SourceAlpha = frames[0].SourceAlpha;
			inter.DestinationAlpha = frames[0].DestinationAlpha;
			inter.Offset = new Point((float)((frames[1].Offset.X - frames[0].Offset.X) * mult + frames[0].Offset.X * subMult), (float)((frames[1].Offset.Y - frames[0].Offset.Y) * mult + frames[0].Offset.Y * subMult));
			inter.Interpolated = true;

			return inter;
		}

		public static InterpolatedKeyFrame Interpolate(Str str, int layerIdx, int frameIdx, bool interpolationOnly = false) {
			var strFrames = str.Layers[layerIdx].KeyFrames;
			StrKeyFrame[] frames = new StrKeyFrame[2];
			int keyFrameIdx = str.Layers[layerIdx].FrameIndex2KeyIndex[frameIdx];

			if (keyFrameIdx < 0)
				return null;

			frames[0] = strFrames[keyFrameIdx];

			if (interpolationOnly) {
				//if (keyFrameIdx + 1 < strFrames.Count && strFrames[keyFrameIdx + 1].Type == 0) {
				//	frames[1] = strFrames[keyFrameIdx + 1];
				//}
				//else if (keyFrameIdx + 1 < strFrames.Count && strFrames[keyFrameIdx + 1].Type == 1) {
				//	frames[1] = strFrames[keyFrameIdx];
				//
				//	if (keyFrameIdx + 2 < strFrames.Count && strFrames[keyFrameIdx + 2].Type == 0) {
				//		frames[1] = strFrames[keyFrameIdx + 2];
				//	}
				//}
			}
			else {
				if (frames[0].IsInterpolated) {
					frames[1] = frames[0];

					if (keyFrameIdx + 1 < strFrames.Count) {
						frames[1] = strFrames[keyFrameIdx + 1];
					}
					else {
						frames[1] = new StrKeyFrame(frames[0]);
						frames[1].FrameIndex = str.MaxKeyFrame;
					}
				}
				else {
					frames[1] = null;
				}
			}

			if (frames[0] == null) {
				return null;
			}

			InterpolatedKeyFrame inter = new InterpolatedKeyFrame();
			inter.LayerIdx = layerIdx;
			inter.KeyIndex = -1;
			inter.FrameIndex = frameIdx;

			if (frames[1] == null || frames[0].FrameIndex == frameIdx) {
				inter.Angle = frames[0].Angle;
				inter.Color[0] = frames[0].Color[0];
				inter.Color[1] = frames[0].Color[1];
				inter.Color[2] = frames[0].Color[2];
				inter.Color[3] = frames[0].Color[3];

				for (int i = 0; i < 8; i++) {
					inter.Vertices[i] = frames[0].Xy[i];
					inter.TextCoords[i] = frames[0].Uv[i];
				}

				inter.Delay = frames[0].Delay;
				inter.AnimationType = frames[0].AnimationType;
				inter.TextureIndex = (int)frames[0].TextureIndex;
				inter.SourceAlpha = frames[0].SourceAlpha;
				inter.DestinationAlpha = frames[0].DestinationAlpha;
				inter.Offset = new Point(frames[0].Offset.X, frames[0].Offset.Y);
				inter.KeyIndex = keyFrameIdx;
				inter.KeyFrame = frames[0];
				inter.Interpolated = false;
			}
			else {
				inter = InterpolateSub(str, layerIdx, frameIdx, frames[0], frames[1]);
				inter.KeyIndex = keyFrameIdx;
			}

			return inter;
		}

		public static void ConvertToFrame(InterpolatedKeyFrame currentFrame, Str str) {
			if (currentFrame.Interpolated) {
				StrKeyFrame frame = currentFrame.ToKeyFrame();

				int frameIndex = currentFrame.FrameIndex;

				var layer = str[currentFrame.LayerIdx];
				var baseKeyIndex = layer.FrameIndex2KeyIndex[frameIndex];
				
				if (baseKeyIndex < 0)
					return;

				str.Commands.Begin();

				if (layer[baseKeyIndex].FrameIndex == frameIndex - 1)
					str.Commands.SetInterpolated(currentFrame.LayerIdx, baseKeyIndex, false);

				if (layer[baseKeyIndex + 1] != null && layer[baseKeyIndex + 1].FrameIndex != frameIndex + 1)
					frame.IsInterpolated = true;

				str.Commands.AddKey(currentFrame.LayerIdx, baseKeyIndex + 1, frame);
				currentFrame.KeyIndex = baseKeyIndex + 1;
				currentFrame.Interpolated = false;
			}
		}
	}
}
