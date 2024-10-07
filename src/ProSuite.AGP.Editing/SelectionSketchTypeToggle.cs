using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing;

public class SelectionSketchTypeToggle : SelectionSketchTypeToggleBase
{
	public SelectionSketchTypeToggle(ISketchTool tool,
	                             SketchGeometryType defaultSelectionSketchType) : base(
		tool, defaultSelectionSketchType) { }
}
