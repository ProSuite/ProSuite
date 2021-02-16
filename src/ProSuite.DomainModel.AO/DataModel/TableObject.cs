using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class TableObject : ObjectObject
	{
		protected TableObject([NotNull] IRow obj,
		                      [NotNull] TableDataset dataset,
		                      [CanBeNull] IFieldIndexCache fieldIndexCache)
			: base((IObject) obj, dataset, fieldIndexCache) { }
	}
}
