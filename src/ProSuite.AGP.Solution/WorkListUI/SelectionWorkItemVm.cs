using System;
using System.Collections.Generic;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionWorkItemVm: WorkItemVmBase
	{
		public SelectionWorkItemVm(SelectionItem workItem) : base(workItem)
		{
			WorkItem = workItem;
		}
	}
}
