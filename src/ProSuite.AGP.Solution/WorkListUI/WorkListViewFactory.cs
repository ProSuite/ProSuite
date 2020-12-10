using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public static class WorkListViewFactory
	{
		public static Tuple<ProWindow, WorkListViewModelBase> CreateView(IWorkList workList)
		{
			if (workList is SelectionWorkList)
			{
				var vm = new SelectionWorkListVm(workList);
				var view = new WorkListView(vm as SelectionWorkListVm);
				return new Tuple<ProWindow, WorkListViewModelBase>(view,vm);
			}

			if (workList is IssueWorkList)
			{
				var vm = new IssueWorkListVm(workList);
				var view = new IssueWorkListView(vm as IssueWorkListVm);
				return new Tuple<ProWindow, WorkListViewModelBase>(view, vm);
			}
			else return new Tuple<ProWindow, WorkListViewModelBase>(null,null);
		}
	}
}
