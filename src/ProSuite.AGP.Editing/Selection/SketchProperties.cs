using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Selection;

public class SketchProperties
{
	public SketchProperties(
		SketchGeometryType sketchGeometryType = SketchGeometryType.Rectangle,
		SpatialRelationship spatialRelationship = SpatialRelationship.Intersects)
	{
		SketchGeometryType = sketchGeometryType;
		SketchOutputMode = SketchOutputMode.Map;
		SpatialRelationship = spatialRelationship;
	}

	public SpatialRelationship SpatialRelationship { get; }

	public SketchGeometryType SketchGeometryType { get; }

	/// screen coords are currently not supported and only relevant
	/// when selecting with the View being in 3D viewing mode
	public SketchOutputMode SketchOutputMode { get; }
}
