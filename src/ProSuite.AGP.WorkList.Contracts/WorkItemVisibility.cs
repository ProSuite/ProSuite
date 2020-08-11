using System;

namespace ProSuite.AGP.WorkList.Contracts
{
	[Flags]
	public enum WorkItemVisibility
	{
		None = 0,
		Todo = 1,
		Done = 2,
		All = 3
	}
}
