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
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace Clients.AGP.ProSuiteSolution.WorkListUI
{
	public class WorkListViewModel : PropertyChangedBase //, IWorkListObserver
	{

		public WorkListViewModel(SelectionWorkList workList)
		{
				CurrentWorkList = workList;
				CurrentWorkList.GoFirst();
				CurrentWorkItem = new WorkItemVm(CurrentWorkList.Current);
		}

		public WorkListViewModel() { }

		private SelectionWorkList _currentWorkList;
		private WorkItemVm _currentWorkItem;
		
		//public RelayCommand GoPreviousItemCmd { get; }
		//public RelayCommand GoNextItemCmd { get; }

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

		private RelayCommand _goPreviousItemCdm;
		private string _description;
		
		private int _count;
		private int _currentIndex;
		private WorkItemStatus _status;
		private bool _visited;

		public RelayCommand GoPreviousItemCmd
		{
			get
			{
				_goPreviousItemCdm = new RelayCommand(() => GoPreviousItem(), () => true);
				return _goPreviousItemCdm;
			}
		}

		public WorkItemStatus Status
		{
			get { return CurrentWorkItem.Status;}
			set
			{
				CurrentWorkItem.Status = value;
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
		
		}

		public int Count
		{
			get { return CurrentWorkList.Count();}
			set { }

		}

		public int CurrentIndex
		{
			get { return CurrentWorkList.DisplayIndex; }
			set { SetProperty(ref _currentIndex, value, () => CurrentIndex); }
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
