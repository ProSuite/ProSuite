using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class IssueWorkListVm: WorkListViewModelBase
	{
		private WorkListView _view;
		private readonly bool _hasDetailSection;

		public IssueWorkListVm(IWorkList workList)
		{
			CurrentWorkList = workList;
			CurrentWorkList.GoNext();
			CurrentWorkItem = new WorkItemVm(CurrentWorkList.Current);
			_hasDetailSection = true;
		}

		public override bool HasDetailSection
		{
			get => _hasDetailSection;
		}

		//protected override WorkListView View
		//{
		//	get => _view;
		//	set => _view = value;
		//}

		//public override void Show(IWorkList workList)
		//{
		//	throw new NotImplementedException();
		//}
	}
}
