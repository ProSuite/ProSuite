using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing;

public interface ISymbolizedSketchTool
{
	void SetSketchType(SketchGeometryType? sketchType);

	Task<bool> CanSetConstructionSketchSymbol(GeometryType geometryType);

	void SetSketchSymbol([CanBeNull] CIMSymbolReference symbolReference);

	bool CanSelectFromLayer([CanBeNull] Layer layer);
	
	bool CanUseSelection([NotNull] Dictionary<BasicFeatureLayer, List<long>> selectionByLayer);
}
