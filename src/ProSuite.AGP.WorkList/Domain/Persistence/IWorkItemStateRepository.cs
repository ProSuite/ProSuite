using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain.Persistence
{
	/// <summary>
	/// Repository interface that encapsulates the persistence of the (volatile) state of work items.
	/// It is used by the <see cref="IWorkItemRepository"/> implementations that manage all aspects
	/// of work item persistence, including the access to the source classes in the geodatabase.
	/// </summary>
	public interface IWorkItemStateRepository
	{
		string WorkListDefinitionFilePath { get; set; }

		IWorkItem Refresh(IWorkItem item);

		void Update(IWorkItem item);

		void UpdateVolatileState([NotNull] IEnumerable<IWorkItem> items);

		void Commit(IList<ISourceClass> sourceClasses);

		void Discard();

		int? CurrentIndex { get; set; }
	}
}
