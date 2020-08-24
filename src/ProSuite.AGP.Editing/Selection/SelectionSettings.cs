using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Selection
{
	public class SelectionSettings
	{
		public SelectionSettings(SketchGeometryType sketchGeometryType = SketchGeometryType.Polygon)
		{
			SketchGeometryType = sketchGeometryType;
			SketchOutputMode = MapView.Active.ViewingMode == MapViewingMode.Map ? SketchOutputMode.Map : SketchOutputMode.Screen;
		}

		public SketchGeometryType SketchGeometryType { get; set; }
		public SketchOutputMode SketchOutputMode { get; set; }
		public int SelectionTolerancePixels { get; set; }
	}
}
