using System.Collections.Generic;

namespace ProSuite.AGP.WorkList.Domain.Persistence;

public interface IWorkListDefinition<T> where T : IWorkItemState
{
	string Name { get; set; }

	string Path { get; set; }

	List<T> Items { get; set; }
}
