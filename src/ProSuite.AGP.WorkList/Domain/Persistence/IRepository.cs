using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain.Persistence
{
	public interface IRepository
	{
		IWorkItem Refresh(IWorkItem item);

		void UpdateVolatileState([NotNull] IEnumerable<IWorkItem> items);

		void Commit();

		void Discard();
	}
}
