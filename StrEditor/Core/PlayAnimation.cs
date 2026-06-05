using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using GRF.FileFormats.StrFormat;
using GRF.Threading;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF.Styles;

namespace StrEditor.Core {
	public class PlayAnimation {
		private PlayThread _playThread;
		private FancyButton _play;
		private StrController _controller;

		public bool IsPlaying { get; private set; }

		public PlayAnimation(FancyButton play) {
			_play = play;
			_play.Click += _play_Click;
			_updatePlay(false);
		}

		public void InitComponent(StrController controller) {
			_controller = controller;

			_playThread = new PlayThread(_controller, this);
			_playThread.Start();

			ApplicationShortcut.Link(ApplicationShortcut.FromString("Space", "StrEditor.PlayPause"), () => _play_Click(null, null), _controller.StrEditorWindow);
		}

		public void Play() {
			if (IsPlaying)
				return;

			_play.Dispatch(p => {
				_updatePlay(true);

				IsPlaying = true;
				_playThread.Resume();
			});
		}

		public void Stop() {
			if (!IsPlaying)
				return;

			_play.Dispatch(p => {
				IsPlaying = false;
				_updatePlay(false);
			});
		}

		private void _play_Click(object sender, RoutedEventArgs e) {
			if (IsPlaying)
				Stop();
			else
				Play();
		}

		private void _updatePlay(bool state) {
			_play.IsPressed = state;
			((TextBlock)_play.FindName("_tbIdentifier")).Margin = new Thickness(3, 0, 0, 3);
			((Grid)((Grid)((Border)_play.FindName("_border")).Child).Children[2]).HorizontalAlignment = HorizontalAlignment.Left;
			((Grid)((Grid)((Border)_play.FindName("_border")).Child).Children[2]).Margin = new Thickness(2, 0, 0, 0);

			if (_play.IsPressed) {
				_play.ImagePath = "stop2.png";
				_play.TextHeader = "Stop";
			}
			else {
				_play.ImagePath = "play.png";
				_play.TextHeader = "Play";
			}
		}

		public class PlayThread : PausableThread {
			private StrController _controller;
			private PlayAnimation _animator;

			public PlayThread(StrController controller, PlayAnimation animator) {
				_controller = controller;
				_animator = animator;
			}

			public void Start() {
				GrfThread.Start(_start);
			}

			private void _start() {
				while (!IsTerminated) {
					try {
						if (!_animator.IsPlaying)
							Pause();

						Str str = _controller.Str;

						if (str == null || str.KeyFrameCount <= 1) {
							_animator.Stop();
							continue;
						}

						if (str.Fps >= 500) {
							_animator.Stop();
							ErrorHandler.HandleException("The animation speed is too fast and might cause issues. The animation will not be displayed.", ErrorLevel.NotSpecified);
							continue;
						}

						int offset = _controller.TimelineEditor.Dispatch(p => p.TimelineFrameIndex);

						Stopwatch watch = new Stopwatch();

						const int interval = 10;

						watch.Start();
						while (_animator.IsPlaying) {
							float frame = watch.ElapsedMilliseconds / 1000f;
							frame = frame * str.Fps;
							frame += offset;

							int frameIndex = ((int)frame) % str.KeyFrameCount;

							_controller.TimelineEditor.Dispatch(p => {
								p.TimelineFrameIndex = frameIndex;
								p.FocusTimelineIntoView();
							});

							Thread.Sleep(interval);
						}
					}
					catch (Exception err) {
						_animator.Stop();
						ErrorHandler.HandleException(err);
					}
				}
			}
		}
	}
}
