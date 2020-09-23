using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace Clients.AGP.ProSuiteSolution.WorkListUI
{
	public class WorkListViewModel : PropertyChangedBase 
	{

		public WorkListViewModel(SelectionWorkList workList)
		{
				CurrentWorkList = workList;
				CurrentWorkList.GoNext();
				CurrentWorkItem = new WorkItemVm(CurrentWorkList.Current);
		}

		public WorkListViewModel() { }

		private SelectionWorkList _currentWorkList;
		private WorkItemVm _currentWorkItem;
		
		
		public ICommand PreviousExtentCmd =>
			FrameworkApplication.GetPlugInWrapper(
				DAML.Button.esri_mapping_prevExtentButton) as ICommand;

		public ICommand NextExtentCmd =>
			FrameworkApplication.GetPlugInWrapper(
				DAML.Button.esri_mapping_nextExtentButton) as ICommand;

		public ICommand ZoomInCmd =>
			FrameworkApplication.GetPlugInWrapper(
				DAML.Button.esri_mapping_fixedZoomInButton) as ICommand;

		public ICommand ZoomOutCmd =>
			FrameworkApplication.GetPlugInWrapper(
				DAML.Button.esri_mapping_fixedZoomOutButton) as ICommand;
		
		
		private RelayCommand _goNextItemCmd;
		public RelayCommand GoNextItemCmd
		{
			get
			{
				_goNextItemCmd = new RelayCommand(()=> GoNextItem(), ()=> true);
				return _goNextItemCmd;
			}
		}

		private RelayCommand _goPreviousItemCmd;
		private RelayCommand _zoomToCmd;
		private int _currentIndex;
		private WorkItemStatus _status;
		private bool _visited;
		private string _count;
		private RelayCommand _panToCmd;

		public RelayCommand GoPreviousItemCmd
		{
			get
			{
				_goPreviousItemCmd = new RelayCommand(() => GoPreviousItem(), () => true);
				return _goPreviousItemCmd;
			}
		}

		public RelayCommand ZoomToCmd
		{
			get
			{
				_zoomToCmd = new RelayCommand(() => ZoomTo(), () => true);
				return _zoomToCmd;
			}
		}

		public RelayCommand PanToCmd
		{
			get
			{
				_panToCmd = new RelayCommand(() => PanTo(), () => true);
				return _panToCmd;
			}
		}

		private void ZoomTo()
		{
			QueuedTask.Run(() =>
			{
				MapView.Active.ZoomTo(CurrentWorkList.Current.Extent);
			});

		}

		private void PanTo()
		{
			QueuedTask.Run(() =>
			{
				MapView.Active.PanTo(CurrentWorkList.Current.Extent);
			});

		}

		public WorkItemStatus Status
		{
			get { return CurrentWorkItem.Status;}
			set
			{
				CurrentWorkItem.Status = value;
				CurrentWorkList.Update(CurrentWorkList.Current);
				SetProperty(ref _status, value, () => Status);
			}
		}
		
		public SelectionWorkList CurrentWorkList
		{
			get => _currentWorkList;

			set { SetProperty(ref _currentWorkList, value, () => CurrentWorkList); }
		}

		public WorkItemVm CurrentWorkItem
		{
			get { return new WorkItemVm(CurrentWorkList.Current); }
			set
			{
				SetProperty(ref _currentWorkItem, value, () => CurrentWorkItem);
				Status = CurrentWorkItem.Status;
				Visited = CurrentWorkItem.Visited;
				CurrentIndex = CurrentWorkList.DisplayIndex;
				Count = GetCount();
			}
		}

		public bool Visited
		{
			get { return CurrentWorkItem.Visited; }
			set
			{
				CurrentWorkItem.Visited = value;
				SetProperty(ref _visited, value, () => Visited);
			}
		}

		public IList<WorkItemVisibility> Visibility
		{
			get
			{
				return Enum.GetValues(typeof(WorkItemVisibility)).Cast<WorkItemVisibility>().ToList<WorkItemVisibility>();
			}
			set { }
		
		}

		private string GetCount()
		{
			var all = CurrentWorkList.Count(null, true);
			var toDo = CurrentWorkList
			           .GetItems(null, true).Count(item => item.Status == WorkItemStatus.Todo);
			return $"{CurrentIndex + 1} of {all} ({toDo} todo, {all} total)";
		}

		public string Count
		{
			get => _count;
			set { SetProperty(ref _count, value, ()=> Count); }
		}

		public int CurrentIndex
		{
			get { return CurrentWorkList.DisplayIndex; }
			set
			{
				SetProperty(ref _currentIndex, value, () => CurrentIndex);
			}
		}

		
		private void GoPreviousItem()
		{
			QueuedTask.Run(() =>
			{
				CurrentWorkList.GoPrevious();
				CurrentWorkItem = new WorkItemVm(CurrentWorkList.Current);
			});

		}
		private void GoNextItem()
		{
			QueuedTask.Run(() =>
			{
				CurrentWorkList.GoNext();
				CurrentWorkItem = new WorkItemVm(CurrentWorkList.Current);
			});

		}
	}
}
