using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Service
{
	public interface IWorkItemRepository
	{
		IEnumerable<WorkItem> GetAll();

	}
}
