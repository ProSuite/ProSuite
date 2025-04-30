using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain;

public abstract class DbStatusWorkList : WorkList
{
	protected DbStatusWorkList([NotNull] IWorkItemRepository repository,
	                           [NotNull] Geometry areaOfInterest,
	                           [NotNull] string name,
	                           [NotNull] string displayName)
		: base(repository, areaOfInterest, name, displayName) { }

	protected override bool CanSetStatusCore()
	{
		return Project.Current?.IsEditingEnabled == true;
	}

	protected override IEnumerable<IWorkItem> GetWorkItemsForInnermostContextCore(
		QueryFilter filter, VisitedSearchOption visitedSearch,
		CurrentSearchOption currentSearch)
	{
		Assert.NotNull(AreaOfInterest);
		Assert.False(AreaOfInterest.IsEmpty, "aoi is empty");

		return base.GetWorkItemsForInnermostContextCore(
			GdbQueryUtils.CreateSpatialFilter(AreaOfInterest), visitedSearch, currentSearch);
	}
}
