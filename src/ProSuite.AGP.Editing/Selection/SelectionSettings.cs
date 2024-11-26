using ArcGIS.Core.Data;

namespace ProSuite.AGP.Editing.Selection
{
	public class SelectionSettings
	{
		public SelectionSettings(
			int selectionTolerancePixels = 3,
			SpatialRelationship spatialRelationship = SpatialRelationship.Intersects)
		{
			SelectionTolerancePixels = selectionTolerancePixels;
			SpatialRelationship = spatialRelationship;
		}

		public SpatialRelationship SpatialRelationship { get; set; }

		/// <summary>
		/// Will be applied to points only
		/// </summary>
		public int SelectionTolerancePixels { get; set; }

		/// <summary>
		/// If true, the default selection sketch will be a rectangle even if in the previous
		/// selection a polygon or lasso was used.
		/// </summary>
		public bool PreferRectangleSelectionSketch { get; set; } = true;
	}
}
