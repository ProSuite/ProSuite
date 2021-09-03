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
	public abstract class WorkItemViewModelBase : PropertyChangedBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private readonly IWorkItem _workItem;
		[CanBeNull] private readonly WorkListViewModelBase _viewModel;
		[CanBeNull] private readonly string _description;
		private WorkItemStatus _status;

		// ReSharper disable once NotNullMemberIsNotInitialized
		protected WorkItemViewModelBase() { }

		protected WorkItemViewModelBase([NotNull] IWorkItem workItem,
		                                [NotNull] WorkListViewModelBase viewModel)
		{
			Assert.ArgumentNotNull(workItem, nameof(workItem));
			Assert.ArgumentNotNull(viewModel, nameof(viewModel));

			_workItem = workItem;
			_viewModel = viewModel;

			_description = workItem.Description;
			_status = workItem.Status;
		}

		[CanBeNull]
		public virtual string Description => _description;

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

						IWorkList worklist = _viewModel?.CurrentWorkList;

						QueuedTask.Run(() => { worklist?.SetStatus(_workItem, value); });
						          //.ContinueWith(t => _viewModel.GoNearestCore());

						// todo daro: make async
						// todo daro: create an event aggregator that propagates this through the whole application?
						//_viewModel.GoNearestCore();
					},
					_msg);

				SetProperty(ref _status, value, () => Status);

				Project.Current.SetDirty();
			}
		}
	}
}
