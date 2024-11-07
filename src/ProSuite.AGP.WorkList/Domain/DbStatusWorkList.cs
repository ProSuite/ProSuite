using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain;

public abstract class DbStatusWorkList : WorkList
{
	protected DbStatusWorkList(IWorkItemRepository repository,
	                           string name, Geometry areaOfInterest = null,
	                           string displayName = null)
		: base(repository, name, areaOfInterest, displayName) { }
}
