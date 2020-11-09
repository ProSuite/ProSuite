using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain.Persistence
{
	// todo daro rename
	public interface IRepository
	{
		IWorkItem Refresh(IWorkItem item);

		void Update(IWorkItem item);

		void UpdateVolatileState([NotNull] IEnumerable<IWorkItem> items);

		void Commit();

		void Discard();

		int? CurrentIndex { get; set; }
	}
}
