using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.Core.GroupedGrf;
using GRF.Image;
using GRF.IO;
using GRF.Threading;
using GrfToWpfBridge;
using OpenTK;
using TokeiLibrary;
using Utilities;
using Configuration = TokeiLibrary.Configuration;

namespace StrEditor.ApplicationConfiguration {
	/// <summary>
	/// Contains all the configuration information
	/// The ConfigAsker shouldn't be used manually to store variable,
	/// make a new property instead. The properties should also always
	/// have a default value.
	/// </summary>
	public static class StrEditorConfiguration {
		private static ConfigAsker _configAsker;

		public static ConfigAsker ConfigAsker {
			get => _configAsker ?? (_configAsker = new ConfigAsker(GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName, "config.txt")));
			set => _configAsker = value;
		}

		private static readonly BufferedProperty<string> _backgroundPath = new BufferedProperty<string>(ConfigAsker, "[StrEditor - BackgroundPath]", Configuration.ApplicationPath, FormatConverters.StringConverter);

		public static string BackgroundPath {
			get => _backgroundPath.Get();
			set => _backgroundPath.Set(value);
		}

		private static readonly BufferedProperty<bool> _showPathing = new BufferedProperty<bool>(ConfigAsker, "[StrEditor - ShowPathing]", false, FormatConverters.BooleanConverter);

		public static bool ShowPathing {
			get => _showPathing.Get();
			set => _showPathing.Set(value);
		}

		private static readonly BufferedProperty<bool> _showBezier = new BufferedProperty<bool>(ConfigAsker, "[StrEditor - ShowBezier]", false, FormatConverters.BooleanConverter);

		public static bool ShowBezier {
			get => _showBezier.Get();
			set => _showBezier.Set(value);
		}

		private static readonly BufferedProperty<bool> _showScale = new BufferedProperty<bool>(ConfigAsker, "[StrEditor - ShowScale]", true, FormatConverters.BooleanConverter);

		public static bool ShowScale {
			get => _showScale.Get();
			set => _showScale.Set(value);
		}

		private static readonly BufferedProperty<bool> _drawTranslationPoints = new BufferedProperty<bool>(ConfigAsker, "[StrEditor - DrawTranslationPoints]", true, FormatConverters.BooleanConverter);

		public static bool DrawTranslationPoints {
			get => _drawTranslationPoints.Get();
			set => _drawTranslationPoints.Set(value);
		}

		private static readonly BufferedProperty<bool> _groupEdit = new BufferedProperty<bool>(ConfigAsker, "[StrEditor - Group Edit]", false, FormatConverters.BooleanConverter);

		public static bool GroupEdit {
			get => _groupEdit.Get();
			set => _groupEdit.Set(value);
		}

		public static bool AttemptReconstrustBias {
			get => Boolean.Parse(ConfigAsker["[StrEditor - AttemptReconstrustBias]", true.ToString()]);
			set => ConfigAsker["[StrEditor - AttemptReconstrustBias]"] = value.ToString();
		}

		public static bool BezierAdjustNodes {
			get => Boolean.Parse(ConfigAsker["[StrEditor - BezierAdjustNodes]", true.ToString()]);
			set => ConfigAsker["[StrEditor - BezierAdjustNodes]"] = value.ToString();
		}

		public static bool KeepTrackKeyFrameEditorCursor {
			get => Boolean.Parse(ConfigAsker["[StrEditor - KeepTrackKeyFrameEditorCursor]", true.ToString()]);
			set => ConfigAsker["[StrEditor - KeepTrackKeyFrameEditorCursor]"] = value.ToString();
		}

		public static bool MustSelectKeyFrameBeforeDragging {
			get => Boolean.Parse(ConfigAsker["[StrEditor - MustSelectKeyFrameBeforeDragging]", false.ToString()]);
			set => ConfigAsker["[StrEditor - MustSelectKeyFrameBeforeDragging]"] = value.ToString();
		}

		public static bool IronPythonAutocomplete {
			get => Boolean.Parse(ConfigAsker["[StrEditor - IronPython - Autocomplete]", true.ToString()]);
			set => ConfigAsker["[StrEditor - IronPython - Autocomplete]"] = value.ToString();
		}

		private static readonly BufferedProperty<bool> _drawReferenceSprite = new BufferedProperty<bool>(ConfigAsker, "[StrEditor - DrawReferenceSprite]", false, FormatConverters.BooleanConverter);

		public static bool DrawReferenceSprite {
			get => _drawReferenceSprite.Get();
			set => _drawReferenceSprite.Set(value);
		}

		#region Generic settings

		public static int EncodingCodepage {
			get => Int32.Parse(ConfigAsker["[ActEditor - Encoding codepage]", "1252"]);
			set => ConfigAsker["[ActEditor - Encoding codepage]"] = value.ToString(CultureInfo.InvariantCulture);
		}

		public static string ProgramDataPath => GrfPath.Combine(Configuration.ApplicationDataPath, ProgramName);

		public static ErrorLevel WarningLevel {
			get => (ErrorLevel)Int32.Parse(ConfigAsker["[ActEditor - Warning level]", "0"]);
			set => ConfigAsker["[ActEditor - Warning level]"] = ((int)value).ToString(CultureInfo.InvariantCulture);
		}

		#region Program's internal configuration and information

		public static string PublicVersion => "1.1.2";
		public static string Author => "Tokeiburu";
		public static string ProgramName => "Str Editor";
		public static string RealVersion => Assembly.GetEntryAssembly().GetName().Version.ToString();

		public static int PatchId {
			get => Int32.Parse(ConfigAsker["[ActEditor - Patch ID]", "0"]);
			set => ConfigAsker["[ActEditor - Patch ID]"] = value.ToString(CultureInfo.InvariantCulture);
		}

		#endregion

		/// <summary>
		/// Gets or sets the extracting service last path.
		/// This setting name cannot be changed due to reflection.
		/// This is used by the PathRequest class.
		/// </summary>
		public static string GifSavePath {
			get => ConfigAsker["[StrEditor - GifSavePath]", Configuration.ApplicationPath];
			set => ConfigAsker["[StrEditor - GifSavePath]"] = value;
		}

		public static string ExtractingServiceLastPath {
			get => ConfigAsker["[ActEditor - ExtractingService - Latest directory]", Configuration.ApplicationPath];
			set => ConfigAsker["[ActEditor - ExtractingService - Latest directory]"] = value;
		}

		public static string SaveAdvancedLastPath {
			get => ConfigAsker["[ActEditor - Save advanced path]", ExtractingServiceLastPath];
			set => ConfigAsker["[ActEditor - Save advanced path]"] = value;
		}

		public static string AppLastPath {
			get => ConfigAsker["[ActEditor - Application latest file name]", Configuration.ApplicationPath];
			set => ConfigAsker["[ActEditor - Application latest file name]"] = value;
		}

		public static string AppLastStrFolder {
			get => ConfigAsker["[ActEditor - Application latest str file]", AppLastPath];
			set => ConfigAsker["[ActEditor - Application latest str file]"] = value;
		}

		public static string AppLastGrfPath {
			get => ConfigAsker["[ActEditor - Application latest grf file name]", Configuration.ApplicationPath];
			set => ConfigAsker["[ActEditor - Application latest grf file name]"] = value;
		}

		#endregion

		#region Others

		/// <summary>
		/// Xaml binding property for the background; this is
		/// to avoid crashes in the designer.
		/// </summary>
		public static Brush UIPanelPreviewBackground => new SolidColorBrush(StrEditorBackgroundColor);
		#endregion

		#region Editor settings

		public static string ActEditorScriptRunnerScript {
			get => ConfigAsker["[ActEditor - Script Runner - Latest script]", "// Script example, for a complete list of available methods,__%LineBreak%// click on the 'Help' button__%LineBreak%foreach (var selectedLayerIndex in selectedLayerIndexes) {__%LineBreak%	var layer = act[selectedActionIndex, selectedFrameIndex, selectedLayerIndex];__%LineBreak%	layer.Translate(-10, 0);__%LineBreak%	layer.Rotate(15);__%LineBreak%}__%LineBreak%__%LineBreak%foreach (var action in act) {__%LineBreak%	foreach (var frame in action) {__%LineBreak%		foreach (var layer in frame) {__%LineBreak%			layer.OffsetX = 2 * layer.OffsetX;__%LineBreak%			layer.ScaleX *= 2f;__%LineBreak%			layer.Scale(1f, 2f);__%LineBreak%		}__%LineBreak%	}__%LineBreak%}__%LineBreak%"];
			set => ConfigAsker["[ActEditor - Script Runner - Latest script]"] = value;
		}

		public static bool ReopenLatestFile {
			get => Boolean.Parse(ConfigAsker["[ActEditor - Open latest file on startup]", true.ToString()]);
			set => ConfigAsker["[ActEditor - Open latest file on startup]"] = value.ToString();
		}

		public static bool InterpolateNewKey {
			get => Boolean.Parse(ConfigAsker["[ActEditor - InterpolateNewKey2]", false.ToString()]);
			set => ConfigAsker["[ActEditor - InterpolateNewKey2]"] = value.ToString();
		}

		public static bool UseCascadeForGifs {
			get => Boolean.Parse(ConfigAsker["[ActEditor - UseCascadeForGifs]", false.ToString()]);
			set => ConfigAsker["[ActEditor - UseCascadeForGifs]"] = value.ToString();
		}

		public static bool AllowSkipGifFrames {
			get => Boolean.Parse(ConfigAsker["[ActEditor - AllowSkipGifFrames]", true.ToString()]);
			set => ConfigAsker["[ActEditor - AllowSkipGifFrames]"] = value.ToString();
		}

		public static bool ShowNonInterpolated {
			get => Boolean.Parse(ConfigAsker["[ActEditor - ShowNonInterpolated]", true.ToString()]);
			set => ConfigAsker["[ActEditor - ShowNonInterpolated]"] = value.ToString();
		}

		public static bool AlwaysSaveTexturesWithStr {
			get => Boolean.Parse(ConfigAsker["[ActEditor - AlwaysSaveTexturesWithStr]", true.ToString()]);
			set => ConfigAsker["[ActEditor - AlwaysSaveTexturesWithStr]"] = value.ToString();
		}

		public static bool ShowUvCoords {
			get => Boolean.Parse(ConfigAsker["[ActEditor - Show UV coords]", false.ToString()]);
			set => ConfigAsker["[ActEditor - Show UV coords]"] = value.ToString();
		}

		public static bool ShellAssociateAct {
			get => Boolean.Parse(ConfigAsker["[Application - Shell associate - Act]", false.ToString()]);
			set => ConfigAsker["[Application - Shell associate - Act]"] = value.ToString();
		}

		public static QuickColorSetting StrEditorBackgroundColorQuick = new QuickColorSetting(ConfigAsker.RetrieveSetting(() => StrEditorBackgroundColor));

		public static Color StrEditorBackgroundColor {
			get => new GrfColor((ConfigAsker["[StrEditor - Background preview color]", GrfColor.ToHex(150, 0, 0, 0)])).ToColor();
			set => ConfigAsker["[StrEditor - Background preview color]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		private static GrfColor? _color0;

		public static GrfColor StrEditorGridLineHorizontalQuick {
			get {
				if (_color0 == null)
					_color0 = StrEditorGridLineHorizontal;

				return _color0.Value;
			}
			set {
				_color0 = value;
				ConfigAsker["[ActEditor - Grid line horizontal color]"] = value.ToHexString();
			}
		}

		public static GrfColor StrEditorGridLineHorizontal {
			get => new GrfColor((ConfigAsker["[ActEditor - Grid line horizontal color]", GrfColor.ToHex(255, 0, 0, 0)]));
			set => ConfigAsker["[ActEditor - Grid line horizontal color]"] = value.ToHexString();
		}

		private static GrfColor? _color1;

		public static GrfColor StrEditorGridLineVerticalQuick {
			get {
				if (_color1 == null)
					_color1 = StrEditorGridLineVertical;

				return _color1.Value;
			}
			set {
				_color1 = value;
				ConfigAsker["[ActEditor - Grid line vertical color]"] = value.ToHexString();
			}
		}

		public static GrfColor StrEditorGridLineVertical {
			get => new GrfColor((ConfigAsker["[ActEditor - Grid line vertical color]", GrfColor.ToHex(255, 0, 0, 0)]));
			set => ConfigAsker["[ActEditor - Grid line vertical color]"] = value.ToHexString();
		}

		private static GrfColor? _color3;

		public static GrfColor StrEditorSpriteSelectionBorderQuick {
			get {
				if (_color3 == null)
					_color3 = StrEditorSpriteSelectionBorder;

				return _color3.Value;
			}
			set {
				_color3 = value;
				ConfigAsker["[ActEditor - Selected sprite border color]"] = value.ToHexString();
			}
		}

		public static GrfColor StrEditorSpriteSelectionBorder {
			get => new GrfColor((ConfigAsker["[ActEditor - Selected sprite border color]", GrfColor.ToHex(255, 255, 0, 0)]));
			set => ConfigAsker["[ActEditor - Selected sprite border color]"] = value.ToHexString();
		}

		private static Vector4? _color4;

		public static Vector4 StrEditorVertexSelectedColorQuick {
			get {
				if (_color4 == null)
					_color4 = new Vector4(StrEditorVertexSelectedColor.R / 255f, StrEditorVertexSelectedColor.G / 255f, StrEditorVertexSelectedColor.B / 255f, StrEditorVertexSelectedColor.A / 255f);

				return _color4.Value;
			}
			set {
				_color4 = value;
				ConfigAsker["[ActEditor - Selected square color]"] = new GrfColor((byte)(255 * value[0]), (byte)(255 * value[1]), (byte)(255 * value[2]), (byte)(255 * value[3])).ToHexString();
			}
		}

		public static Color StrEditorVertexSelectedColor {
			get => new GrfColor((ConfigAsker["[ActEditor - Selected square color]", GrfColor.ToHex(255, 255, 0, 0)])).ToColor();
			set => ConfigAsker["[ActEditor - Selected square color]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		private static Vector4? _color5;

		public static Vector4 StrEditorVertexColorQuick {
			get {
				if (_color5 == null)
					_color5 = new Vector4(StrEditorVertexColor.R / 255f, StrEditorVertexColor.G / 255f, StrEditorVertexColor.B / 255f, StrEditorVertexColor.A / 255f);

				return _color5.Value;
			}
			set {
				_color5 = value;
				ConfigAsker["[ActEditor - Square color]"] = new GrfColor((byte)(255 * value[0]), (byte)(255 * value[1]), (byte)(255 * value[2]), (byte)(255 * value[3])).ToHexString();
			}
		}

		public static Color StrEditorVertexColor {
			get => new GrfColor((ConfigAsker["[ActEditor - Square color]", GrfColor.ToHex(255, 255, 255, 255)])).ToColor();
			set => ConfigAsker["[ActEditor - Square color]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public class QuickColorSetting {
			private readonly ConfigAskerSetting _setting;
			private Vector4? _color;

			public QuickColorSetting(ConfigAskerSetting setting) {
				_setting = setting;
			}

			public void Set(Vector4 color) {
				_color = color;
				string old = _setting.Get();
				_setting.Set(new GrfColor((byte)(255 * color[3]), (byte)(255 * color[0]), (byte)(255 * color[1]), (byte)(255 * color[2])).ToHexString());
				string new_ = _setting.Get();
				_setting.OnPreviewPropertyChanged(old, new_);
			}

			public Vector4 Get() {
				if (_color == null) {
					var color = new GrfColor(_setting.Get()).ToColor();
					_color = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
				}

				return _color.Value;
			}

			public Vector4 Color => Get();
		}

		public class QuickSolidColorBrushSetting {
			private readonly ConfigAskerSetting _setting;
			private SolidColorBrush _color;

			public QuickSolidColorBrushSetting(ConfigAskerSetting setting) {
				_setting = setting;
			}

			public void Set(Color color) {
				_color = new SolidColorBrush(color);
				string old = _setting.Get();
				_setting.Set(new GrfColor(color.A, color.R, color.G, color.B).ToHexString());
				string new_ = _setting.Get();
				_setting.OnPreviewPropertyChanged(old, new_);
			}

			public SolidColorBrush Get() {
				if (_color == null) {
					var color = new GrfColor(_setting.Get()).ToColor();
					_color = new SolidColorBrush(color);
				}

				return _color;
			}

			public SolidColorBrush Color => Get();
		}

		public static QuickSolidColorBrushSetting LayerEditorEaseColorQuick = new QuickSolidColorBrushSetting(ConfigAsker.RetrieveSetting(() => LayerEditorEaseColor));

		public static Color LayerEditorEaseColor {
			get => new GrfColor((ConfigAsker["[StrEditor - LayerEditorEaseColor]", GrfColor.ToHex(204, 255, 210, 98)])).ToColor();
			set => ConfigAsker["[StrEditor - LayerEditorEaseColor]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickSolidColorBrushSetting LayerEditorAnimationColorQuick = new QuickSolidColorBrushSetting(ConfigAsker.RetrieveSetting(() => LayerEditorAnimationColor));

		public static Color LayerEditorAnimationColor {
			get => new GrfColor((ConfigAsker["[StrEditor - LayerEditorAnimationColor]", GrfColor.ToHex(204, 154, 213, 154)])).ToColor();
			set => ConfigAsker["[StrEditor - LayerEditorAnimationColor]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickSolidColorBrushSetting LayerEditorErrorColorQuick = new QuickSolidColorBrushSetting(ConfigAsker.RetrieveSetting(() => LayerEditorErrorColor));

		public static Color LayerEditorErrorColor {
			get => new GrfColor((ConfigAsker["[StrEditor - LayerEditorErrorColor]", GrfColor.ToHex(204, 220, 127, 127)])).ToColor();
			set => ConfigAsker["[StrEditor - LayerEditorErrorColor]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickSolidColorBrushSetting LayerEditorBezierColorQuick = new QuickSolidColorBrushSetting(ConfigAsker.RetrieveSetting(() => LayerEditorBezierColor));

		public static Color LayerEditorBezierColor {
			get => new GrfColor((ConfigAsker["[StrEditor - LayerEditorBezierColor]", GrfColor.ToHex(127, 196, 127, 220)])).ToColor();
			set => ConfigAsker["[StrEditor - LayerEditorBezierColor]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickColorSetting StrEditorPathLineColorQuick = new QuickColorSetting(ConfigAsker.RetrieveSetting(() => StrEditorPathLineColor));

		public static Color StrEditorPathLineColor {
			get => new GrfColor((ConfigAsker["[StrEditor - Path line color]", GrfColor.ToHex(255, 0, 0, 0)])).ToColor();
			set => ConfigAsker["[StrEditor - Path line color]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickColorSetting StrEditorSelectNodeColorQuick = new QuickColorSetting(ConfigAsker.RetrieveSetting(() => StrEditorSelectNodeColor));

		public static Color StrEditorSelectNodeColor {
			get => new GrfColor((ConfigAsker["[StrEditor - StrEditorSelectNodeColor]", GrfColor.ToHex(255, 255, 127, 127)])).ToColor();
			set => ConfigAsker["[StrEditor - StrEditorSelectNodeColor]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickColorSetting StrEditorPathNodeCurrentColorQuick = new QuickColorSetting(ConfigAsker.RetrieveSetting(() => StrEditorPathNodeCurrentColor));

		public static Color StrEditorPathNodeCurrentColor {
			get => new GrfColor((ConfigAsker["[StrEditor - Path current node color]", GrfColor.ToHex(255, 0, 255, 255)])).ToColor();
			set => ConfigAsker["[StrEditor - Path current node color]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickColorSetting StrEditorPathNodeColorQuick = new QuickColorSetting(ConfigAsker.RetrieveSetting(() => StrEditorPathNodeColor));

		public static Color StrEditorPathNodeColor {
			get => new GrfColor((ConfigAsker["[StrEditor - Path node color]", GrfColor.ToHex(255, 180, 180, 180)])).ToColor();
			set => ConfigAsker["[StrEditor - Path node color]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickColorSetting BezierNode1Quick = new QuickColorSetting(ConfigAsker.RetrieveSetting(() => BezierNode1));

		public static Color BezierNode1 {
			get => new GrfColor((ConfigAsker["[StrEditor - BezierNode1]", GrfColor.ToHex(255, 255, 0, 0)])).ToColor();
			set => ConfigAsker["[StrEditor - BezierNode1]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickColorSetting GifBackgroundQuick = new QuickColorSetting(ConfigAsker.RetrieveSetting(() => GifBackground));

		public static Color GifBackground {
			get => new GrfColor((ConfigAsker["[StrEditor - GifBackground]", GrfColor.ToHex(255, 0, 0, 0)])).ToColor();
			set => ConfigAsker["[StrEditor - GifBackground]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickColorSetting BezierNode2Quick = new QuickColorSetting(ConfigAsker.RetrieveSetting(() => BezierNode2));

		public static Color BezierNode2 {
			get => new GrfColor((ConfigAsker["[StrEditor - BezierNode2]", GrfColor.ToHex(255, 0, 0, 255)])).ToColor();
			set => ConfigAsker["[StrEditor - BezierNode2]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		public static QuickColorSetting BezierLineQuick = new QuickColorSetting(ConfigAsker.RetrieveSetting(() => BezierLine));

		public static Color BezierLine {
			get => new GrfColor((ConfigAsker["[StrEditor - BezierLine]", GrfColor.ToHex(255, 0, 0, 0)])).ToColor();
			set => ConfigAsker["[StrEditor - BezierLine]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B);
		}

		private static readonly BufferedProperty<float> _zoomMultiplier = new BufferedProperty<float>(ConfigAsker, "[ActEditor - Zoom in multiplier]", 1, FormatConverters.SingleConverter);

		public static float StrEditorZoomInMultiplier {
			get => _zoomMultiplier.Get();
			set => _zoomMultiplier.Set(value);
		}

		public static double KeyFrameHeight {
			get => double.Parse(ConfigAsker["[StrEditor - KeyFrameHeight]", "30"]);
			set => ConfigAsker["[StrEditor - KeyFrameHeight]"] = value.ToString(CultureInfo.InvariantCulture);
		}

		private static ObservableDictionary<string, string> _remapper;

		public static ObservableDictionary<string, string> Remapper {
			get {
				if (_remapper != null)
					return _remapper;

				var value = ConfigAsker["[Str Editor - Remapper]", ""];

				var gestures = new ObservableDictionary<string, string>();
				string[] groups = value.Split('%');

				foreach (var sub in groups) {
					if (sub.Length < 1)
						continue;

					string[] values = sub.Split('|');

					gestures[values[0]] = values[1];
				}

				_remapper = gestures;

				_remapper.CollectionChanged += delegate {
					StringBuilder b = new StringBuilder();

					foreach (var keyPair in _remapper) {
						b.Append(keyPair.Key);
						b.Append("|");
						b.Append(keyPair.Value);
						b.Append("%");
					}

					ConfigAsker["[Str Editor - Remapper]"] = b.ToString();
				};

				return gestures;
			}
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="checkBox">The check box.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		public static void Bind(CheckBox checkBox, Func<bool> get, Action<bool> set) {
			checkBox.IsChecked = get();
			checkBox.Checked += (e, a) => set(true);
			checkBox.Unchecked += (e, a) => set(false);
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="checkBox">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		public static void Bind(CheckBox checkBox, Func<bool> get, Action<bool> set, Action extra) {
			checkBox.IsChecked = get();

			checkBox.Checked += (e, a) => {
				set(true);
				extra();
			};

			checkBox.Unchecked += (e, a) => {
				set(false);
				extra();
			};
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="checkBox">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="extra">The action to take upon setting the binding.</param>
		public static void Bind(MenuItem checkBox, Func<bool> get, Action<bool> set, Action extra) {
			checkBox.IsChecked = get();

			checkBox.Checked += (e, a) => {
				set(true);
				extra();
			};

			checkBox.Unchecked += (e, a) => {
				set(false);
				extra();
			};
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="tb">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="converter">The converter to parse the string value.</param>
		public static void Bind<T>(TextBox tb, Func<T> get, Action<T> set, Func<string, T> converter) {
			tb.Text = get().ToString();

			tb.TextChanged += delegate {
				try {
					set(converter(tb.Text));
				}
				catch {
				}
			};
		}

		/// <summary>
		/// Binds the specified UIElement with a setting.
		/// </summary>
		/// <param name="tb">The UIElement.</param>
		/// <param name="get">The get method.</param>
		/// <param name="set">The set method.</param>
		/// <param name="converter">The converter to parse the string value.</param>
		/// /// <param name="extra">The action to take upon setting the binding.</param>
		public static void Bind<T>(TextBox tb, Func<T> get, Action<T> set, Func<string, T> converter, Action extra) {
			tb.Text = get().ToString();

			tb.TextChanged += delegate {
				try {
					set(converter(tb.Text));
					extra();
				}
				catch {
				}
			};
		}
		#endregion

		public static int ThemeIndex {
			get => int.Parse(ConfigAsker["[StrEditor - Theme index]", "0"]);
			set => ConfigAsker["[StrEditor - Theme index]"] = value.ToString(CultureInfo.InvariantCulture);
		}

		public static int Snap {
			get => int.Parse(ConfigAsker["[StrEditor - Snap]", "1"]);
			set => ConfigAsker["[StrEditor - Snap]"] = value.ToString(CultureInfo.InvariantCulture);
		}

		public class BufferedProperty<T> {
			private readonly ConfigAsker _ca;
			private readonly Func<string, T> _converter;
			private readonly T _def;
			private readonly string _prop;
			private bool _isSet;
			private T _value;

			public BufferedProperty(ConfigAsker ca, string prop, T def, Func<string, T> converter) {
				_ca = ca;
				_prop = prop;
				_def = def;
				_converter = converter;
			}

			public T Get() {
				if (_isSet)
					return _value;

				_isSet = true;
				_value = _converter(_ca[_prop, _def.ToString()]);
				return _value;
			}

			public void Set(T value) {
				_value = value;
				_isSet = true;
				_ca[_prop] = value.ToString();
			}

			public void Reset() {
				_isSet = false;
				_value = _def;
			}
		}

		public sealed class GrfResources {
			private readonly GrfHolder _grf;
			private MultiGrfReader _multiGrf = new MultiGrfReader();
			private bool _loaded = false;
			private bool _modified = false;
			private List<MultiGrfPath> _resources = new List<MultiGrfPath>();
			private bool _threadLoad = false;
			private bool _firstLoad;
			private object _lock = new object();

			public delegate void LoadedEventHandler();
			public delegate void ModifiedEventHandler();

			public event ModifiedEventHandler Modified;

			public bool Dirty {
				get { return _modified || !_loaded; }
			}

			private void OnModified() {
				ModifiedEventHandler handler = Modified;
				if (handler != null) handler();
			}

			private static string _mapExtractorResources {
				get => ConfigAsker["[MapExtractor - Resources]", ""];
				set => ConfigAsker["[MapExtractor - Resources]"] = value;
			}

			public MultiGrfReader MultiGrf {
				get {
					while (_threadLoad) {
						Thread.Sleep(100);
					}

					lock (_lock) {
						if (Dirty) {
							Reload();
						}
					}

					return _multiGrf;
				}

				set => _multiGrf = value;
			}

			public void SaveResources(string resources) {
				_mapExtractorResources = resources;
				_modified = true;
				OnModified();
			}

			/// <summary>
			/// Loads the GRF resource paths from the configuration file.
			/// </summary>
			/// <returns>A list of the GRF resource paths.</returns>
			public List<MultiGrfPath> LoadResources() {
				if (!Dirty)
					return _resources;

				var items = Methods.StringToList(_mapExtractorResources).Select(p => new MultiGrfPath(p) { FromConfiguration = true, IsCurrentlyLoadedGrf = false }).ToList();

				// Remove this old system
				for (int i = 0; i < items.Count; i++) {
					if (items[i].Path.StartsWith(GrfStrings.CurrentlyOpenedGrfHeader) ||
						items[i].Path.StartsWith("Currently opened GRF: ") ||
						items[i].Path.StartsWith("Currently opened GRF : ")) {
						items.RemoveAt(i);
						i--;
					}
				}

				if (_grf != null) {
					bool loadedGrf = false;

					// Mark the currently opened GRF
					for (int i = 0; i < items.Count; i++) {
						if (items[i].Path == _grf.FileName) {
							items[i].IsCurrentlyLoadedGrf = true;
							loadedGrf = true;
						}
					}

					if (!loadedGrf)
						items.Insert(0, new MultiGrfPath(_grf.FileName) { FromConfiguration = false, IsCurrentlyLoadedGrf = true });
				}

				_resources = items.ToList();
				return items;
			}

			public void Reload() {
				try {
					if (!Dirty)
						return;
					var paths = LoadResources();

					// When loading the GRFs, always make the open one to the front
					for (int i = 1; i < paths.Count; i++) {
						if (paths[i].IsCurrentlyLoadedGrf) {
							paths.Insert(0, paths[i]);
							paths.RemoveAt(i + 1);
							break;
						}
					}

					_multiGrf.Update(paths, _grf);
					_modified = false;
					_loaded = true;
					OnModified();
				}
				finally {
					_threadLoad = false;
				}
			}

			public GrfResources(GrfHolder grf = null) {
				_multiGrf.CurrentGrfAlwaysFirst = true;
				_grf = grf;
				_firstLoad = false;

				if (_grf != null) {
					_grf.ContainerOpened += delegate {
						if (!_firstLoad) {
							_firstLoad = true;
							return;
						}

						if (_threadLoad)
							return;

						_loaded = false;
						_modified = true;

						// Deferred load!
						_threadLoad = true;
						GrfThread.Start(Reload);
					};
				}
			}
		}

		public static GrfResources Resources = new GrfResources();
	}
}
