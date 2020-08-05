using System.Collections.Generic;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISelectionItemRepository : IWorkItemRepository
	{
		void RegisterDatasets(Dictionary<GdbTableIdentity, List<long>> selection);
	}
}
