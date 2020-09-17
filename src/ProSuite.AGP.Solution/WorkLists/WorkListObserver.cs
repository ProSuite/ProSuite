using System;
using System.Collections.Generic;
using ArcGIS.Desktop.Framework;
using Clients.AGP.ProSuiteSolution.WorkListUI;
using ProSuite.AGP.Solution.WorkListUI;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class WorkListObserver: IWorkListObserver
	{

		public List<WorkListViewContext> viewContexts { get; set; } = new List<WorkListViewContext>();
		private Object _lockObj = new Object();

		public void WorkListAdded(IWorkList workList)
		{
			WorkListViewContext context;

			if (TryGetContextByWorkListName(workList.Name, out context))
			{
				var viewModel = context.View.DataContext as WorkListViewModel;
				viewModel.CurrentWorkList = workList as SelectionWorkList;
			}
			else
			{
				WorkListViewModel vm = new WorkListViewModel(workList as SelectionWorkList);

				WorkListView view = new WorkListView(vm);
				view.Title = workList.Name;

				viewContexts.Add(new WorkListViewContext(view));
			}
		}

		public void WorkListRemoved(IWorkList workList)
		{
			WorkListViewContext viewContext;
			if (TryGetContextByWorkListName(workList.Name, out viewContext))
			{
				viewContexts.Remove(viewContext);
			}
		}

		public void WorkListModified(IWorkList workList)
		{
			WorkListViewContext context;
			if (TryGetContextByWorkListName(workList.Name, out context))
			{
				var viewModel = context.View.DataContext as WorkListViewModel;
				viewModel.CurrentWorkList = workList as SelectionWorkList;
			}
		}

		public void Show(IWorkList workList)
		{
			WorkListViewContext context;
			if (TryGetContextByWorkListName(workList.Name, out context)) 
			{
				showView(context);
			}
			else
			{
				WorkListViewModel vm = new WorkListViewModel(workList as SelectionWorkList);

				WorkListView view = new WorkListView(vm);

				WorkListViewContext newContext = new WorkListViewContext(view);

				viewContexts.Add(newContext);

				showView(context);
			}
		}

		private bool TryGetContextByWorkListName(string name, out WorkListViewContext viewContext)
		{
			viewContext = viewContexts.Find(context =>
			{
				var vm = context.View.DataContext as WorkListViewModel;
				return vm.CurrentWorkList.Name == name;
			});
			if (viewContext != null)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private void showView(WorkListViewContext viewContext)
		{
			if (viewContext.ViewIsVisible)
			{
				return;

			}
			viewContext.View.Owner = FrameworkApplication.Current.MainWindow;
			viewContext.View.Show();
			viewContext.View.Closed += View_Closed;
			viewContext.ViewIsVisible = true;
		}

		private void View_Closed(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}
	}
}
