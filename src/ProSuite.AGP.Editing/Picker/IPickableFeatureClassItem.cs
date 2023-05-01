using System.Collections.Generic;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickableFeatureClassItem: IPickableItem
	{
		List<BasicFeatureLayer> Layers { get; set; }
		IReadOnlyList<long> Oids { get; }
	}
}
