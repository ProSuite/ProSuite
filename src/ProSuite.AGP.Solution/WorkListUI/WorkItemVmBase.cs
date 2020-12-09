using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class WorkItemVmBase : PropertyChangedBase
	{
		private WorkItemStatus _status;
		private bool _visited;
		private const string NoItem = "no current item";

		public WorkItemVmBase(IWorkItem workItem)
		{
			WorkItem = workItem;
		}

		protected IWorkItem WorkItem { get; set; }

		public string Description
		{
			get { return WorkItem == null ? NoItem : WorkItem.Description; }
			set { }
		}

		public WorkItemStatus Status
		{
			get { return WorkItem?.Status ?? WorkItemStatus.Todo; }
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
			get { return WorkItem?.Visited ?? false; }
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
