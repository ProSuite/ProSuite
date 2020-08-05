using System.Collections.Generic;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISelectionItemRepository : IWorkItemRepository
	{
		void RegisterDatasets(Dictionary<GdbTableReference, List<long>> selection);
	}
}
