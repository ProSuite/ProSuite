using System;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.WorkList
{
	public class WorkItemVm: PropertyChangedBase
	{
		private WorkItemStatus _status;
		private bool _visited;

		public WorkItemVm(IWorkItem workItem)
		{
			this.WorkItem = workItem;
		}

		private IWorkItem WorkItem { get; set; }
		

		public String Description
		{
			get { return WorkItem.Description; }
			set { }
		}

		public WorkItemStatus Status
		{
			get { return WorkItem.Status; }
			set
			{
				_status = value;
				SetProperty(ref _status, value, () => Status);
				WorkItem.Status = value;
			}
		}

		public bool Visited
		{
			get { return WorkItem.Visited; }
			set
			{
				_visited = value;
				SetProperty(ref _visited, value, () => Visited);
				WorkItem.Visited = value;
			}
		}


	}
}
