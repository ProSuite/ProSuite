using ArcGIS.Desktop.Mapping;

namespace ProSuite.Commons.AGP.Carto;

public interface ISymbolDisplayManager
{
	public IIndexedProperty<Map, bool> AutoSwitch { get; }
	public IIndexedProperty<Map, double> AutoMinScaleDenom { get; }
	public IIndexedProperty<Map, double> AutoMaxScaleDenom { get; }

	public IIndexedProperty<Map, bool> NoMaskingWithoutSLD { get; }

	bool? QuickUsesSLD(Map map);
	bool? QuickUsesLM(Map map);

	bool UsesSLD(Map map, bool uncached = false);
	bool ToggleSLD(Map map, bool? enable = null);

	bool UsesLM(Map map, bool uncached = false);
	bool ToggleLM(Map map, bool? enable = null);
}
