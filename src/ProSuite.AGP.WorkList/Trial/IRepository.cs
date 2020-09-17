using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Trial
{
	public interface IRepository<T> where T : IWorkItemState
	{
		IWorkItem Refresh(IWorkItem item);

		void UpdateVolatileState([NotNull] IEnumerable<IWorkItem> items);

		void Commit();

		void Discard();
	}
}
