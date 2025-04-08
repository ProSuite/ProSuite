using System.Collections.Generic;

namespace ProSuite.AGP.WorkList.Contracts
{
	/// <summary>
	/// Repository interface that encapsulates the persistence of the (volatile) state of work items.
	/// It is used by the <see cref="IWorkItemRepository"/> implementations that manage all aspects
	/// of work item persistence, including the access to the source classes in the geodatabase.
	/// </summary>
	public interface IWorkItemStateRepository
	{
		IWorkItem Refresh(IWorkItem item);

		void UpdateState(IWorkItem item);

		void Commit(IList<ISourceClass> sourceClasses);

		void Discard();

		int? CurrentIndex { get; set; }

		void Rename(string name);
	}
}
