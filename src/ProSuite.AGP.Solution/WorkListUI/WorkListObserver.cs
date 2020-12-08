using System;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	class WorkListObserver : IWorkListObserver
	{
		[CanBeNull]
		private ProWindow _view { get; set; }

		[CanBeNull]
		private WorkListViewModelBase _viewModel { get; set; }

		public void WorkListAdded(IWorkList workList)
		{
			RunOnUIThread(() =>
			{
				if (workList is SelectionWorkList)
				{
					_viewModel = new SelectionWorkListVm(workList);
					_view = new WorkListView(_viewModel as SelectionWorkListVm)
					        {Title = workList.Name};
				}

				if (workList is IssueWorkList)
				{
					_viewModel = new IssueWorkListVm(workList);
					_view = new IssueWorkListView(_viewModel as IssueWorkListVm)
					        {Title = workList.Name};
				}
			});
		}

		public void WorkListRemoved(IWorkList workList)
		{
			if (_viewModel == null) return;

			if (! ensureWorkListsMatch(workList, _viewModel.CurrentWorkList))
			{
				return;
			}

			RunOnUIThread(() =>
			{
				_view?.Close();
				_viewModel = null;
				_view = null;
			});
		}

		public void WorkListModified(IWorkList workList)
		{
			if (_viewModel == null)
			{
				return;
			}

			if (! ensureWorkListsMatch(workList, _viewModel.CurrentWorkList))
			{
				return;
			}

			_viewModel.CurrentWorkList = workList;
		}

		public void Show(IWorkList workList)
		{
			if (_viewModel == null)
			{
				return;
			}

			if (ensureWorkListsMatch(workList, _viewModel.CurrentWorkList))
			{
				RunOnUIThread(() => _view?.Show());
			}
		}

		private bool ensureWorkListsMatch(IWorkList workList, IWorkList compareWorkList)
		{
			if (workList != null && compareWorkList != null)
			{
				return workList.Name == compareWorkList.Name;
			}

			return false;
		}

		//Utility method to consolidate UI update logic
		private static void RunOnUIThread(Action action)
		{
			if (FrameworkApplication.Current.Dispatcher.CheckAccess())
				action(); //No invoke needed
			else
				//We are not on the UI
				FrameworkApplication.Current.Dispatcher.BeginInvoke(action);
		}
	}
}
