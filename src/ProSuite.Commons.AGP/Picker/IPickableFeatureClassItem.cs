using System.Collections.Generic;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.Commons.AGP.Picker
{
	public interface IPickableFeatureClassItem : IPickableItem
	{
		List<BasicFeatureLayer> Layers { get; }

		IReadOnlyList<long> Oids { get; }
	}
}
