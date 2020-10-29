using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.ProTrials
{
	public class ProTrialsModule : Module, IExtensionConfig
	{
		private static ProTrialsModule _instance;
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private static ExtensionState _extensionState = ExtensionState.Disabled;

		private readonly IList<Graphic> _graphics = new List<Graphic>();
		private MapView _graphicsMapView;

		public static ProTrialsModule Instance
		{
			get
			{
				return _instance ?? (_instance =
					                     (ProTrialsModule) FrameworkApplication.FindModule(
						                     "ProSuite_ProTrials_Module"));
			}
		}

		#region Extension Config (Licensing/Enabling)

		internal static string AuthorizationID { get; set; } = "";

		internal static bool CheckLicensing(string id)
		{
			bool valid = !string.IsNullOrWhiteSpace(id);

			if (valid)
			{
				FrameworkApplication.State.Activate("prosuite_protrials_enabled");
				_extensionState = ExtensionState.Enabled;
			}
			else
			{
				FrameworkApplication.State.Deactivate("prosuite_protrials_enabled");
				_extensionState = ExtensionState.Disabled;
			}

			return valid;
		}

		// Implement Message and ProductName to override <extensionConfig> in DAML

		public string Message
		{
			get => string.Empty;
			set { }
		}

		public string ProductName
		{
			get => string.Empty;
			set { }
		}

		public ExtensionState State
		{
			get => _extensionState;
			set
			{
				if (value == ExtensionState.Disabled)
				{
					_extensionState = ExtensionState.Disabled;
				}
				else if (value == ExtensionState.Enabled)
				{
					if (! CheckLicensing(AuthorizationID))
					{
						new ProTrialsRegistrationWindow().ShowDialog();
					}
				}
			}
		}

		#endregion

		public void SetGraphics(IEnumerable<MapPoint> points, CIMSymbolReference symbol, double refScale)
		{
			var mapView = MapView.Active;
			if (mapView == null) return;

			_graphicsMapView = mapView;

			foreach (var point in points)
			{
				var disposable = mapView.AddOverlay(point, symbol, refScale);

				_graphics.Add(new Graphic(point, symbol, disposable));
			}
		}

		public void ColorGraphics()
		{
			_msg.Info("Updating symbol of existing map overlay graphics");

			using (var colors = ColorUtils.RandomColors().GetEnumerator())
			{
				foreach (var graphic in _graphics)
				{
					if (! colors.MoveNext()) break;
					var symbol = SymbolUtils.CreateMarker(
						                        colors.Current, 5,
						                        SymbolUtils.MarkerStyle.Circle)
					                        .MakePointSymbol()
					                        .MakeSymbolReference();

					_graphicsMapView.UpdateOverlay(graphic.Disposable, graphic.Point, symbol);
				}
			}
		}

		public void ClearGraphics()
		{
			_msg.Info("Removing existing map overlay graphics");

			foreach (var graphic in _graphics)
			{
				graphic.Disposable.Dispose();
			}

			_graphics.Clear();
		}

		protected override bool Initialize()
		{
			return true;
		}

		protected override bool CanUnload()
		{
			return true;
		}

		private class Graphic
		{
			public readonly MapPoint Point;
			public readonly CIMSymbolReference Symbol;
			public readonly IDisposable Disposable;

			public Graphic(MapPoint point, CIMSymbolReference symbol, IDisposable disposable)
			{
				Point = point;
				Symbol = symbol;
				Disposable = disposable;
			}
		}
	}

	public class ClearSunflowerButton : Button
	{
		protected override void OnClick()
		{
			QueuedTask.Run(() => ProTrialsModule.Instance.ClearGraphics());
		}
	}

	public class ColorSunflowerButton : Button
	{
		protected override void OnClick()
		{
			QueuedTask.Run(() => ProTrialsModule.Instance.ColorGraphics());
		}
	}

	public class CreateSunflowerTool : MapTool
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public CreateSunflowerTool()
		{
			IsSketchTool = true;
			SketchType = SketchGeometryType.Rectangle;
			SketchOutputMode = SketchOutputMode.Map;
		}

		protected override Task OnToolActivateAsync(bool hasMapViewChanged)
		{
			// TODO initialize symbology stuff

			return Task.FromResult(0);
		}

		protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
		{
			return Task.FromResult(0);
		}

		protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
		{
			// Sketch geometry: sref is null for SketchOutputMode.Screen,
			// but map's spatial reference for SketchOutputMode.Map (good).

			var mapView = MapView.Active;
			if (mapView == null || geometry == null || geometry.IsEmpty)
				return Task.FromResult(true); // true means we handled this event

			var center = GeometryEngine.Instance.Centroid(geometry);
			var sref = geometry.SpatialReference;
			_msg.InfoFormat("Sketch: x={0}, y={1}, sref={2}", center.X, center.Y, sref?.Name ?? "null");

			var mapSRef = mapView.Map?.SpatialReference;
			var refScale = mapView.Map?.ReferenceScale ?? 0.0; // no refScale is encoded as zero
			_msg.InfoFormat("Map: sref={0}, refScale={1}", mapSRef?.Name ?? "null", refScale);

			// Get feature(s), generate pattern, update overlay
			return QueuedTask.Run(() =>
			{
				var features = mapView.GetFeatures(geometry);
				mapView.FlashFeature(features);

				var outlines = new List<Geometry>();

				foreach (var pair in features)
				{
					var layer = pair.Key;
					var uri = layer.URI;

#if PRO27
					foreach (var oid in pair.Value)
					{
						var outline = layer.QueryDrawingOutline(oid, mapView, DrawingOutlineType.Exact);
						outlines.Add(outline);
					}
#endif
				}

				var perimeter = GeometryEngine.Instance.Union(outlines);
				var projected = GeometryEngine.Instance.Project(perimeter, mapSRef);
				var symbol = SymbolUtils.CreatePolygonSymbol(ColorUtils.RedRGB.SetAlpha(50))
				                        .MakeSymbolReference();

				// Add combined perimeter as Tool Overlay -- will be
				// automatically removed when the tool is deactivated:
				var overlay = AddOverlay(perimeter, symbol, refScale);
				// Just for reference: can update overlay's sym and geom:
				// bool ok = UpdateOverlay(overlay, geometry, symbol);


				if (geometry.SpatialReference == null)
				{
					center = mapView.ScreenToMap(new System.Windows.Point(center.X, center.Y));
				}

				var seedSymbol = SymbolUtils.CreateMarker(
					                            ColorUtils.BlackRGB, 5,
					                            SymbolUtils.MarkerStyle.Circle)
				                            .MakePointSymbol()
				                            .MakeSymbolReference();

				var points = new List<MapPoint>();
				const double k = 12;
				foreach (var coords in SunflowerPoints(center.X, center.Y, k).Take(50))
				{
					var point = MapPointBuilder.CreateMapPoint(coords, perimeter.SpatialReference);
					if (GeometryEngine.Instance.Contains(perimeter, point))
					{
						points.Add(point);
					}
				}

				ProTrialsModule.Instance.SetGraphics(points, seedSymbol, refScale);

				return true;
			});
		}

		protected override void OnToolKeyDown(MapViewKeyEventArgs k)
		{
			// Intended for synchronous actions; set k.Handled to true to have HandleKeyDownAsync invoked
			k.Handled = true;
		}

		protected override Task HandleKeyDownAsync(MapViewKeyEventArgs k)
		{
			// Intended for async actions; called only if k.Handled=true in OnToolKeyDown
			return Task.FromResult(0);
		}

		/// <summary>
		/// Yield points spiraling around the given center point in about
		/// the same way as sunflower seeds. The first point is at x0/y0.
		/// The "spreading factor" k determines how tightly or loosely the
		/// points are packed. Use Take(n) to get the first n points.
		/// </summary>
		public static IEnumerable<Coordinate2D> SunflowerPoints(double x0, double y0, double k)
		{
			const double phi = 1.61803398874989;
			double phisq = phi * phi;
			const int n = int.MaxValue;

			for (int i = 0; i < n; i++)
			{
				double r = k * Math.Sqrt(i);
				double a = 2 * Math.PI * (i / phisq % 1);

				double x = x0 + r * Math.Cos(a);
				double y = y0 + r * Math.Sin(a);

				yield return new Coordinate2D(x, y);
			}
		}
	}
}
