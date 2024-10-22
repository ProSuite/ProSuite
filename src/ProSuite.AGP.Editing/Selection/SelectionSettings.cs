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
	}
}
