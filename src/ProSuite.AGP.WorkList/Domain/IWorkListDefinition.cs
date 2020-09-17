using System.Collections.Generic;

namespace ProSuite.AGP.WorkList.Domain
{
	// todo daro: abstract class WorkListDefinition?
	public interface IWorkListDefinition<T> where T : IWorkItemState
	{
		string Path { get; set; }

		List<T> Items { get; set; }
	}
}
