using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing;

public interface ISymbolizedSketchTool
{
	bool CanSetConstructionSketchSymbol(GeometryType geometryType);

	void SetSketchSymbol([CanBeNull] CIMSymbolReference symbolReference);

	void SetSketchType(SketchGeometryType type);

	bool CanSelectFromLayer([CanBeNull] Layer layer);
}
