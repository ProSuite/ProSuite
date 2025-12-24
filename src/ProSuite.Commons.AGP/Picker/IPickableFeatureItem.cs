using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Picker;

public interface IPickableFeatureItem : IPickableItem
{
	long Oid { get; }

	[NotNull]
	BasicFeatureLayer Layer { get; }

	[NotNull]
	Feature Feature { get; }
}
