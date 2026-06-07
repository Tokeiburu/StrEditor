using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ErrorManager;
using GRF.FileFormats.StrFormat;
using GRF.FileFormats.StrFormat.Commands;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.TimelineEditor.Logic;
using StrEditor.Core.TimelineEditor.Rendering;
using StrEditor.Core.TimelineEditor.State;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using Utilities;
using Utilities.Commands;
using static StrEditor.Core.TimelineEditor.Logic.EditorCommands;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace StrEditor.Core.TimelineEditor.Controls {
	/// <summary>
	/// Interaction logic for KeyFrameEditor.xaml
	/// </summary>
	public partial class Editor : UserControl {
		public const int MinKeyFrameHeight = 10;
		public const int MaxKeyFrameHeight = 30;
		public double KeyFrameHeight = 30d;
		public double KeyFrameWidth = 10d;
		private int _timelineFrameIndex;
		private StrController _controller;
		private int _layerCopy;
		private readonly DispatcherTimer _dispatcherTimer;
		private EditorRenderer _renderer;
		private LayerHeadersController _layerHeaderController;
		private EditorCommands _editorCommands;

		public EditorCommands Commands => _editorCommands;
		public Selection Selection;
		public Selection SelectionPreview;
		public EditorRenderer Renderer => _renderer;
		public LayerHeadersController LayerHeaderController => _layerHeaderController;
		public StrController Controller => _controller;

		private Point _oldPosition;
		private bool _hasMouseMoveScrolled = false;
		private Str _str;

		public delegate void ViewChangedEventHandler(ScrollViewer scrollViewer, double horizontalOffset);
		public event ViewChangedEventHandler ViewChanged;
		public void OnViewChanged(ScrollViewer scrollViewer, double horizontalOffset) => ViewChanged?.Invoke(scrollViewer, horizontalOffset);

		public int TimelineFrameIndex {
			get => _timelineFrameIndex;
			set {
				if (value >= _controller.Str.KeyFrameCount)
					value = 0;

				if (value == _timelineFrameIndex)
					return;

				_timelineFrameIndex = value;
				this.Dispatch(delegate {
					_controller.KeyFrameEditor.IsEnabled = _timelineFrameIndex == Selection.SelectedFrame;
				});

				OnTimelineFrameIndexChanged();
			}
		}

		internal int TimelineFrameIndexNoEvent {
			set => _timelineFrameIndex = value;
		}

		public int SelectedFrameIndex => Selection.SelectedFrame;
		public int SelectedLayerIndex => Selection.SelectedLayer;

		public bool IsFirstLayerInvisible => (_controller.Str != null) && (_controller.Str.Layers.Count > 0 && _controller.Str.Layers[0].KeyFrames.Count == 0);

		public int DrawYOffset => IsFirstLayerInvisible ? 1 : 0;

		public delegate void KeyFrameEditorEvent();

		public event KeyFrameEditorEvent PositionChanged;
		public event KeyFrameEditorEvent FrameIndexChanged;
		public event KeyFrameEditorEvent LayerIndexChanged;
		public event KeyFrameEditorEvent TimelineFrameIndexChanged;

		public virtual void OnLayerIndexChanged() => LayerIndexChanged?.Invoke();
		public virtual void OnTimelineFrameIndexChanged() => TimelineFrameIndexChanged?.Invoke();
		public virtual void OnFrameIndexChanged() => FrameIndexChanged?.Invoke();
		public virtual void OnPositionChanged() => PositionChanged?.Invoke();

		public Editor() {
			InitializeComponent();

			_dispatcherTimer = new DispatcherTimer();
			_dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
			_dispatcherTimer.Tick += new EventHandler(_dispatcherTimer_Tick);
			_keySelectorPreview._gridInternal.Background = new SolidColorBrush(Color.FromArgb(159, 104, 214, 73));

			KeyFrameHeight = StrEditorConfiguration.KeyFrameHeight;

			_gridEvents.MouseRightButtonDown += _gridEvents_MouseRightButtonDown;
			_gridEvents.MouseLeftButtonDown += _gridEvents_MouseLeftButtonDown;
			_gridEvents.MouseMove += _gridEvents_MouseMove;
			_gridEvents.MouseUp += _gridEvents_MouseUp;
		}

		private void _selection_SelectionChanged(SelectionChangedArgs args) {
			if (args.LayerChanged)
				OnLayerIndexChanged();

			if (args.FrameChanged)
				OnFrameIndexChanged();

			if (args.LayerChanged || args.FrameChanged)
				OnPositionChanged();
		}

		public void InitComponent(StrController controller) {
			_controller = controller;

			_renderer = new EditorRenderer(this);
			_editorCommands = new EditorCommands(this);
			_layerHeaderController = new LayerHeadersController(this);
			Selection = new Selection(_keySelector, _controller);
			Selection.SelectionChanged += _selection_SelectionChanged;
			SelectionPreview = new Selection(_keySelectorPreview, _controller);
			_timelinePart.Init(this);
			
			_initializeShortcuts();
			controller.StrEditorWindow.StrLoaded += _strEditorWindow_StrLoaded;
		}

		private void _strEditorWindow_StrLoaded(object sender) {
			// Remove previous event handlers
			if (_str != null) {
				_str.Commands.ModifiedStateChanged -= _commands_CommandIndexChanged;
			}

			_str = _controller.Str;
			_controller.Str.Commands.ModifiedStateChanged += _commands_CommandIndexChanged;
		}

		private void _initializeShortcuts() {
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorCreateBezier, Commands.CreateBezier, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorDeleteBezier, Commands.DeleteBezier, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorCenterOrigin, Commands.CenterOrigin, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorCopy, _miCopy, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorPaste, _miPaste, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorDelete, _miDelete, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorDeleteAll, _miDeleteAll, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorInterpolate, _miInterpolate, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorDeleteInterpolate, _miDeleteInterpolate, this);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorInsertUp, _miLayerInsertAbove, this);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorInsertDown, _miLayerInsertBelow, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorNewKey, _miNewKey, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorEndKey, _miNewEndKey, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorSetFromPrevious, _miCopyPrevious, this);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorSelectAll, _miSelectAll, this);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorTextureCopy, _miLayerCopy, this);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorTexturePaste, _miLayerPaste, this);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorDuplicate, _miLayerDuplicate, this);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorDelete, _miLayerDelete, this);

			ApplicationShortcut.Link(StrEditorCommands.LayerEditorCopy, _miCopy, _controller.FrameViewer);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorPaste, _miPaste, _controller.FrameViewer);
			ApplicationShortcut.Link(StrEditorCommands.KeyFrameEditorDelete, _miDelete, _controller.FrameViewer);

			ApplicationShortcut.Link(StrEditorCommands.StrEditorSaveAsGif, delegate {
				_controller.GifData.IsGifMode = true;
				_controller.StrEditorWindow._gifSettings.Show();
			}, _controller.StrEditorWindow);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorPasteColor, _miPasteColor, _controller.StrEditorWindow);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorPasteBlend, _miPasteBlend, _controller.StrEditorWindow);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorPasteOffset, _miPasteOffset, _controller.StrEditorWindow);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorPasteAngle, _miPasteAngle, _controller.StrEditorWindow);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorPastePositions, _miPastePositions, _controller.StrEditorWindow);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorPasteTexture, _miPasteTexture, _controller.StrEditorWindow);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorPasteAnimation, _miPasteAnimation, _controller.StrEditorWindow);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorPasteBias, _miPasteBias, _controller.StrEditorWindow);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorPasteBezier, _miPasteBezier, _controller.StrEditorWindow);

			ApplicationShortcut.Link(StrEditorCommands.FrameViewerFlipHorizontal, () => _controller.FrameViewer.Commands.FlipH(), _controller.StrEditorWindow);
			ApplicationShortcut.Link(StrEditorCommands.FrameViewerFlipVertical, () => _controller.FrameViewer.Commands.FlipV(), _controller.StrEditorWindow);

			ApplicationShortcut.Link(StrEditorCommands.FrameViewerGroupEdit, delegate {
				_controller.StrEditorWindow._buttonGroupEdit.IsChecked = !_controller.StrEditorWindow._buttonGroupEdit.IsChecked;
				StrEditorConfiguration.GroupEdit = _controller.StrEditorWindow._buttonGroupEdit.IsChecked.Value;
			}, _controller.StrEditorWindow);
		}

		public void DeleteCommandIndexEvents() {
			_controller.Str.Commands.ModifiedStateChanged -= _commands_CommandIndexChanged;
		}

		public void AddCommandIndexEvents() {
			_controller.Str.Commands.ModifiedStateChanged += _commands_CommandIndexChanged;
		}

		private UpdateDispatcher _updateDispatcher = new UpdateDispatcher(1);

		private void _commands_CommandIndexChanged(object sender, IStrCommand command) {
			try {
				if (_str.Commands.StackStatus == StackStatus.Execute && command is StrGroupCommand groupCommand && groupCommand.NullCommands.Count == 0) {
					AddPositionCommand();
				}

				_updateDispatcher.Execute(delegate {
					this.Dispatcher.BeginInvoke(new Action(delegate {
						_controller.KeyFrameEditor._tbMaxFrames.Text = _controller.Str.MaxKeyFrame.ToString(CultureInfo.InvariantCulture);

						Renderer.Reload();

						TryRecoverCommandPreviousSelection(command);
						Selection.Set(Selection);

						// Only invallidate the key frame data if layer's frame index is dirty
						// Otherwise, this will mess with the currently edited text box field
						if (Selection.SelectedLayer > -1 && Selection.SelectedLayer < _str.Layers.Count && _str[Selection.SelectedLayer].FrameIndexDirty)
							_controller.KeyFrameEditor.InvalidateKeyFrame();
						
						_controller.Str.InvalidateVisualRedraw();
					}), DispatcherPriority.Render);
				});
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private bool TryRecoverCommandPreviousSelection(IStrCommand command) {
			if (!StrEditorConfiguration.KeepTrackKeyFrameEditorCursor)
				return false;

			switch(_str.Commands.StackStatus) {
				case StackStatus.Redo:
				case StackStatus.Undo:
					break;
				default:
					return false;
			}

			SetEditorPositionCommand positionCommand = null;

			if (command is StrGroupCommand groupCommand) {
				if (_str.Commands.StackStatus == StackStatus.Undo)
					positionCommand = groupCommand.NullCommands.FirstOrDefault() as SetEditorPositionCommand;
				else
					positionCommand = groupCommand.NullCommands.LastOrDefault() as SetEditorPositionCommand;
			}

			if (positionCommand != null) {
				Selection.Set(positionCommand.FrameIndex, positionCommand.FrameCount, positionCommand.LayerIndex, positionCommand.LayerCount);
				FocusSelection(3);
				return true;
			}

			return false;
		}

		public void AddPositionCommand() {
			Selection.AddPositionCommand(_str);
		}

		private void _gridEvents_MouseUp(object sender, MouseButtonEventArgs e) {
			if (_controller.Str == null)
				return;

			if (_gridEvents.IsMouseCaptured)
				_gridEvents.ReleaseMouseCapture();

			if (SelectionPreview.IsActive) {
				if (SelectionPreview.StartLayer + SelectionPreview.LayerCount <= 1 || SelectionPreview.StartLayer >= _controller.Str.Layers.Count ||
					SelectionPreview.StartFrame + SelectionPreview.FrameCount <= 0 || SelectionPreview.StartFrame >= _controller.Str.KeyFrameCount ||
					(SelectionPreview.StartFrame == Selection.StartFrame && SelectionPreview.StartLayer == Selection.StartLayer)) {
					SelectionPreview.Deselect();
					return;
				}

				Commands.MoveSelection();
				Selection.Set(SelectionPreview);
				SelectionPreview.Deselect();
			}
		}

		private void _gridEvents_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			try {
				if (_controller.Str == null)
					return;

				var position = e.GetPosition(_primaryGrid);
				var minX = Selection.FrameCount < 0 ? (Selection.StartFrame + Selection.FrameCount + 1) * KeyFrameWidth : Selection.StartFrame * KeyFrameWidth;
				var maxX = Selection.FrameCount < 0 ? (Selection.StartFrame + 1) * KeyFrameWidth : (Selection.StartFrame + Selection.FrameCount) * KeyFrameWidth;
				var offsetY = IsFirstLayerInvisible ? 1 : 0;

				if (position.Y < (Selection.SelectedLayer - offsetY) * KeyFrameHeight || position.Y > ((Selection.SelectedLayer - offsetY) + 1) * KeyFrameHeight || position.X < minX || position.X > maxX) {
					_gridEvents_MouseMoveHandler(position, e, e.GetPosition(this), false);
				}

				_gridEvents.ContextMenu.IsOpen = true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _gridEvents_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			try {
				if (e.LeftButton != MouseButtonState.Pressed)
					return;

				var position = e.GetPosition(_primaryGrid);

				if (e.ClickCount >= 2) {
					int frameIndex = (int)(position.X / KeyFrameWidth);

					if (CopyState.IsFrameIndexInterpolated(_controller.Str, Selection.SelectedLayer, frameIndex)) {
						int baseKeyIndex = _controller.Str[Selection.SelectedLayer].FrameIndex2KeyIndex[frameIndex];

						var currentPosition = Selection.Anchor;

						Selection.Set(
							_controller.Str[Selection.SelectedLayer, baseKeyIndex].FrameIndex, 
							(baseKeyIndex + 1 < _controller.Str[Selection.SelectedLayer].KeyFrames.Count ? _controller.Str[Selection.SelectedLayer, baseKeyIndex + 1].FrameIndex : _controller.Str.KeyFrameCount) - _controller.Str[Selection.SelectedLayer, baseKeyIndex].FrameIndex,
							Selection.StartLayer, Selection.LayerCount, sanitize: true, enableEvents: false);
						Selection.SetMoveOverride(currentPosition);
						return;
					}
				}

				if (_isMouseOverKeyNode(position)) {
					if (!Selection.IsWithinSelection(position)) {
						_gridEvents_MouseMoveHandler(position, e, e.GetPosition(this), false);
					}
					SelectionPreview.Set(Selection, false);
					SelectionPreview.SetOffset((int)(position.X / KeyFrameWidth), (int)(position.Y / KeyFrameHeight) + DrawYOffset);
					return;
				}

				SelectionPreview.Deselect();

				if (!_gridEvents.IsMouseCaptured) {
					_gridEvents.CaptureMouse();
				}
				else {
					_gridEvents_MouseMoveHandler(position, e, e.GetPosition(this), false);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private bool _isMouseOverKeyNode(Point position) {
			if ((int)position.Y % KeyFrameHeight > KeyFrameHeight - 11) {
				bool isMouseWithinSelection = Selection.IsWithinSelection(position);

				if (StrEditorConfiguration.MustSelectKeyFrameBeforeDragging && !isMouseWithinSelection)
					return false;

				if (!isMouseWithinSelection || Selection.Length == 1) {
					int frameIndex = (int)(position.X / KeyFrameWidth);
					int mouseLayerIndex = (int)(position.Y / KeyFrameHeight) + (IsFirstLayerInvisible ? 1 : 0);

					if (mouseLayerIndex < 0 || mouseLayerIndex >= _controller.Str.Layers.Count ||
						frameIndex < 0 || frameIndex >= _controller.Str.KeyFrameCount)
						return false;

					int baseKeyIndex = _controller.Str[mouseLayerIndex].FrameIndex2KeyIndex[frameIndex];

					if (baseKeyIndex > -1 && _controller.Str[mouseLayerIndex, baseKeyIndex].FrameIndex == frameIndex)
						return true;

					return false;
				}

				return true;
			}

			return false;
		}

		private void _gridEvents_MouseMoveHandler(Point position, MouseEventArgs e, Point positionAbsolute, bool fromDispatcher) {
			try {
				int drawOffsetY = IsFirstLayerInvisible ? 1 : 0;

				// Find key!
				int selectedLayerIndex = (int)(position.Y / KeyFrameHeight) + drawOffsetY;
				int selectedFrameIndex = (int)(position.X / KeyFrameWidth);
				
				bool hasMouseMoveScrolled = _hasMouseMoveScrolled;
				_hasMouseMoveScrolled = false;

				if (fromDispatcher && !_isWithinViewport(position)) {
					_dispatcherTimer.Stop();
					_dispatcherTimer.Start();
				}
				else if (!fromDispatcher && !_isWithinViewport(position)) {
					if (hasMouseMoveScrolled && (positionAbsolute - _oldPosition).Length < 3) {
						_dispatcherTimer.Stop();
						_dispatcherTimer.Start();

						if (e != null) {
							e.Handled = true;
						}

						return;
					}
				}

				_dispatcherTimer.Stop();
				_oldPosition = positionAbsolute;

				FocusPosition(selectedFrameIndex, selectedLayerIndex);

				if (_controller.Str == null)
					return;

				if (!_gridEvents.IsMouseCaptured)
					_gridEvents.CaptureMouse();

				if (SelectionPreview.IsActive) {
					SelectionPreview.PreviewTo(selectedFrameIndex, selectedLayerIndex);
					return;
				}

				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) {
					Selection.SetTarget(selectedFrameIndex, selectedLayerIndex);
				}
				else {
					Selection.SetXY(selectedFrameIndex, selectedLayerIndex);
				}

				if (e != null) {
					e.Handled = true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _dispatcherTimer_Tick(object sender, EventArgs e) {
			if (!_gridEvents.IsMouseCaptured) {
				_dispatcherTimer.Stop();
				return;
			}

			_gridEvents_MouseMoveHandler(Mouse.GetPosition(_primaryGrid), null, new Point(), true);
		}

		private void _gridEvents_MouseMove(object sender, MouseEventArgs e) {
			try {
				if (e.LeftButton != MouseButtonState.Pressed) {
					var position = e.GetPosition(_primaryGrid);

					if (_isMouseOverKeyNode(position)) {
						Cursor = Cursors.Hand;
						return;
					}

					Cursor = null;
					return;
				}

				Cursor = null;
				_gridEvents_MouseMoveHandler(e.GetPosition(_primaryGrid), e, e.GetPosition(this), false);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void UpdateBackgroundBrush() {
			var str = _controller.Str;

			if (str != null)
				_gridLayerBackground.Height = KeyFrameHeight * (str.Layers.Count + (IsFirstLayerInvisible ? -1 : 0));

			_visualBrushLayerBackground.Viewport = new Rect(10 - _svKeyFrames.HorizontalOffset, -_svKeyFrames.VerticalOffset, KeyFrameWidth * 5, KeyFrameHeight);
		}

		private void _svKeyFrames_ScrollChanged(object sender, ScrollChangedEventArgs e) {
			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			try {
				if (e.VerticalChange != 0) {
					double stepSize = KeyFrameHeight;

					var scrollViewer = (ScrollViewer)sender;
					var steps = Math.Round(scrollViewer.VerticalOffset / stepSize, 0);
					var scrollPosition = steps * stepSize;
					if (scrollPosition >= scrollViewer.ScrollableHeight || scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight) {
						scrollViewer.ScrollToBottom();
						_svLayerHeaders.ScrollToVerticalOffset(scrollViewer.VerticalOffset);
						_svMoveLayers.ScrollToVerticalOffset(scrollViewer.VerticalOffset);
						return;
					}
					scrollViewer.ScrollToVerticalOffset(scrollPosition);
					_svLayerHeaders.ScrollToVerticalOffset(scrollPosition);
					_svMoveLayers.ScrollToVerticalOffset(scrollPosition);
				}
				else {
					double stepSize = KeyFrameWidth;

					var scrollViewer = (ScrollViewer)sender;
					var steps = Math.Round(scrollViewer.HorizontalOffset / stepSize, 0);
					var scrollPosition = steps * stepSize;
					if (scrollPosition >= scrollViewer.ScrollableWidth) {
						scrollViewer.ScrollToRightEnd();
						OnViewChanged(_svKeyFrames, scrollViewer.HorizontalOffset);
						return;
					}

					scrollViewer.ScrollToHorizontalOffset(scrollPosition);
					OnViewChanged(_svKeyFrames, scrollPosition);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				UpdateBackgroundBrush();
			}
		}

		private void _svMoveLayers_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
			e.Handled = true;
		}

		private void _mainGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
			try {
				var str = _controller.Str;

				if (e.Key == Key.Home) {
					e.Handled = true;

					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) {
						Selection.SetTarget(0, Selection.Current.Y);
						FocusSelection(3);
					}
					else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
						Selection.Set(0, 1, Selection.StartLayer, Selection.LayerCount);
						FocusSelection(3);
					}
					else {
						Selection.SetXY(0, Selection.Current.Y);
						FocusSelection();
					}
				}
				else if (e.Key == Key.End) {
					e.Handled = true;

					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) {
						Selection.SetTarget(str.MaxKeyFrame, Selection.Current.Y);
						FocusSelection(3);
					}
					else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
						Selection.Set(str.MaxKeyFrame, 1, Selection.StartLayer, Selection.LayerCount);
						FocusSelection(3);
					}
					else {
						Selection.SetXY(str.MaxKeyFrame, Selection.Current.Y);
						FocusSelection();
					}
				}
				else if (e.Key == Key.Right) {
					e.Handled = true;

					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift)) {
						Selection.SetTarget(MoveState.FindIndex(MoveState.Direction.Right, Selection, str), Selection.Current.Y);
						FocusSelection(3);
					}
					else if (Keyboard.Modifiers == ModifierKeys.Control) {
						int keyIndex = str[Selection.Current.Y].GetNextKeyIndex(Selection.Current.X);
						Selection.Set(keyIndex < 0 ? str.KeyFrameCount : str[Selection.Current.Y, keyIndex].FrameIndex, 1, Selection.StartLayer, Selection.LayerCount);
						FocusSelection();
					}
					else if (Keyboard.Modifiers == ModifierKeys.Shift) {
						Selection.SetTarget(++Selection.CurrentX, Selection.Current.Y);
						FocusSelection(3);
					}
					else {
						Selection.SetXY(++Selection.CurrentX, Selection.Current.Y);
						FocusSelection();
					}
				}
				else if (e.Key == Key.Left) {
					e.Handled = true;
				
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift)) {
						Selection.SetTarget(MoveState.FindIndex(MoveState.Direction.Left, Selection, str), Selection.Current.Y);
						FocusSelection(3);
					}
					else if (Keyboard.Modifiers == ModifierKeys.Control) {
						int keyIndex = str[Selection.Current.Y].GetPreviousKeyIndex(Selection.Current.X);
						Selection.Set(keyIndex < 0 ? 0 : str[Selection.Current.Y, keyIndex].FrameIndex, 1, Selection.StartLayer, Selection.LayerCount);
						FocusSelection();
					}
					else if (Keyboard.Modifiers == ModifierKeys.Shift) {
						Selection.SetTarget(--Selection.CurrentX, Selection.Current.Y);
						FocusSelection(3);
					}
					else {
						Selection.SetXY(--Selection.CurrentX, Selection.Current.Y);
						FocusSelection();
					}
				}
				else if (e.Key == Key.Up) {
					e.Handled = true;
					
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift)) {
						int lidx = 0;

						for (lidx = Selection.Current.Y - 1; lidx >= 0; lidx--) {
							if (str[lidx].FrameIndex2KeyIndex[Selection.StartFrame] > -1 || lidx == Selection.SelectedLayer)
								break;
						}

						Selection.SetTarget(Selection.Current.X, lidx);
						FocusSelection(3);
					}
					else if (Keyboard.Modifiers == ModifierKeys.Control) {
						int lidx = 0;

						for (lidx = Selection.Current.Y - 1; lidx >= 0; lidx--) {
							if (str[lidx].FrameIndex2KeyIndex[Selection.Current.X] > -1)
								break;
						}

						Selection.SetLayerTarget(lidx, lidx);
						FocusSelection();
					}
					else if (Keyboard.Modifiers == ModifierKeys.Shift) {
						Selection.SetTarget(Selection.Current.X, --Selection.Current.Y);
						FocusSelection(3);
					}
					else {
						Selection.SetXY(Selection.CurrentX, --Selection.Current.Y);
						FocusSelection();
					}
				}
				else if (e.Key == Key.Down) {
					e.Handled = true;

					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift)) {
						int lidx = str.Layers.Count;

						for (lidx = Selection.Current.Y + 1; lidx < str.Layers.Count; lidx++) {
							if (str[lidx].FrameIndex2KeyIndex[Selection.StartFrame] > -1 || lidx == Selection.SelectedLayer)
								break;
						}

						Selection.SetTarget(Selection.Current.X, lidx);
						FocusSelection(3);
					}
					else if (Keyboard.Modifiers == ModifierKeys.Control) {
						int lidx = str.Layers.Count;

						for (lidx = Selection.Current.Y + 1; lidx < str.Layers.Count; lidx++) {
							if (str[lidx].FrameIndex2KeyIndex[Selection.Current.X] > -1)
								break;
						}

						Selection.SetLayerTarget(lidx, lidx);
						FocusSelection();
					}
					else if (Keyboard.Modifiers == ModifierKeys.Shift) {
						Selection.SetTarget(Selection.Current.X, ++Selection.Current.Y);
						FocusSelection(3);
					}
					else {
						Selection.SetXY(Selection.CurrentX, ++Selection.Current.Y);
						FocusSelection();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void FocusTimelineIntoView() {
			try {
				if (_svKeyFrames.ScrollableWidth > 0) {
					var position = _timelinePart.GetSelectorPosition();

					if (position < _svKeyFrames.HorizontalOffset ||
						position >= _svKeyFrames.HorizontalOffset + _svKeyFrames.ActualWidth) {
						_svKeyFrames.ScrollToHorizontalOffset(position);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public int GetLayerIndex(object sender = null) => sender is MenuItem mi && mi.IsVisible ? LayerHeaderController.LastClickedLayer : Selection.SelectedLayer;

		private void _miLayerDuplicate_Click(object sender, RoutedEventArgs e) => LayerHeaderController.DuplicateLayer(GetLayerIndex(sender));
		private void _miLayerCopy_Click(object sender, RoutedEventArgs e) => _layerCopy = GetLayerIndex(sender);
		private void _miLayerPaste_Click(object sender, RoutedEventArgs e) => LayerHeaderController.PasteTextures(_layerCopy, GetLayerIndex(sender));
		private void _miLayerDelete_Click(object sender, RoutedEventArgs e) => LayerHeaderController.DeleteLayer(GetLayerIndex(sender));
		private void _miLayerInsertAbove_Click(object sender, RoutedEventArgs e) => LayerHeaderController.InsertAbove(GetLayerIndex(sender));
		private void _miLayerInsertBelow_Click(object sender, RoutedEventArgs e) => LayerHeaderController.InsertBelow(GetLayerIndex(sender));
		private void _miHideToggle_Click(object sender, RoutedEventArgs e) => LayerHeaderController.ToggleVisibility(GetLayerIndex(sender));
		private void _miHideAllButThis_Click(object sender, RoutedEventArgs e) => LayerHeaderController.HideAllButThis(GetLayerIndex(sender));
		private void _miShowAll_Click(object sender, RoutedEventArgs e) => LayerHeaderController.ShowAll();

		private void _miDelete_Click(object sender, RoutedEventArgs e) => Commands.DeleteKeys(Selection);
		private void _miCopy_Click(object sender, RoutedEventArgs e) => Commands.Copy(Selection);
		private void _miPaste_Click(object sender, RoutedEventArgs e) => Commands.Paste(Selection);
		private void _miDeleteAll_Click(object sender, RoutedEventArgs e) => Commands.DeleteKeysAll(Selection);
		private void _miSelectAll_Click(object sender, RoutedEventArgs e) => Commands.SelectAllKeysInLayer(Selection);
		private void _miInterpolate_Click(object sender, RoutedEventArgs e) => Commands.SetInterpolate(Selection, true);
		private void _miDeleteInterpolate_Click(object sender, RoutedEventArgs e) => Commands.SetInterpolate(Selection, false);
		private void _miNewKey_Click(object sender, RoutedEventArgs e) => Commands.SetNewKey(Selection);
		private void _miNewEndKey_Click(object sender, RoutedEventArgs e) => Commands.SetEndKey(Selection);
		private void _miCopyPrevious_Click(object sender, RoutedEventArgs e) => Commands.CopyPreviousKey(Selection);

		private void _miPasteColor_Click(object sender, RoutedEventArgs e) => Commands.PasteData(PasteDataType.Color);
		private void _miPasteBlend_Click(object sender, RoutedEventArgs e) => Commands.PasteData(PasteDataType.Blend);
		private void _miPasteOffset_Click(object sender, RoutedEventArgs e) => Commands.PasteData(PasteDataType.Offset);
		private void _miPasteAngle_Click(object sender, RoutedEventArgs e) => Commands.PasteData(PasteDataType.Angle);
		private void _miPastePositions_Click(object sender, RoutedEventArgs e) => Commands.PasteData(PasteDataType.Positions);
		private void _miPasteTexture_Click(object sender, RoutedEventArgs e) => Commands.PasteData(PasteDataType.Texture);
		private void _miPasteAnimation_Click(object sender, RoutedEventArgs e) => Commands.PasteData(PasteDataType.Animation);
		private void _miPasteBias_Click(object sender, RoutedEventArgs e) => Commands.PasteData(PasteDataType.Bias);
		private void _miPasteBezier_Click(object sender, RoutedEventArgs e) => Commands.PasteData(PasteDataType.BezierPositions);
	}
}
