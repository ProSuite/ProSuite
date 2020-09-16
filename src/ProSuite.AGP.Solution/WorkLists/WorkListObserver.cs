using System;
using System.Collections.Generic;
using ArcGIS.Desktop.Framework;
using Clients.AGP.ProSuiteSolution.WorkListUI;
using ProSuite.AGP.Solution.WorkListUI;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class WorkListObserver: IWorkListObserver
	{

		public List<WorkListViewContext> viewContexts { get; set; } = new List<WorkListViewContext>();
		private Object _lockObj = new Object();

		public void WorkListAdded(IWorkList workList)
		{
			WorkListViewModel vm = new WorkListViewModel(workList);

			viewContexts.Add(new WorkListViewContext()
			                 {
								 
								 view = new WorkListView(vm),
								 ViewIsVisible = false
			                 });
		}

		public void WorkListRemoved(IWorkList workList)
		{
			throw new System.NotImplementedException();
		}

		public void WorkListModified(IWorkList workList)
		{
			throw new System.NotImplementedException();
		}

		public void Show(IWorkList workList)
		{
			if (GetContextByWorkListName(workList.Name) == null)
			{
				WorkListViewModel vm = new WorkListViewModel(workList);

				viewContexts.Add(new WorkListViewContext()
				                 {
					                 view = new WorkListView(vm),
					                 ViewIsVisible = false

				                 });
			}

			var context = GetContextByWorkListName(workList.Name);
			var view = context.view;
			
			showView(context);

			context.ViewIsVisible = true;

		}

		private WorkListViewContext GetContextByWorkListName(string name)
		{
			return viewContexts.Find(context =>
			{
				var vm = context.view.DataContext as WorkListViewModel;
				return vm.CurrentWorkList.Name == name;
			});
		}

		private void showView(WorkListViewContext viewContext)
		{
			viewContext.view.Owner = FrameworkApplication.Current.MainWindow;
			viewContext.view.Show();
			viewContext.ViewIsVisible = true;
		}

	}
}
