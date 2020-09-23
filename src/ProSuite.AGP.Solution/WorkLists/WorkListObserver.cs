using System;
using System.Collections.Generic;
using System.Windows;
using ArcGIS.Desktop.Framework;
using Clients.AGP.ProSuiteSolution.WorkListUI;
using ProSuite.AGP.Solution.WorkListUI;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class WorkListObserver : IWorkListObserver
	{
		public List<WorkListViewContext> ViewContexts { get; set; } =
			new List<WorkListViewContext>();

		public void WorkListAdded(IWorkList workList)
		{
			WorkListViewContext context;

			if (TryGetContextByWorkListName(workList.Name, out context))
			{
				WorkListViewModel viewModel = context.ViewModel;
				viewModel.CurrentWorkList = workList as SelectionWorkList;
			}
			else
			{
				var vm = new WorkListViewModel(workList as SelectionWorkList);

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
				var vm = new WorkListViewModel(workList as SelectionWorkList);

				var newContext = new WorkListViewContext(vm);

				ViewContexts.Add(newContext);

				showView(context);
			}
		}

		private bool TryGetContextByWorkListName(string name, out WorkListViewContext viewContext)
		{
			viewContext =
				ViewContexts.Find(context => context.ViewModel.CurrentWorkList.Name == name);
			return viewContext != null;
		}

		private void showView(WorkListViewContext viewContext)
		{
			if (viewContext.ViewIsVisible)
			{
				return;
			}

			var view = new WorkListView(viewContext.ViewModel);
			view.Owner = Application.Current.MainWindow;
			view.Show();
			view.Closed += View_Closed;
			viewContext.ViewIsVisible = true;
		}

		private void View_Closed(object sender, EventArgs e)
		{
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
