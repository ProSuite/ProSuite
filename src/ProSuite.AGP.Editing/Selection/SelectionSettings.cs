using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Internal.Mapping;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Selection
{
	public class SelectionSettings
	{
		public SelectionSettings(SketchGeometryType sketchGeometryType = SketchGeometryType.Rectangle, int selectionTolerancePixels = 3, SpatialRelationship spatialRelationship = SpatialRelationship.Intersects)
		{
			SketchGeometryType = sketchGeometryType;
			SketchOutputMode = MapView.Active.ViewingMode == MapViewingMode.Map ? SketchOutputMode.Map : SketchOutputMode.Screen;
			SelectionTolerancePixels = selectionTolerancePixels;
			SpatialRelationship = spatialRelationship;
		}

		public SpatialRelationship SpatialRelationship { get; set; }

		public SketchGeometryType SketchGeometryType { get; set; }
		public SketchOutputMode SketchOutputMode { get; set; }

		/// <summary>
		/// Will be applied to all geometries except points
		/// </summary>
		public int SelectionTolerancePixels { get; set; }

		

		/// <summary>
		/// Will be applied to point geometries
		/// </summary>
		public const int PointBufferInPixels = 3;
	}
}
