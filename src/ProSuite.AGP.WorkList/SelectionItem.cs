using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class SelectionItem : WorkItem
	{
		public SelectionItem(long id, [NotNull] Row row) : base(id, row) { }
	}
}
