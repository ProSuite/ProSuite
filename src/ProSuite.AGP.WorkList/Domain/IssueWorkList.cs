using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	public class IssueWorkList : WorkList
	{
		public IssueWorkList(IWorkItemRepository repository,
		                     [NotNull] string name,
		                     [CanBeNull] Geometry areaOfInterest = null,
							 [CanBeNull] string displayName = null) :
			base(repository, name, areaOfInterest, displayName) { }

		public IssueWorkList(IWorkItemRepository repository,
		                     [NotNull] string name,
		                     [CanBeNull] string displayName) :
			base(repository, name, null, displayName)
		{ }

		protected override string GetDisplayNameCore()
		{
			return "Issue Work List";
		}
	}
}
