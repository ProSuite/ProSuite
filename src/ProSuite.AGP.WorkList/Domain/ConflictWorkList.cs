using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain;

public class ConflictWorkList : DbStatusWorkList
{
	public ConflictWorkList([NotNull] IWorkItemRepository repository,
	                        [NotNull] IMapViewContext mapViewContext,
	                        [NotNull] Geometry areaOfInterest,
	                        [NotNull] string name,
	                        [NotNull] string displayName) : base(
		repository, mapViewContext, areaOfInterest, name, displayName) { }

	public override void UpdateDefinitionExpressions() { }
}
