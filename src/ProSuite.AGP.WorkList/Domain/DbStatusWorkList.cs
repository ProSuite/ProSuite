using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain;

public abstract class DbStatusWorkList : WorkList
{
	protected DbStatusWorkList(IWorkItemRepository repository,
	                           string name, Geometry areaOfInterest = null,
	                           string displayName = null)
		: base(repository, name, areaOfInterest, displayName) { }


	[CanBeNull]
	public IAttributeReader GetAttributeReader(long forSourceClassId)
	{
		return Repository.SourceClasses
		                 .FirstOrDefault(sc => sc.GetUniqueTableId() == forSourceClassId)
		                 ?.AttributeReader;
	}

	protected override bool CanSetStatusCore()
	{
		return Project.Current?.IsEditingEnabled == true;
	}
}
