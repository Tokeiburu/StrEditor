using ErrorManager;
using GRF.Graphics;
using GrfToWpfBridge;
using StrEditor.Core.Viewport.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace StrEditor.Core.Viewport {
	public class InteractionManager {
		public const float ScaleClickRatio = 4f;
		public readonly BezierTool BezierTool = new BezierTool();
		public readonly ViewPanTool ViewportMoveTool = new ViewPanTool();
		public readonly OriginTool OriginTool = new OriginTool();
		public readonly SelectNodeTool SelectNodeTool = new SelectNodeTool();
		public readonly PointTranslateTool PointTranslateTool = new PointTranslateTool();
		public readonly LayerTransformTool LayerTransformTool = new LayerTransformTool();
		public readonly GifTool GifTool = new GifTool();
		public readonly UvTranslateTool UvTranslateTool = new UvTranslateTool();

		private FrameViewer _viewport;
		public List<ToolHandle> Handles = new List<ToolHandle>();
		public System.Drawing.Point Start;

		public ToolHandle ActiveHandle = null;
		public EditTool ActiveTool = null;

		public InteractionManager(FrameViewer viewport) {
			_viewport = viewport;
		}

		public bool OnMouseDown(MouseEventArgs e) {
			try {
				var w = _viewport.ViewportToWorld(e.Location);
				var args = new FrameViewerEventArgs();
				args.MouseArgs = e;
				args.MouseEventState = MouseEventState.MouseDown;

				for (int i = Handles.Count - 1; i >= 0; i--) {
					ToolHandle evt = Handles[i];
					if (w.X >= evt.MouseArea.Left &&
						w.X <= evt.MouseArea.Right &&
						w.Y <= -evt.MouseArea.Top &&
						w.Y >= -evt.MouseArea.Bottom) {
						args.PointId = evt.Id;

						if (evt.Tool.EventController(_viewport, args)) {
							Start = e.Location;
							SetActiveTool(evt.Tool);
							ActiveHandle = evt;
							return true;
						}
					}
				}

				return false;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
		}

		public bool OnMouseMove(MouseEventArgs e) {
			try {
				if (ActiveTool == null)
					return false;

				var args = new FrameViewerEventArgs();
				args.MouseArgs = e;
				args.PointId = ActiveHandle.Id;
				args.MouseEventState = MouseEventState.MouseMove;
				args.DeltaX = e.X - Start.X;
				args.DeltaY = e.Y - Start.Y;
				args.Start = Start;

				ActiveTool.EventController(_viewport, args);
				return true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
		}

		public bool OnMouseUp(MouseEventArgs e) {
			try {
				if (ActiveTool == null)
					return false;

				var args = new FrameViewerEventArgs();
				args.MouseArgs = e;
				args.PointId = ActiveHandle.Id;
				args.MouseEventState = MouseEventState.MouseUp;
				args.DeltaX = e.X - Start.X;
				args.DeltaY = e.Y - Start.Y;
				args.Start = Start;

				ActiveTool.EventController(_viewport, args);
				SetActiveTool(null);
				return true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
		}

		public void Register(EditTool tool) {
			Register(new TkVector2(), 0, 0, tool);
		}

		public void Register(Point p, float scale, int id, EditTool tool) {
			Register(p.ToTkVector2(), scale, id, tool);
		}

		public void Register(TkVector2 p, float scale, int id, EditTool tool) {
			scale *= ScaleClickRatio;
			Register(p, scale, scale, id, tool);
		}

		public void Register(TkVector2 p, float scaleX, float scaleY, int id, EditTool tool) {
			Rect rect = new Rect(
				new System.Windows.Point(p.X - (float)(scaleX / (2f * _viewport.ZoomEngine.Scale)), p.Y + (float)(scaleX / (2f * _viewport.ZoomEngine.Scale))),
				new System.Windows.Point(p.X + (float)(scaleY / (2f * _viewport.ZoomEngine.Scale)), p.Y - (float)(scaleY / (2f * _viewport.ZoomEngine.Scale))));

			if (scaleX == 0 || scaleY == 0) {
				rect = new Rect(double.MinValue, double.MinValue, double.PositiveInfinity, double.PositiveInfinity);
			}

			Register(rect, id, tool);
		}

		public void Register(in Rect area, int id, EditTool tool) {
			Handles.Add(new ToolHandle(area, id, tool));
		}

		public void Clear() {
			Handles.Clear();
		}

		public void SetActiveTool(EditTool tool) {
			ActiveTool = tool;
		}

		public bool IsToolActive(params EditTool[] tools) {
			return tools.Contains(ActiveTool);
		}

		public bool IsSelecedLayerUnderMouse(System.Windows.Point w) {
			var selectedLayer = _viewport.GetSelectedRenderer();

			if (selectedLayer == null)
				return false;

			return selectedLayer.IsMouseUnder(w);
		}
	}
}
