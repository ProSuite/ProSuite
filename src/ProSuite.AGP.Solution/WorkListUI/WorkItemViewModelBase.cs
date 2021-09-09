using System.Windows.Input;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
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

		[CanBeNull] private readonly IWorkList _workList;
		[CanBeNull] private readonly IWorkItem _workItem;

		// ReSharper disable once NotNullMemberIsNotInitialized
		protected WorkItemViewModelBase() { }

		protected WorkItemViewModelBase([NotNull] IWorkItem workItem,
		                                [NotNull] IWorkList workList)
		{
			Assert.ArgumentNotNull(workItem, nameof(workItem));
			Assert.ArgumentNotNull(workList, nameof(workList));

			_workItem = workItem;
			_workList = workList;
		}

		[CanBeNull]
		public virtual string Description => _workItem?.Description;

		public WorkItemStatus Status
		{
			get => _workItem?.Status ?? WorkItemStatus.Unknown;
			private set
			{
				if (_workItem == null)
				{
					return;
				}

				_workItem.Status = value;

				NotifyPropertyChanged(nameof(Status));
			}
		}

		public ICommand SetStatusCommand =>
			new RelayCommand(SetStatusAsync, () => _workItem != null);

		private async void SetStatusAsync(object parameter)
		{
			if (_workItem == null)
			{
				return;
			}

			var status = (WorkItemStatus) parameter;

			Project.Current.SetDirty();

			await ViewUtils.TryAsync(() =>
			{
				IWorkList worklist = _workList;

				return QueuedTask.Run(() => { worklist?.SetStatus(_workItem, status); });
			}, _msg);
		}
	}
}
