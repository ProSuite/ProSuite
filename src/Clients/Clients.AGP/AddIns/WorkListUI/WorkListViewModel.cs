using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace Clients.AGP.ProSuiteSolution.WorkListUI
{
	public class WorkListViewModel : PropertyChangedBase, IWorkListObserver
	{
		public WorkListViewModel()
		{
			WorkListCentral = new WorkListCentral();
			WorkListCentral.RegisterObserver(this);
		}

		private WorkList _workList;
		private WorkItem _workItem;
		public WorkListCentral WorkListCentral { get; }

		public WorkList WorkList
		{
			get => _workList;
			set { SetProperty(ref _workList, value, () => WorkList); }
		}

		public WorkItem CurrentWorkItem
		{
			get => _workItem;
			set { SetProperty(ref _workItem, value, () => CurrentWorkItem); }
		}

		public void WorkListAdded(IWorkList workList)
		{
			throw new NotImplementedException();
		}

		public void WorkListRemoved(IWorkList workList)
		{
			throw new NotImplementedException();
		}

		public void WorkListModified(IWorkList workList)
		{
			throw new NotImplementedException();
		}
	}
}
