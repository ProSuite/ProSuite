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
		private const string NoItem = "no current item";

		public WorkItemVm(IWorkItem workItem)
		{
			this.WorkItem = workItem;
		}

		private IWorkItem WorkItem { get; set; }
		

		public String Description
		{
			get
			{
				return WorkItem == null ? NoItem : WorkItem.Description;
			}
			set { }
		}

		public WorkItemStatus Status
		{
			get
			{
				
				return WorkItem == null ? WorkItemStatus.Todo: WorkItem.Status;
			}
			set
			{
				if (WorkItem != null)
				{
					WorkItem.Status = value;
				}
				
				SetProperty(ref _status, value, () => Status);
			}
		}

		public bool Visited
		{
			get
			{
				return WorkItem == null ? false: WorkItem.Visited;
			}
			set
			{
				if (WorkItem != null)
				{
					WorkItem.Visited = value;
				}
				SetProperty(ref _visited, value, () => Visited);
			}
		}


	}
}
