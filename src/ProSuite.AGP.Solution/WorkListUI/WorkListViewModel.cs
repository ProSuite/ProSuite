using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace Clients.AGP.ProSuiteSolution.WorkListUI
{
	public class WorkListViewModel : PropertyChangedBase //, IWorkListObserver
	{

		public WorkListViewModel(SelectionWorkList workList)
		{
			GoPreviousItemCmd = new RelayCommand(GoPreviousItem, () => true, false,
			                                     true);
			GoNextItemCmd = new RelayCommand(GoNextItem, () => true, false,
			                                     true);
			
			CurrentWorkList = workList;
			CurrentWorkList.GoFirst();
			CurrentWorkItem = CurrentWorkList.Current;

		}

		public WorkListViewModel() { }

		private SelectionWorkList _currentWorkList;
		private IWorkItem _currentWorkItem;
		
		public RelayCommand GoPreviousItemCmd { get; }
		public RelayCommand GoNextItemCmd { get; }

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


		public SelectionWorkList CurrentWorkList
		{
			get => _currentWorkList;
			set { SetProperty(ref _currentWorkList, value, () => CurrentWorkList); }
		}

		public IWorkItem CurrentWorkItem
		{
			get => _currentWorkItem;
			set { SetProperty(ref _currentWorkItem, value, () => CurrentWorkItem); }
		}

		public int CurrentIndex => CurrentWorkList.DisplayIndex;
		
		private void GoPreviousItem()
		{
			CurrentWorkList.GoPrevious();
			CurrentWorkItem = CurrentWorkList.Current;
		}
		private void GoNextItem()
		{
			CurrentWorkList.GoNext();
			CurrentWorkItem = CurrentWorkList.Current;
		}
	}
}
