using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

/// <summary>
/// Repository interface that encapsulates the persistence of the (volatile) state of work items.
/// It is used by the <see cref="IWorkItemRepository"/> implementations that manage all aspects
/// of work item persistence, including the access to the source classes in the geodatabase.
/// </summary>
public interface IWorkItemStateRepository
{
	/// <summary>
	/// Loads all states from the persisted state (work list file).
	/// </summary>
	void LoadAllStates();

	void Refresh(IWorkItem item);

	void UpdateState(IWorkItem item);

	/// <summary>
	/// Writes all states to the persisted state in the <see cref="WorkListDefinitionFilePath"/>
	/// </summary>
	/// <param name="sourceClasses"></param>
	/// <param name="extent"></param>
	void Commit([NotNull] IList<ISourceClass> sourceClasses,
	            Envelope extent);

	int? CurrentIndex { get; set; }

	void Rename(string name);

	/// <summary>
	/// The full path of the persistent storage.
	/// </summary>
	string WorkListDefinitionFilePath { get; set; }
}
