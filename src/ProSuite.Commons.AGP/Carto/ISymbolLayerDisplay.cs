using ArcGIS.Desktop.Mapping;

namespace ProSuite.Commons.AGP.Carto;

public interface ISymbolLayerDisplay
{
	public bool AutoSwitch { get; set; }
	public double AutoMinScaleDenom { get; set; }
	public double AutoMaxScaleDenom { get; set; }

	public bool NoMaskingWithoutSLD { get; set; }

	bool? QuickUsesSLD(Map map);
	bool? QuickUsesLM(Map map);

	bool UsesSLD(Map map, bool uncached = false);
	bool ToggleSLD(Map map, bool? enable = null);

	bool UsesLM(Map map, bool uncached = false);
	bool ToggleLM(Map map, bool? enable = null);
}
