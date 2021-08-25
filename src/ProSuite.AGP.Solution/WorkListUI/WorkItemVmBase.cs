using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public abstract class WorkItemVmBase : PropertyChangedBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		[NotNull] private readonly IWorkItem _workItem;
		[NotNull] private readonly IWorkList _workList;
		private readonly string _description;
		private WorkItemStatus _status;
		private bool _visited;

		// ReSharper disable once NotNullMemberIsNotInitialized
		protected WorkItemVmBase() { }

		protected WorkItemVmBase([NotNull] IWorkItem workItem, [NotNull] IWorkList workList)
		{
			Assert.ArgumentNotNull(workItem, nameof(workItem));
			Assert.ArgumentNotNull(workList, nameof(workList));

			_workItem = workItem;
			_workList = workList;

			_description = workItem.Description;
			_status = workItem.Status;
			_visited = workItem.Visited;
		}

		[CanBeNull]
		public string Description => _description;

		public WorkItemStatus Status
		{
			get => _status;
			set
			{
				// don't set status if it doesn't changes
				if (_status == value)
				{
					return;
				}

				ViewUtils.Try(
					() =>
					{
						_workItem.Status = value;

						QueuedTask.Run(() => { _workList.SetStatus(_workItem, value); });
					},
					_msg);

				SetProperty(ref _status, value, () => Status);

				Project.Current.SetDirty();
			}
		}

		public bool Visited
		{
			get => _visited;
			set { SetProperty(ref _visited, value, () => Visited); }
		}
	}
}
