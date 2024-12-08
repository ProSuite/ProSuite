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

	/// <summary>
	/// Gets the source row from the database.
	/// </summary>
	/// <param name="currentItem"></param>
	/// <returns></returns>
	[CanBeNull]
	public Row GetDbRow(DbStatusWorkItem currentItem)
	{
		// Consider pulling up to interface
		var gdbRepository = (GdbItemRepository) Repository;

		return gdbRepository.GetGdbItemRow(currentItem);
	}

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
