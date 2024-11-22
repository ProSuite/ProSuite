using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;

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
	public Row GetDbRow(DbStatusWorkItem currentItem)
	{
		// Consider pulling up to interface
		var gdbRepository = (GdbItemRepository) Repository;

		return gdbRepository.GetGdbItemRow(currentItem);
	}
}
