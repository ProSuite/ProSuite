using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	public class IssueWorkList : DbStatusWorkList
	{
		public IssueWorkList(IWorkItemRepository repository,
		                     [NotNull] string name,
		                     [CanBeNull] Geometry areaOfInterest = null,
		                     [CanBeNull] string displayName = null) :
			base(repository, name, areaOfInterest, displayName) { }

		// TODO: (daro) revise. If WorkListUtils.Create doesn't instantiate the work list anymore
		//		 we can ensure AOI is not null. Necessary to not search the entire DB.
		/// <summary>
		/// Do not change this constructor at all, it is used for dynamic loading!
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="name"></param>
		/// <param name="displayName"></param>
		[UsedImplicitly]
		public IssueWorkList(IWorkItemRepository repository,
		                     [NotNull] string name,
		                     [CanBeNull] string displayName) :
			base(repository, name, null, displayName) { }

		protected override string GetDisplayNameCore()
		{
			return "Issue Work List";
		}
	}
}
