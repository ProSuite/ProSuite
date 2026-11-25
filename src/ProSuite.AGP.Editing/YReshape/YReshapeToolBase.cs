using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.AGP.Editing.Properties;

namespace ProSuite.AGP.Editing.YReshape;

public abstract class YReshapeToolBase : AdvancedReshapeToolBase
{
	protected override string OptionsFileName => "YReshapeToolOptions.xml";

	protected override SelectionCursors FirstPhaseCursors { get; } =
		SelectionCursors.CreateArrowCursors(Resources.YReshapeOverlay);

	protected override bool CanSelectGeometryType(GeometryType geometryType)
	{
		return geometryType == GeometryType.Polyline;
	}
}