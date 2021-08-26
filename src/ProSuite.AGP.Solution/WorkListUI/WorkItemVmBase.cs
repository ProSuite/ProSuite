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
		[CanBeNull] private readonly IWorkItem _workItem;
		[NotNull] private readonly IWorkList _workList;
		private WorkItemStatus _status;

		// ReSharper disable once NotNullMemberIsNotInitialized
		protected WorkItemVmBase() { }

		protected WorkItemVmBase([NotNull] IWorkItem workItem, [NotNull] IWorkList workList)
		{
			Assert.ArgumentNotNull(workItem, nameof(workItem));
			Assert.ArgumentNotNull(workList, nameof(workList));

			_workItem = workItem;
			_workList = workList;

			Description = workItem.Description;
			_status = workItem.Status;
		}

		[CanBeNull]
		public string Description { get; }

		public bool CanSetStatus => _workItem != null;

		public WorkItemStatus Status
		{
			get => _status;
			set
			{
				// don't set status if it doesn't changes
				if (_status == value || _workItem == null)
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
	}
}
