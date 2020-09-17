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
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace Clients.AGP.ProSuiteSolution.WorkListUI
{
	public class WorkListViewModel : PropertyChangedBase //, IWorkListObserver
	{

		public WorkListViewModel(SelectionWorkList workList)
		{
			//GoPreviousItemCmd = new RelayCommand(GoPreviousItem, () => true);
			//GoNextItemCmd = new RelayCommand(GoNextItem, () => true, false,true);
			
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


		private ICommand _testCmd;
		public ICommand TestCmd
		{
			get
			{
				if (_testCmd == null)
				{
					_testCmd = new RelayCommand(() =>
					{
						MessageBox.Show("hi");
					}, () => true);
				}
				return _testCmd;
			}
		}

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

		public RelayCommand GoPreviousItemCmd
		{
			get
			{
				_goPreviousItemCdm = new RelayCommand(() => GoPreviousItem(), () => true);
				return _goPreviousItemCdm;
			}
		}

		public SelectionWorkList CurrentWorkList
		{
			get => _currentWorkList;
			set { SetProperty(ref _currentWorkList, value, () => CurrentWorkList); }
		}

		public WorkItemVm CurrentWorkItem
		{
			get => _currentWorkItem;
			set
			{
				//Description = CurrentWorkItem.Description;
				SetProperty(ref _currentWorkItem, value, () => CurrentWorkItem);
			}
		}

		public string Description
		{
			get
			{
				return CurrentWorkItem.Description;
			}
			set
			{
				_description = value;
				SetProperty(ref _description, value, () => Description);
			}
		}

		public int CurrentIndex => CurrentWorkList.DisplayIndex;
		
		private void GoPreviousItem()
		{
			CurrentWorkList.GoPrevious();
			CurrentWorkItem = new WorkItemVm(CurrentWorkList.Current);
		}
		private void GoNextItem()
		{
			CurrentWorkList.GoNext();
			CurrentWorkItem = new WorkItemVm(CurrentWorkList.Current);
		}
	}
}
