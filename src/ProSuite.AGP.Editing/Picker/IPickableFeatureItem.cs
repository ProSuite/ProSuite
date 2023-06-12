using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.Editing.Picker
{
	public interface IPickableFeatureItem : IPickableItem
	{
		long Oid { get; }
		BasicFeatureLayer Layer { get; }
		Feature Feature { get; }
	}
}
