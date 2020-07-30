using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace Clients.AGP.ProSuiteSolution.WorkListUI
{
	public class WorkListViewModel : PropertyChangedBase, IWorkListObserver
	{
		public WorkListViewModel()
		{
			GoPreviousItemCmd = new RelayCommand(GoPreviousItem, () => true, false,
			                                     true);
			GoNextItemCmd = new RelayCommand(GoNextItem, () => true, false,
			                                     true);
			WorkListCentral = new WorkListCentral();
			WorkListCentral.RegisterObserver(this);
			WorkLists = WorkListCentral.GetAllLists();
			CurrentWorkList = WorkLists.First();
			CurrentWorkItem = CurrentWorkList.GetItems().First();
		}

		private IWorkList _currentWorkList;
		private IEnumerable<IWorkList> _workLists;
		private IWorkItem _currentWorkItem;
		public WorkListCentral WorkListCentral { get; }
		public RelayCommand GoPreviousItemCmd { get; }
		public RelayCommand GoNextItemCmd { get; }

		public IEnumerable<IWorkList> WorkLists
		{
			get => _workLists;
			set { SetProperty(ref _workLists, value, () => WorkLists); }
		}
		public IWorkList CurrentWorkList
		{
			get => _currentWorkList;
			set { SetProperty(ref _currentWorkList, value, () => CurrentWorkList); }
		}

		public IWorkItem CurrentWorkItem
		{
			get => _currentWorkItem;
			set { SetProperty(ref _currentWorkItem, value, () => CurrentWorkItem); }
		}

		public void WorkListAdded(IWorkList workList)
		{
			//TODO test this with a test button that adds another worklist with testitems
			throw new NotImplementedException();
		}

		public void WorkListRemoved(IWorkList workList)
		{
			throw new NotImplementedException();
		}

		public void WorkListModified(IWorkList workList)
		{
			throw new NotImplementedException();
		}

		private void GoPreviousItem()
		{
			CurrentWorkList.GoPrevious();
			CurrentWorkItem = CurrentWorkList.Current;
			CurrentWorkItem.SetVisited(); //TODO this should be done by the worklist itself when moving to next/previous
		}
		private void GoNextItem()
		{
			CurrentWorkList.GoNext();
			CurrentWorkItem = CurrentWorkList.Current;
			CurrentWorkItem.SetVisited(); //TODO this should be done by the worklist itself when moving to next/previous
		}
	}
}
