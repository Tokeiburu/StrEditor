using System.Collections.Generic;
using System.Linq;
using GRF.FileFormats.StrFormat;

namespace StrEditor.Core.TimelineEditor.State {
	public class CopyState {
		private readonly Str _str;
		private List<StrKeyFrame> _copyFrames;
		private List<int> _frameIndexesToDelete;
		public bool CreateNewLeft;
		public bool CreateNewRight;
		public StrKeyFrame NewLeftKeyFrame;
		public StrKeyFrame NewRightKeyFrame;
		private int _layerIndex;
		private readonly List<StrKeyFrame> _oriCopyFrames;

		public CopyState(Str str, int layerIndex, List<StrKeyFrame> oriCopyFrames) {
			_str = str;
			_oriCopyFrames = oriCopyFrames;
			_layerIndex = layerIndex;
		}

		public void Init(int frameIndexStart) {
			_copyFrames = new List<StrKeyFrame>();
			
			// Adjust frame indexes
			for (int i = 0; i < _oriCopyFrames.Count; i++) {
				var keyFrame = new StrKeyFrame(_oriCopyFrames[i]);

				keyFrame.FrameIndex += frameIndexStart;

				if (keyFrame.FrameIndex < 0)
					continue;

				if (keyFrame.Type == 0 && keyFrame.FrameIndex >= _str.KeyFrameCount)
					break;

				if (keyFrame.Type == 0 && keyFrame.IsInterpolated && keyFrame.FrameIndex == _str.KeyFrameCount - 1) {
					keyFrame.IsInterpolated = false;
				}

				_copyFrames.Add(keyFrame);
			}

			_frameIndexesToDelete = new List<int>();

			for (int i = 0; i < _oriCopyFrames.Count; i++) {
				_frameIndexesToDelete.Add(_copyFrames[i].FrameIndex);

				if (_copyFrames[i].IsInterpolated && i + 1 < _oriCopyFrames.Count) {
					for (int j = _copyFrames[i].FrameIndex + 1; j < _copyFrames[i + 1].FrameIndex; j++) {
						_frameIndexesToDelete.Add(j);
					}
				}
			}

			_initSub();
		}

		private void _initSub() {
			// Check left
			var leftKeyFrame = _copyFrames[0];
			var rightKeyFrame = _copyFrames.Last();
			var layer = _str[_layerIndex];

			if (leftKeyFrame.Type == 2 && IsFrameIndexInterpolated(_str, _layerIndex, leftKeyFrame.FrameIndex - 1)) {
				CreateNewLeft = true;
				int keyIndex = layer.FrameIndex2KeyIndex[leftKeyFrame.FrameIndex - 1];
				NewLeftKeyFrame = InterpolatedKeyFrame.InterpolateSub(_str, _layerIndex, leftKeyFrame.FrameIndex - 1, layer[keyIndex], layer[keyIndex + 1]).ToKeyFrame();
			}

			if (rightKeyFrame.Type == 2 && IsFrameIndexInterpolated(_str, _layerIndex, rightKeyFrame.FrameIndex + 1) && layer[layer.FrameIndex2KeyIndex[rightKeyFrame.FrameIndex + 1]].FrameIndex != rightKeyFrame.FrameIndex + 1) {
				CreateNewRight = true;
				int keyIndex = layer.FrameIndex2KeyIndex[rightKeyFrame.FrameIndex + 1];
				NewRightKeyFrame = InterpolatedKeyFrame.InterpolateSub(_str, _layerIndex, rightKeyFrame.FrameIndex + 1, layer[keyIndex], layer[keyIndex + 1]).ToKeyFrame();
				NewRightKeyFrame.IsInterpolated = true;
			}

			if (rightKeyFrame.Type == 0 && IsFrameIndexInterpolated(_str, _layerIndex, rightKeyFrame.FrameIndex + 1) && !rightKeyFrame.IsInterpolated) {
				rightKeyFrame.IsInterpolated = true;
			}
		}

		public static bool IsFrameIndexInterpolated(Str str, int layerIndex, int frameIndex) {
			if (frameIndex < 0 || frameIndex >= str.KeyFrameCount)
				return false;

			int baseKeyIndex = str[layerIndex].FrameIndex2KeyIndex[frameIndex];

			if (baseKeyIndex < 0)
				return false;

			return str[layerIndex][baseKeyIndex].IsInterpolated;
		}

		public void Paste(int insertKeyIndex) {
			if (CreateNewLeft)
				//_str.Commands.AddKey(_layerIndex, insertKeyIndex++, NewLeftKeyFrame);
				_str.Commands.SetKey(_layerIndex, NewLeftKeyFrame);

			for (int i = 0; i < _copyFrames.Count; i++) {
				if (_copyFrames[i].Type == 2)
					continue;

				//_str.Commands.AddKey(_layerIndex, insertKeyIndex++, _copyFrames[i]);
				_str.Commands.SetKey(_layerIndex, _copyFrames[i]);
			}

			if (CreateNewRight)
				//_str.Commands.AddKey(_layerIndex, insertKeyIndex++, NewRightKeyFrame);
				_str.Commands.SetKey(_layerIndex, NewRightKeyFrame);
		}

		public int Delete() {
			int insertKeyIndex = -1;
			var layer = _str[_layerIndex];

			if (_frameIndexesToDelete.Count > 0)
				insertKeyIndex = _deleteKeyFrames(_layerIndex, _frameIndexesToDelete[0], _frameIndexesToDelete.Last() - _frameIndexesToDelete[0] + 1);

			if (insertKeyIndex == -1) {
				// Nothing was deleted
				int i;

				for (i = 0; i < layer.KeyFrames.Count; i++) {
					if (_frameIndexesToDelete[0] < layer[i].FrameIndex) {
						insertKeyIndex = i;
						break;
					}
				}

				if (i == layer.KeyFrames.Count) {
					insertKeyIndex = layer.KeyFrames.Count;
				}
			}

			return insertKeyIndex;
		}

		private int _deleteKeyFrames(int layerIndex, int frameIndexStart, int count) {
			int previousKeyIndex = -1;
			int lastKeyIndex = -1;
			var layer = _str[layerIndex];

			for (int i = count + frameIndexStart - 1; i >= frameIndexStart; i--) {
				int frameIndex = i;

				if (frameIndex < 0 || frameIndex >= _str.KeyFrameCount)
					continue;

				int keyIndex = layer.FrameIndex2KeyIndex[frameIndex];

				if (keyIndex == previousKeyIndex)
					continue;

				if (layer[keyIndex] == null) {
					keyIndex = previousKeyIndex;
					continue;
				}

				if (layer[keyIndex].FrameIndex < frameIndexStart)
					continue;

				previousKeyIndex = lastKeyIndex = keyIndex;
				_str.Commands.DeleteKey(layerIndex, keyIndex);
			}

			return lastKeyIndex;
		}
	}
}
