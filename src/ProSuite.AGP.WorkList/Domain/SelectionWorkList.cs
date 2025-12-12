using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain;

public class SelectionWorkList : WorkList
{
	public SelectionWorkList([NotNull] IWorkItemRepository repository,
	                         [NotNull] IMapViewContext mapViewContext,
	                         [CanBeNull] Geometry areaOfInterest,
	                         [NotNull] string uniqueName,
	                         [NotNull] string displayName) :
		base(repository, mapViewContext, areaOfInterest, uniqueName, displayName)
	{ }
}
