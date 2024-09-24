using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.AGP.Editing;

public interface ISymbolizedSketchTool
{
	bool CanSetConstructionSketchSymbol(GeometryType geometryType);

	void SetSketchSymbol([CanBeNull] CIMSymbolReference symbolReference);

	void SetSketchType(SketchGeometryType type);

	bool CanSelectFromLayer([CanBeNull] Layer layer);
	
	public bool CanUseSelection([NotNull] Dictionary<BasicFeatureLayer, List<long>> selectionByLayer);
}
