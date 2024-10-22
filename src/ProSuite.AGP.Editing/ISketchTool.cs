using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing;

public interface ISketchTool
{
	string Caption { get; }

	void SetSketchType(SketchGeometryType? sketchType);
}
