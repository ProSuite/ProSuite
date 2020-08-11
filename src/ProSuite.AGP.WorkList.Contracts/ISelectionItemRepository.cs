using System.Collections.Generic;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface ISelectionItemRepository : IWorkItemRepository
	{
		// todo daro: get rid of this! Pass in all needed parameters in constructor.
		void RegisterDatasets(Dictionary<GdbTableIdentity, List<long>> selection);
	}
}
