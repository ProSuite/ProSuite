using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain;

public abstract class DbStatusWorkList : WorkList
{
	protected DbStatusWorkList([NotNull] IWorkItemRepository repository,
	                           [NotNull] Geometry areaOfInterest,
	                           [NotNull] string name,
	                           [NotNull] string displayName)
		: base(repository, areaOfInterest, name, displayName) { }

	public override bool CanSetStatus()
	{
		return base.CanSetStatus() && base.CanSetStatus();
	}

	protected virtual bool CanSetStatusCore()
	{
		return Project.Current?.IsEditingEnabled == true;
	}
}
