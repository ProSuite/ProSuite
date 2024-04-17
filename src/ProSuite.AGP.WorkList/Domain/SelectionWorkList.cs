using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	public class SelectionWorkList : WorkList
	{
		public SelectionWorkList(IWorkItemRepository repository,
		                         string uniqueName,
		                         string displayName,
		                         [CanBeNull] Geometry areaOfInterest = null) :
			base(repository, uniqueName, areaOfInterest, displayName) { }

		protected override string GetDisplayNameCore()
		{
			return "Selection Work List";
		}
	}
}
