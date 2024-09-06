using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing;

public interface ISymbolizedSketchTool
{
	MapView ActiveMapView { get; }
	void SetSketchSymbol(CIMSymbolReference symbolReferencembol);
}
