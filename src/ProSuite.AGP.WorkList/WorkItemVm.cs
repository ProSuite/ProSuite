using System;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.WorkList
{
	public class WorkItemVm: PropertyChangedBase
	{
		private IWorkItem _workItem;
		private string  _description;
		private WorkItemStatus _status;
		private bool _visited;

		public WorkItemVm(IWorkItem workItem)
		{
			this.WorkItem = workItem;
			Description = WorkItem.Description;
			Status = WorkItem.Status;
			Visited = WorkItem.Visited;
		}

		public IWorkItem WorkItem
		{
			get { return _workItem; }
			set { _workItem = value; }
		}

		public String Description
		{
			get { return _workItem.Description; }
			set
			{
				_description = value;
				SetProperty(ref _description, value, () => Description);
			}
		}

		public WorkItemStatus Status
		{
			get { return _workItem.Status; }
			set
			{
				_status = value;
				SetProperty(ref _status, value, () => Status);
				_workItem.Status = value;
			}
		}

		public bool Visited
		{
			get { return _workItem.Visited; }
			set
			{
				_visited = value;
				SetProperty(ref _visited, value, () => Visited);
				_workItem.Visited = value;
			}
		}


	}
}
