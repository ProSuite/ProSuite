using ArcGIS.Desktop.Mapping;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// The public interface of the Symbol Display Manager:
/// allows querying and toggling SLD and LM on a given map,
/// and maintains per-map user preferences. The QuickFoo()
/// methods are fast enough to use in a Button's OnUpdate()
/// method but only ask an internal state cache and may
/// return null (unknown). 
/// </summary>
public interface ISymbolDisplayManager
{
	public IIndexedProperty<Map, bool> NoMaskingWithoutSLD { get; }

	public IIndexedProperty<Map, bool> AutoSwitch { get; }
	public IIndexedProperty<Map, double> AutoMinScaleDenom { get; }
	public IIndexedProperty<Map, double> AutoMaxScaleDenom { get; }

	public IIndexedProperty<Map, bool?> WantSLD { get; }
	public IIndexedProperty<Map, bool?> WantLM { get; }

	bool? QuickUsesSLD(Map map);

	bool? QuickUsesLM(Map map);

	bool UsesSLD(Map map, bool uncached = false);

	bool ToggleSLD(Map map, bool? enable = null);

	bool UsesLM(Map map, bool uncached = false);

	bool ToggleLM(Map map, bool? enable = null);
}

public interface IIndexedProperty<in TKey, TValue>
{
	TValue this[TKey key] { get; set; }
}
