using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain;

public class SelectionWorkList : WorkList
{
	public SelectionWorkList([NotNull] IWorkItemRepository repository,
	                         [CanBeNull] Geometry areaOfInterest,
	                         [NotNull] string uniqueName,
	                         [NotNull] string displayName) :
		base(repository, areaOfInterest, uniqueName, displayName)
	{ }
}
