using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain.Persistence;

public class EmptyWorkItemStateRepository : IWorkItemStateRepository
{
	private int? _currentIndex = 0;

	public string WorkListDefinitionFilePath { get; set; }

	public IWorkItem Refresh(IWorkItem item)
	{
		return item;
	}

	public void UpdateState(IWorkItem item)
	{
	}

	public void UpdateVolatileState(IEnumerable<IWorkItem> items)
	{
	}

	public void Commit(IList<ISourceClass> sourceClasses)
	{
	}

	public void Discard()
	{
	}

	public int? CurrentIndex
	{
		get => _currentIndex;
		set => _currentIndex = value;
	}

	public void Rename(string name)
	{
		throw new System.NotImplementedException();
	}
}
