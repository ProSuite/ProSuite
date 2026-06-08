using System;

namespace ProSuite.AGP.WorkList.Contracts;

[Flags]
public enum WorkItemStatus
{
	Unknown,
	Todo,
	Done
}
