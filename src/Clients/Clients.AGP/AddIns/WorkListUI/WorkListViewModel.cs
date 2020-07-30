using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace Clients.AGP.ProSuiteSolution.WorkListUI
{
	public class WorkListViewModel : PropertyChangedBase
	{
		private WorkList _workList;
		private WorkItem _workItem;

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
	}
}
