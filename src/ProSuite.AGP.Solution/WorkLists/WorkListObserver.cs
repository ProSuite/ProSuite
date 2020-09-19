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

		public List<WorkListViewContext> ViewContexts { get; set; } = new List<WorkListViewContext>();
		//public List<WorkListViewModel> viewModels { get; set; } = new List<WorkListViewModel>();
		private Object _lockObj = new Object();

		public void WorkListAdded(IWorkList workList)
		{
			WorkListViewContext context;

			if (TryGetContextByWorkListName(workList.Name, out context))
			{
				var viewModel = context.ViewModel;
				viewModel.CurrentWorkList = workList as SelectionWorkList;
			}
			else
			{
				WorkListViewModel vm = new WorkListViewModel(workList as SelectionWorkList);

				ViewContexts.Add(new WorkListViewContext(vm));
			}
		}

		public void WorkListRemoved(IWorkList workList)
		{
			WorkListViewContext viewContext;
			if (TryGetContextByWorkListName(workList.Name, out viewContext))
			{
				ViewContexts.Remove(viewContext);
			}
		}

		public void WorkListModified(IWorkList workList)
		{
			WorkListViewContext context;
			if (TryGetContextByWorkListName(workList.Name, out context))
			{
				context.ViewModel.CurrentWorkList = workList as SelectionWorkList;
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

				WorkListViewContext newContext = new WorkListViewContext(vm);

				ViewContexts.Add(newContext);

				showView(context);
			}
		}

		private bool TryGetContextByWorkListName(string name, out WorkListViewContext viewContext)
		{
			viewContext = ViewContexts.Find(context => context.ViewModel.CurrentWorkList.Name == name);
			return viewContext != null;
		}

		private void showView(WorkListViewContext viewContext)
		{
			if (viewContext.ViewIsVisible)
			{
				return;

			}
			WorkListView view = new WorkListView(viewContext.ViewModel);
			view.Owner = FrameworkApplication.Current.MainWindow;
			view.Show();
			view.Closed += View_Closed;
			viewContext.ViewIsVisible = true;
		}

		private void View_Closed(object sender, EventArgs e)
		{
			//TODO unload worklist layer here.
			
			var view = sender as WorkListView;
			var viewModel = view.DataContext as WorkListViewModel;
			WorkListViewContext context;
			if (TryGetContextByWorkListName(viewModel.CurrentWorkList.Name, out context))
			{
				context.ViewIsVisible = false;
			}
			WorkListsModule.Current.RemoveWorkListLayer(viewModel.CurrentWorkList);
				 
		}
	}
}
