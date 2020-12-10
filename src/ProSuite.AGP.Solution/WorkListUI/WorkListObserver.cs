using System;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	class WorkListObserver : IWorkListObserver
	{
		[CanBeNull]
		private ProWindow _view { get; set; }

		public IWorkList WorkList { get; private set; }

		[CanBeNull]
		private WorkListViewModelBase ViewModel { get; set; }

		private static bool EnsureWorkListsMatch(IWorkList workList, IWorkList compareWorkList)
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

		public void WorkListAdded(IWorkList workList)
		{
			if (WorkList != null)
			{
				return;
			}

			RunOnUIThread(() =>
			{
				var tuple = WorkListViewFactory.CreateView(workList);
				_view = tuple.Item1;
				ViewModel = tuple.Item2;
				WorkList = workList;

				//if (workList is SelectionWorkList)
				//{
				//	_viewModel = new SelectionWorkListVm(workList);
				//	_view = new WorkListView(_viewModel as SelectionWorkListVm)
				//	        {Title = workList.Name};
				//	_workList = workList;
				//}

				//if (workList is IssueWorkList)
				//{
				//	_viewModel = new IssueWorkListVm(workList);
				//	_view = new IssueWorkListView(_viewModel as IssueWorkListVm)
				//	        {Title = workList.Name};
				//	_workList = workList;
				//}
			});
		}

		public void WorkListRemoved(IWorkList workList)
		{
			if (ViewModel == null) return;

			if (! EnsureWorkListsMatch(workList, ViewModel.CurrentWorkList))
			{
				return;
			}

			RunOnUIThread(() =>
			{
				_view?.Close();
				WorkList = null;
				ViewModel = null;
				_view = null;
			});
		}

		public void WorkListModified(IWorkList workList)
		{
			if (ViewModel == null)
			{
				return;
			}

			if (! EnsureWorkListsMatch(workList, ViewModel.CurrentWorkList))
			{
				return;
			}

			WorkList = workList;
			ViewModel.CurrentWorkList = workList;
		}

		public void Show(IWorkList workList)
		{
			if (ViewModel == null)
			{
				return;
			}

			if (EnsureWorkListsMatch(workList, ViewModel.CurrentWorkList))
			{
				RunOnUIThread(() => _view?.Show());
			}
		}
	}
}
