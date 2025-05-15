using System;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain;

public class SelectionWorkList : WorkList, IDisposable
{
	public SelectionWorkList([NotNull] IWorkItemRepository repository,
	                         [NotNull] Geometry areaOfInterest,
	                         [NotNull] string uniqueName,
	                         [NotNull] string displayName) :
		base(repository, areaOfInterest, uniqueName, displayName) { }

	public void Dispose()
	{
		DeactivateRowCacheSynchronization();
	}
}
