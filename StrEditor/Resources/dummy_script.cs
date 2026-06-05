using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.FileFormats.StrFormat;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.SprFormat;
using GRF.FileFormats.PalFormat;
using GRF.Image;
using GRF.Image.Decoders;
using GRF.Graphics;
using GRF.Core;
using GRF.IO;
using GRF.System;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;
using Point = System.Windows.Point;

namespace Scripts {
    public class Script : IStrScript {
		public object DisplayName {
			get { return "MyCustomScript"; }
		}

		public string Group {
			get { return "Scripts"; }
		}

		public string InputGesture {
			get { return null; }
		}

		public string Image {
			get { return null; }
		}
		
		public void Execute(Str str, int selectedLayerIndex, int selectedFrameIndex, int[] selectedLayerIndexes, int[] selectedFrameIndexes) {
			if (str == null) return;
			
			Exception backupErr = null;
			
			try {
				str.Commands.BeginNoDelay();
				str.Commands.Backup(_ => {
					try {
						
					}
					catch (Exception err) {
						backupErr = err;
					}
				}, "MyCustomScript", true);
				
				if (backupErr != null) {
					throw backupErr;
				}
			}
			catch (Exception err) {
				str.Commands.CancelEdit();
				ErrorHandler.HandleException(err, ErrorLevel.Warning);
			}
			finally {
				str.Commands.End();
				str.InvalidateVisualRedraw();
			}
		}
		
		public bool CanExecute(Str str, int selectedLayerIndex, int selectedFrameIndex, int[] selectedLayerIndexes, int[] selectedFrameIndexes) {
			return true;
			//return str != null;
		}
	}
}