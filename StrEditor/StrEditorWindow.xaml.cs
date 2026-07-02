using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using GRF.Image;
using GRF.IO;
using GRF.GrfSystem;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core;
using StrEditor.Core.OpenGLComponents;
using StrEditor.WPF;
using TokeiLibrary;
using TokeiLibrary.Paths;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;
using Utilities.Services;
using Image = System.Drawing.Image;
using Path = System.IO.Path;
using System.Linq;
using StrEditor.Core.Scripting;
using GRF.FileFormats.StrFormat.Commands;
using static GRF.FileFormats.StrFormat.Commands.ScaleFromPivotCommand;
using StrEditor.Services;
using StrEditor.Core.StrConverters;
using static StrEditor.Services.StrSaveService;
using StrEditor.Core.Viewport;
using System.Collections.Generic;

namespace StrEditor {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private readonly WpfRecentFiles _recentFiles;
		private StrLoadService _strLoadService = new StrLoadService();
		private StrSaveService _strSaveService = new StrSaveService();
		public StrController Controller { get; set; }
		public bool IsNew { get; set; }
		public static MainWindow Instance { get; private set; }
		private bool _enableFrameIndexEvents = true;
		public RecentFilesManager RecentFiles => _recentFiles;
		private FrameViewerController _viewportController;

		public delegate void StrEditorEventDelegate(object sender);
		public event StrEditorEventDelegate StrLoaded;
		public void OnStrLoaded() => StrLoaded?.Invoke(this);

		public MainWindow() {
			Instance = this;
			InitializeComponent();

			_recentFiles = new WpfRecentFiles(StrEditorConfiguration.ConfigAsker, 6, _miOpenRecent, "Str");
			_recentFiles.FileClicked += new RecentFilesManager.RFMFileClickedEventHandler(_recentFiles_FileClicked);

			if (!ImageConverterManager.IsSet)
				return;

			// Initialize editor
			_initializeComponents();
			_initializeShortcuts();
			_initializeCommands();

			var snap = StrEditorConfiguration.Snap;
			_cbSnap.SelectedIndex = StrEditorConfiguration.Snap == 0 ? 0 : (int)Math.Round(Math.Log(snap) / Math.Log(2)) + 1;

			// Setup primary event when loading a STR file, which in turn will forward the update to all the components.
			StrLoaded += _strLoaded;

			_loadRecentOrCreateNewStr();
		}

		private void _loadRecentOrCreateNewStr() {
			try {
				if (_recentFiles.Files.Count > 0 && StrEditorConfiguration.ReopenLatestFile) {
					if (File.Exists(_recentFiles.Files[0])) {
						Open(_recentFiles.Files[0]);
					}
					else {
						_miNew_Click(null, null);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _strLoaded(object sender) {
			Controller.Str.Layers.ForEach(p => p.GenerateTexturesHash());
			Controller.Str.Commands.CommandIndexChanged += delegate {
				SetTitle();
			};

			_timelineEditor.Renderer.Reload();
			_keyFrameEditor.Init(Controller.Str);

			Controller.Str.VisualInvalidated += delegate {
				_frameViewer.QuickUpdate();
			};
			Controller.Str.VisualInvalidatedRedraw += delegate {
				_frameViewer.Update();
			};

			_frameViewer.StrRenderer?.Clear();

			_tmbUndo.SetUndo(Controller.Str.Commands);
			_tmbRedo.SetRedo(Controller.Str.Commands);

			Controller.Str.Commands.CommandUndo += delegate {
				_timelineEditor.Selection.Set(_timelineEditor.Selection);
				_keyFrameEditor.InvalidateKeyFrame();
			};

			Controller.Str.Commands.CommandRedo += delegate {
				_timelineEditor.Selection.Set(_timelineEditor.Selection);
				_keyFrameEditor.InvalidateKeyFrame();
			};

			var layers = Controller.Str.Layers;
			_timelineEditor.Selection.SetXY(0, 0);
			_timelineEditor.FocusSelection();
			_timelineEditor.OnPositionChanged();

			SetTitle();
		}

		private void _initializeComponents() {
			// Setup StrController, which will be used by all other components to communicate with one another
			StrController controller = new StrController();
			controller.KeyFrameEditor = _keyFrameEditor;
			controller.TimelineEditor = _timelineEditor;
			controller.FrameViewer = _frameViewer;
			controller.StrEditorWindow = this;
			controller.GifEditControl = _gifSettings;
			controller.PlayAnimation = new PlayAnimation(_play);
			Controller = controller;

			_frameViewer.InitComponent(controller);
			_timelineEditor.InitComponent(controller);
			_keyFrameEditor.InitComponent(controller);
			_gifSettings.InitComponent(controller);
			controller.PlayAnimation.InitComponent(controller);
			_viewportController = new FrameViewerController(controller);
		}

		private void _initializeCommands() {
			var kfeCommands = Controller.TimelineEditor.Commands;
			var fvCommands = Controller.FrameViewer.Commands;

			_miAddBezier.Click += (sender, args) => kfeCommands.CreateBezier();
			_miDelBezier.Click += (sender, args) => kfeCommands.DeleteBezier();
			_miCenterOrigin.Click += (sender, args) => kfeCommands.CenterOrigin();
			_miScaleCenterKeyFrame.Click += (sender, args) => kfeCommands.Scale(ScaleFromPivotCommand.ScaleMode.KeyFrame, PivotMode.Center);
			_miScaleCenterLayer.Click += (sender, args) => kfeCommands.Scale(ScaleFromPivotCommand.ScaleMode.Layer, PivotMode.Center);
			_miScaleCenterStr.Click += (sender, args) => kfeCommands.Scale(ScaleFromPivotCommand.ScaleMode.Str, PivotMode.Center);
			_miScaleOriginKeyFrame.Click += (sender, args) => kfeCommands.Scale(ScaleFromPivotCommand.ScaleMode.KeyFrame, PivotMode.Origin);
			_miScaleOriginLayer.Click += (sender, args) => kfeCommands.Scale(ScaleFromPivotCommand.ScaleMode.Layer, PivotMode.Origin);
			_miScaleOriginStr.Click += (sender, args) => kfeCommands.Scale(ScaleFromPivotCommand.ScaleMode.Str, PivotMode.Origin);
			_miScaleWorldKeyFrame.Click += (sender, args) => kfeCommands.Scale(ScaleFromPivotCommand.ScaleMode.KeyFrame, PivotMode.Defined);
			_miScaleWorldLayer.Click += (sender, args) => kfeCommands.Scale(ScaleFromPivotCommand.ScaleMode.Layer, PivotMode.Defined);
			_miScaleWorldStr.Click += (sender, args) => kfeCommands.Scale(ScaleFromPivotCommand.ScaleMode.Str, PivotMode.Defined);
			_miCopy.Click += (sender, args) => kfeCommands.Copy();
			_miPaste.Click += (sender, args) => kfeCommands.Paste();
			_miFlipH.Click += (sender, args) => fvCommands.FlipH();
			_miFlipV.Click += (sender, args) => fvCommands.FlipV();
			_miFlipH2.Click += (sender, args) => fvCommands.FlipHSelf();
			_miFlipV2.Click += (sender, args) => fvCommands.FlipVSelf();
			_miFlipHTexture.Click += (sender, args) => fvCommands.FlipHTexture();
			_miFlipVTexture.Click += (sender, args) => fvCommands.FlipVTexture();
			_miMergeStr.Click += _miMergeStr_Click;

			_imgResetBackground.MouseEnter += (s, e) => Mouse.OverrideCursor = Cursors.Hand;
			_imgResetBackground.MouseLeave += (s, e) => Mouse.OverrideCursor = null;
			_imgResetBackground.PreviewMouseDown += (s, e) => {
				e.Handled = true;
				_frameViewer.ResetBackground();
			};
			_imgResetBackground.PreviewMouseUp += (s, e) => {
				e.Handled = true;
				_frameViewer.ResetBackground();
			};
			_miSelectBackground.Click += _miSelectBackground_Click;

			Binder.Bind(_buttonPathing, () => StrEditorConfiguration.ShowPathing, v => StrEditorConfiguration.ShowPathing = v, () => Controller.FrameViewer.QuickUpdate());
			Binder.Bind(_buttonBezier, () => StrEditorConfiguration.ShowBezier, v => StrEditorConfiguration.ShowBezier = v, () => Controller.FrameViewer.QuickUpdate());
			Binder.Bind(_buttonScale, () => StrEditorConfiguration.DrawTranslationPoints, v => StrEditorConfiguration.DrawTranslationPoints = v, () => Controller.FrameViewer.QuickUpdate());
			Binder.Bind(_buttonGroupEdit, () => StrEditorConfiguration.GroupEdit, v => StrEditorConfiguration.GroupEdit = v, () => Controller.FrameViewer.QuickUpdate());
			Binder.Bind(_miDrawReferenceSprite, () => StrEditorConfiguration.DrawReferenceSprite, v => StrEditorConfiguration.DrawReferenceSprite = v, () => Controller.FrameViewer.QuickUpdate());

			_miDrawPriority.SubmenuOpened += delegate {
				_miDrawReferenceSpriteBack.IsChecked = !StrEditorConfiguration.DrawReferenceSpritePriority;
				_miDrawReferenceSpriteFront.IsChecked = StrEditorConfiguration.DrawReferenceSpritePriority;
			};
			_miDrawReferenceSpriteBack.Click += delegate {
				StrEditorConfiguration.DrawReferenceSpritePriority = !StrEditorConfiguration.DrawReferenceSpritePriority;
				Controller.FrameViewer.QuickUpdate();
			};
			_miDrawReferenceSpriteFront.Click += delegate {
				StrEditorConfiguration.DrawReferenceSpritePriority = !StrEditorConfiguration.DrawReferenceSpritePriority;
				Controller.FrameViewer.QuickUpdate();
			};
		}

		private void _initializeShortcuts() {
			ApplicationShortcut.Link(StrEditorCommands.Undo, () => Controller.Str?.Commands.Undo(), this);
			ApplicationShortcut.Link(StrEditorCommands.Redo, () => Controller.Str?.Commands.Redo(), this);
			ApplicationShortcut.Link(StrEditorCommands.Open, _miOpen, this);
			ApplicationShortcut.Link(StrEditorCommands.SaveAsGif, _miSaveGif, this);
			ApplicationShortcut.Link(StrEditorCommands.Save, _miSave, this);
			ApplicationShortcut.Link(StrEditorCommands.SaveAs, _miSaveAs, this);
			ApplicationShortcut.Link(StrEditorCommands.ExportAsPng, _miExportPng, this);
			ApplicationShortcut.Link(StrEditorCommands.Copy, _miCopy, this);
			ApplicationShortcut.Link(StrEditorCommands.Paste, _miPaste, this);
			ApplicationShortcut.Link(StrEditorCommands.Cut, _miCut, this);
			ApplicationShortcut.Link(StrEditorCommands.AddBezierCurve, _miAddBezier, this);
			ApplicationShortcut.Link(StrEditorCommands.RemoveBezierCurve, _miDelBezier, this);
			ApplicationShortcut.Link(StrEditorCommands.CenterOrigin, _miCenterOrigin, this);
			ApplicationShortcut.Link(StrEditorCommands.ScaleCenterKeyFrame, _miScaleCenterKeyFrame, this);
			ApplicationShortcut.Link(StrEditorCommands.ScaleCenterLayer, _miScaleCenterLayer, this);
			ApplicationShortcut.Link(StrEditorCommands.ScaleCenterStr, _miScaleCenterStr, this);
			ApplicationShortcut.Link(StrEditorCommands.ScaleOriginKeyFrame, _miScaleOriginKeyFrame, this);
			ApplicationShortcut.Link(StrEditorCommands.ScaleOriginLayer, _miScaleOriginLayer, this);
			ApplicationShortcut.Link(StrEditorCommands.ScaleOriginStr, _miScaleOriginStr, this);
			ApplicationShortcut.Link(StrEditorCommands.ScaleWorldKeyFrame, _miScaleWorldKeyFrame, this);
			ApplicationShortcut.Link(StrEditorCommands.ScaleWorldLayer, _miScaleWorldLayer, this);
			ApplicationShortcut.Link(StrEditorCommands.ScaleWorldStr, _miScaleWorldStr, this);
			ApplicationShortcut.Link(StrEditorCommands.FlipHorizontal, _miFlipH, this);
			ApplicationShortcut.Link(StrEditorCommands.FlipVertical, _miFlipV, this);
			ApplicationShortcut.Link(StrEditorCommands.FlipTextureHorizontal, _miFlipHTexture, this);
			ApplicationShortcut.Link(StrEditorCommands.FlipTextureVertical, _miFlipVTexture, this);
			ApplicationShortcut.Link(StrEditorCommands.FlipHorizontal2, _miFlipH2, this);
			ApplicationShortcut.Link(StrEditorCommands.FlipVertical2, _miFlipV2, this);
			ApplicationShortcut.Link(StrEditorCommands.Merge, _miMergeStr, this);

			//ApplicationShortcut.Link(StrEditorCommands.ViewportTranslateLeft, () => Controller.KeyFrameEditor.TranslateOffset(-1, 0), this);
			//ApplicationShortcut.Link(StrEditorCommands.ViewportTranslateRight, () => Controller.KeyFrameEditor.TranslateOffset(1, 0), this);
			//ApplicationShortcut.Link(StrEditorCommands.ViewportTranslateUp, () => Controller.KeyFrameEditor.TranslateOffset(0, -1), this);
			//ApplicationShortcut.Link(StrEditorCommands.ViewportTranslateDown, () => Controller.KeyFrameEditor.TranslateOffset(0, 1), this);
		}

		public void Open(TkPath file, bool isNew = false) {
			try {
				if (!CloseStr(Controller.Str))
					return;

				var result = _strLoadService.Load(file);

				if (result.AddToRecentFiles)
					RecentFiles.AddRecentFile(result.FilePath);
				if (result.RemoveToRecentFiles)
					RecentFiles.RemoveRecentFile(result.FilePath);
				if (result.ErrorMessage != null)
					ErrorHandler.HandleException(result.ErrorMessage);
				if (!result.Success)
					return;

				if (result.IsNew)
					IsNew = true;
				else
					IsNew = isNew;

				Controller.Str = result.LoadedStr;
				OnStrLoaded();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private bool Save() {
			try {
				if (Controller.Str == null)
					return false;

				if (IsNew)
					return SaveAs();

				var result = _strSaveService.Save(Controller.Str);

				if (result.IsNewCleared)
					IsNew = false;
				if (result.SaveCommandIndex)
					Controller.Str.Commands.SaveCommandIndex();

				SetTitle();
				return true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			return false;
		}

		public bool SaveAs() {
			try {
				var context = _strSaveService.CreateSaveContext(Controller.Str);
				if (context == null) return false;

				var result = _strSaveService.ExecuteSave(context);

				if (result.IsNewCleared)
					IsNew = false;
				if (result.AddToRecentFiles)
					RecentFiles.AddRecentFile(result.NewFilePath);
				if (result.SaveCommandIndex)
					Controller.Str.Commands.SaveCommandIndex();

				return true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			return false;
		}

		public bool CloseStr(Str str) {
			if (str != null && str.Commands.IsModified) {
				var res = WindowProvider.ShowDialog("The STR has been modified, would you like to save it first?\n\n" + str.LoadedPath, "Modified Str - " + Path.GetFileNameWithoutExtension(str.LoadedPath), MessageBoxButton.YesNoCancel);

				if (res == MessageBoxResult.Yes) {
					if (!Save()) {
						return false;
					}
				}

				if (res == MessageBoxResult.Cancel) {
					return false;
				}
			}

			if (str != null) {
				str.Commands.ClearCommands();
			}

			ResourceManager.TemporaryResources.Clear();
			return true;
		}

		public void SetTitle() {
			var str = Controller.Str;
			string name = Path.GetFileName(str.LoadedPath);

			this.Dispatch(delegate {
				if (!str.Commands.IsModified && !IsNew) {
					Title = name;
				}
				if (!str.Commands.IsModified && IsNew) {
					Title = name + " *";
				}
				else if (str.Commands.IsModified || IsNew) {
					Title = name + " *";
				}
			});
		}

		private void _miMergeStr_Click(object sender, RoutedEventArgs e) {
			string path = TkPathRequest.OpenFile(new Setting(null, typeof(StrEditorConfiguration).GetProperty("AppLastPath")), "filter", FileFormat.MergeFilters(FileFormat.Str));
			_viewportController.MergeStr(path);
		}

		private void _miSelectBackground_Click(object sender, RoutedEventArgs e) {
			string path = TkPathRequest.OpenFile(new Setting(null, typeof(StrEditorConfiguration).GetProperty("BackgroundPath")), "filter", FileFormat.MergeFilters(FileFormat.Image));

			if (path != null) {
				_frameViewer.LoadBackground(path);
			}
		}

		private void _timelineEditor_TimelineFrameIndexChanged() {
			if (Controller.Str == null)
				return;

			if (!_enableFrameIndexEvents)
				return;

			_frameViewer.QuickUpdate();
		}

		private void _timelineEditor_PositionChanged() {
			if (Controller.Str == null)
				return;

			try {
				if (_timelineEditor.SelectedLayerIndex < 0)
					return;

				_enableFrameIndexEvents = false;
				_timelineEditor.TimelineFrameIndex = _timelineEditor.SelectedFrameIndex;

				var inter = InterpolatedKeyFrame.Interpolate(Controller.Str, _timelineEditor.SelectedLayerIndex, _timelineEditor.SelectedFrameIndex);

				_keyFrameEditor.AsyncUpdate(inter);
				_frameViewer.Update();
			}
			finally {
				_enableFrameIndexEvents = true;
			}
		}

		private void _recentFiles_FileClicked(string file) => Open(file);

		private void _miOpen_Click(object sender, RoutedEventArgs e) {
			try {
				string file = PathRequest.OpenFileExtract("filter", FileFormat.MergeFilters(new FileFormat(".str;.ezv;.gif", "All Animation"), FileFormat.Str, FileFormat.Ezv, FileFormat.Gif));

				if (file != null) {
					Open(file);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miSave_Click(object sender, RoutedEventArgs e) => Save();

		private void _miSaveGif_Click(object sender, RoutedEventArgs e) {
			Controller.GifData.IsGifMode = true;
			_gifSettings.Show();
		}

		private void _miExportPng_Click(object sender, RoutedEventArgs e) {
			Controller.GifData.IsPngMode = true;
			_gifSettings.Show();
		}

		private void _miSaveAs_Click(object sender, RoutedEventArgs e) => SaveAs();

		private void _miSelectStr_Click(object sender, RoutedEventArgs e) {
			try {
				TkPath path = new TkPath(Controller.Str.LoadedPath);
				OpeningService.FilesOrFolders(path.FilePath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miSettings_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new SettingsDialog(this), _miSettings, this);
		}

		private void _miAbout_Click(object sender, RoutedEventArgs e) {
			var dialog = new AboutDialog(StrEditorConfiguration.PublicVersion, StrEditorConfiguration.RealVersion, StrEditorConfiguration.Author, StrEditorConfiguration.ProgramName);
			dialog.Owner = WpfUtilities.TopWindow;
			dialog.ShowDialog();
		}
		private void _miClose_Click(object sender, RoutedEventArgs e) => Close();

		private void _miNew_Click(object sender, RoutedEventArgs e) {
			try {
				if (!CloseStr(Controller.Str))
					return;

				Controller.Str = new Str();
				IsNew = true;

				for (int i = 0; i < 3; i++) {
					StrLayer layer = new StrLayer(Controller.Str);
					Controller.Str.Layers.Add(layer);
				}

				Controller.Str.MaxKeyFrame = 120;

				OnStrLoaded();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miScriptRunner_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.Show(new ScriptRunnerDialog(this), _miScriptRunner, this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected override void OnClosing(CancelEventArgs e) {
			try {
				if (!CloseStr(Controller.Str)) {
					e.Cancel = true;
					return;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			base.OnClosing(e);
			ApplicationManager.Shutdown();
		}

		private void _wfh_PreviewDragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _wfh_PreviewDrop(object sender, DragEventArgs e) {
			try {
				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

					if (files != null && files.Length > 0) {
						string path = files[0];

						if (path.IsExtension(".str", ".ezv", ".gif")) {
							Open(path);
						}
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _cbSnap_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				switch (_cbSnap.SelectedIndex) {
					case 0:
						StrEditorConfiguration.Snap = 0;
						break;
					default:
						StrEditorConfiguration.Snap = Int32.Parse(((ComboBoxItem)_cbSnap.SelectedItem).Content.ToString());
						break;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
