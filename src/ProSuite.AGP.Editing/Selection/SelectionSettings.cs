using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Selection
{
	public class SelectionSettings
	{
		public SelectionSettings(
			int selectionTolerancePixels = 3,
			SpatialRelationship spatialRelationship = SpatialRelationship.Intersects)
		{
			SketchOutputMode = SketchOutputMode.Map;
			SelectionTolerancePixels = selectionTolerancePixels;
			SpatialRelationship = spatialRelationship;
		}

		public SpatialRelationship SpatialRelationship { get; set; }

		/// screen coords are currently not supported and only relevant
		/// when selecting with the View being in 3D viewing mode
		public SketchOutputMode SketchOutputMode { get; set; }

		/// <summary>
		/// Will be applied to points only
		/// </summary>
		public int SelectionTolerancePixels { get; set; }
	}
}
