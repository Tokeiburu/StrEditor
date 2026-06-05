using System;
using System.Reflection;
using System.Windows;
using ErrorManager;
using GRF.Image;
using GRF.IO;
using GRF.GrfSystem;
using GrfToWpfBridge.Application;
using StrEditor.ApplicationConfiguration;
using TokeiLibrary;
using Utilities;

namespace StrEditor {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public App() {
			Configuration.ConfigAsker = StrEditorConfiguration.ConfigAsker;
			ErrorHandler.SetErrorHandler(new DefaultErrorHandler());
			Settings.TempPath = GrfPath.Combine(StrEditorConfiguration.ProgramDataPath, "tmp");
			TemporaryFilesManager.ClearTemporaryFiles();
		}

		protected override void OnStartup(StartupEventArgs e) {
			ApplicationManager.CrashReportEnabled = true;
			ImageConverterManager.AddConverter(new DefaultImageConverter());

			Configuration.SetImageRendering(Resources);

			Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/GRFEditorStyles.xaml", UriKind.RelativeOrAbsolute) });
			//Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "%20") + ";component/WPF/Styles/StyleDark.xaml", UriKind.RelativeOrAbsolute) });

			if (!Methods.IsWinVistaOrHigher() && Methods.IsWinXPOrHigher()) {
				// We are on Windows XP, force the style.
				try {
					Uri uri = new Uri("PresentationFramework.Aero;V3.0.0.0;31bf3856ad364e35;component\\themes/aero.normalcolor.xaml", UriKind.Relative);
					Resources.MergedDictionaries.Add(LoadComponent(uri) as ResourceDictionary);
				}
				catch {
					MessageBox.Show("Failed to apply a style override for Windows XP's theme.");
				}
			}

			base.OnStartup(e);
		}
	}
}
