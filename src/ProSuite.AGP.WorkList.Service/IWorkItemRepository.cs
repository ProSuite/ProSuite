using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Service
{
	public interface IWorkItemRepository
	{
		IEnumerable<IWorkItem> GetAll();

		// alternativ: IWorkList GetWorkList(); // list.Name, list.Extent
	}
}
