using System;
using System.Linq;
using System.Windows;
using GRF.FileFormats.StrFormat;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StrEditor.Core.TimelineEditor.State {
	public class SelectionTarget {
		public int StartFrame;
		public int FrameCount;

		public int End => StartFrame + FrameCount - 1;

		public int Focus;
		public int FocusMargin;
	}

	public struct KeyframeSegment {
		public int FrameIndex;
		public int Length;
		public int EndIndex => FrameIndex + Length;
		public int TargetIndex;

		public static KeyframeSegment Create(int frameIndex, int endIndex, int targetIndex = -1) {
			return new KeyframeSegment { FrameIndex = frameIndex, Length = endIndex - frameIndex, TargetIndex = targetIndex };
		}
	}

	public class MoveState {
		public enum Direction {
			Left, Right, Top, Bottom
		}

		public static int FindIndex(Direction direction, Selection selection, Str str) {
			var layer = str[selection.Current.Y];
			var frameIndex = selection.Current.X;
			int targetFrameIndex = 0;
			var frameIndex2KeyIndex = layer.FrameIndex2KeyIndex;
			int keyIndex = frameIndex2KeyIndex[selection.Current.X];

			switch (direction) {
				case Direction.Right:
					FindFrameNeighbors(selection, layer, frameIndex, out KeyframeSegment _, out KeyframeSegment right);
					targetFrameIndex = right.TargetIndex;
					return selection.Current.X < selection.Anchor.X && targetFrameIndex > selection.Anchor.X ? selection.Anchor.X : targetFrameIndex;
				case Direction.Left:
					FindFrameNeighbors(selection, layer, frameIndex, out KeyframeSegment left, out KeyframeSegment _);
					targetFrameIndex = left.TargetIndex;
					return selection.Current.X > selection.Anchor.X && targetFrameIndex < selection.Anchor.X ? selection.Anchor.X : targetFrameIndex;
			}

			return -1;
		}

		public static void FindFrameNeighbors(Selection selection, StrLayer layer, int frameIndex, out KeyframeSegment left, out KeyframeSegment right) {
			int maxKeyFrames = layer.FrameIndex2KeyIndex.Length;

			if (frameIndex > 0) {
				int keyIndex = layer.FrameIndex2KeyIndex[frameIndex - 1];

				// Find left segment
				if (keyIndex == -1) {
					keyIndex = layer.GetPreviousKeyIndex(frameIndex - 1);

					if (keyIndex == -1)
						left = KeyframeSegment.Create(0, frameIndex);
					else
						left = KeyframeSegment.Create(layer[keyIndex].FrameIndex + 1, frameIndex);
				}
				else {
					left = KeyframeSegment.Create(layer[keyIndex].FrameIndex, frameIndex);
				}
			}
			else {
				left = KeyframeSegment.Create(0, 0);
			}

			if (frameIndex + 1 < maxKeyFrames) {
				// Find right segment
				int keyIndex = layer.FrameIndex2KeyIndex[frameIndex + 1];
				int diff = selection.Current.X < selection.Anchor.X ? 1 : 0;

				if (keyIndex == -1) {
					keyIndex = layer.GetNextKeyIndex(frameIndex + 1);

					if (keyIndex == -1)
						right = KeyframeSegment.Create(frameIndex + 1, maxKeyFrames);
					else
						right = KeyframeSegment.Create(frameIndex + 1, layer[keyIndex].FrameIndex);
				}
				else {
					int endIndex = 0;
					var nextKeyFrame = layer[keyIndex + 1];

					if (nextKeyFrame == null)
						endIndex = layer[keyIndex].FrameIndex + 1;
					else
						endIndex = nextKeyFrame.FrameIndex;

					right = KeyframeSegment.Create(frameIndex + 1, endIndex);
				}
			}
			else {
				right = KeyframeSegment.Create(maxKeyFrames, maxKeyFrames);
			}

			// Find target locations
			if (selection.Current.X >= selection.Anchor.X) {
				right.TargetIndex = right.EndIndex - 1;
			}
			else {
				int keyIndex = layer.FrameIndex2KeyIndex[frameIndex];

				// Is the current frame a single length keyFrame?
				if (layer.GetKeyFrameLength(keyIndex) == 1)
					right.TargetIndex = frameIndex + 1;
				else
					right.TargetIndex = right.EndIndex;
			}

			if (selection.Current.X <= selection.Anchor.X) {
				left.TargetIndex = left­.FrameIndex;
			}
			else {
				int keyIndex = layer.FrameIndex2KeyIndex[frameIndex];
				
				// Is the current frame a single length keyFrame?
				if (layer.GetKeyFrameLength(keyIndex) == 1)
					left.TargetIndex = frameIndex - 1;
				else
					left.TargetIndex = left.FrameIndex - 1;
			}
		}
	}
}
