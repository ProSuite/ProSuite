using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Solution.Selection;
using ProSuite.AGP.Solution.WorkLists;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class WorkListViewModel : PropertyChangedBase, IWorkListObserver
	{
		private const double _seconds = 0.3;
		private SelectionWorkList _currentWorkList;
		private WorkItemVm _currentWorkItem;
		private int _currentIndex;
		private WorkItemStatus _status;
		private bool _visited;
		private string _count;
		private RelayCommand _goNextItemCmd;
		private RelayCommand _goFirstItemCmd;
		private RelayCommand _goPreviousItemCmd;
		private RelayCommand _zoomToCmd;
		private RelayCommand _panToCmd;
		private RelayCommand _zoomToAllCmd;
		private RelayCommand _pickWorkItemCmd;

		public WorkListViewModel(SelectionWorkList workList)
		{
			CurrentWorkList = workList;
			CurrentWorkList.GoNext();
			CurrentWorkItem = new WorkItemVm(CurrentWorkList.Current);
		}

		public ICommand ClearSelectionCmd =>
			FrameworkApplication.GetPlugInWrapper(
				DAML.Button.esri_mapping_clearSelectionButton) as ICommand;

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

		
		public RelayCommand GoNextItemCmd
		{
			get
			{
				_goNextItemCmd = new RelayCommand(() => GoNextItem(), () => true);
				return _goNextItemCmd;
			}
		}

		public RelayCommand GoFirstItemCmd
		{
			get
			{
				_goFirstItemCmd = new RelayCommand(() => GoFirstItem(), () => true);
				return _goFirstItemCmd;
			}
		}

		public RelayCommand ZoomToAllCmd
		{
			get
			{
				_zoomToAllCmd = new RelayCommand(() => ZoomToAllAsync(), () => true);
				return _zoomToAllCmd;
			}
		}

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
				_zoomToCmd = new RelayCommand(ZoomToAsync, () => true);
				return _zoomToCmd;
			}
		}

		public RelayCommand PanToCmd
		{
			get
			{
				_panToCmd = new RelayCommand(PanToAsync, () => true);
				return _panToCmd;
			}
		}

		public RelayCommand PickWorkItemCmd
		{
			get
			{
				_pickWorkItemCmd = new RelayCommand(PickWorkItem, () => true);
				return _pickWorkItemCmd;
			}
		}

		

		public WorkItemStatus Status
		{
			get => CurrentWorkItem.Status;
			set
			{
				CurrentWorkItem.Status = value;

				// NOTE: has to run inside QueuedTask because it triggers an event
				//		 which does MapView.Active.Invalidate
				QueuedTask.Run(() => { CurrentWorkList.Update(CurrentWorkList.Current); });

				SetProperty(ref _status, value, () => Status);
			}
		}

		// todo daro: of type IWorkList?
		public SelectionWorkList CurrentWorkList
		{
			get => _currentWorkList;

			set { SetProperty(ref _currentWorkList, value, () => CurrentWorkList); }
		}

		public WorkItemVm CurrentWorkItem
		{
			get => new WorkItemVm(CurrentWorkList.Current);
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
			get => CurrentWorkItem.Visited;
			set
			{
				CurrentWorkItem.Visited = value;
				SetProperty(ref _visited, value, () => Visited);
			}
		}

		public IList<WorkItemVisibility> Visibility
		{
			get => Enum.GetValues(typeof(WorkItemVisibility)).Cast<WorkItemVisibility>()
			           .ToList();
			set { }
		}

		private string GetCount()
		{
			int all = CurrentWorkList.Count(null, true);
			int toDo = CurrentWorkList
			           .GetItems(null, true).Count(item => item.Status == WorkItemStatus.Todo);
			return $"{CurrentIndex + 1} of {all} ({toDo} todo, {all} total)";
		}

		public string Count
		{
			get => _count;
			set { SetProperty(ref _count, value, () => Count); }
		}

		public int CurrentIndex
		{
			get => CurrentWorkList.DisplayIndex;
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

		private async Task ZoomToAsync()
		{
			IWorkItem item = CurrentWorkList.Current;

			if (item == null)
			{
				return;
			}

			await MapView.Active.ZoomToAsync(GetEnvelope(item), TimeSpan.FromSeconds(_seconds));
		}

		private async Task PanToAsync()
		{
			IWorkItem item = CurrentWorkList.Current;

			if (item == null)
			{
				return;
			}

			await MapView.Active.PanToAsync(GetEnvelope(item), TimeSpan.FromSeconds(_seconds));
		}

		private async Task ZoomToAllAsync()
		{
			await MapView.Active.ZoomToAsync(CurrentWorkList.Extent);
		}

		private void GoFirstItem()
		{
			QueuedTask.Run(() =>
			{
				CurrentWorkList.GoFirst();
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

		private void PickWorkItem()
		{
			WorkListsModule.Current.WorkItemPicked += Current_WorkItemPicked;
			FrameworkApplication.SetCurrentToolAsync("ProSuiteTools_PickWorkListItemTool");
			
		}

		private void Current_WorkItemPicked(object sender, WorkItemPickArgs e)
		{
			QueuedTask.Run(() =>
			{
				//var shapeJson = e.features.First().GetShape().ToJson();
				var OID = e.features.First().GetObjectID();
				IWorkItem selectedItem = CurrentWorkList.GetItems().FirstOrDefault(item => item.OID == OID);
				foreach (var item in CurrentWorkList.GetItems())
				{
					Console.WriteLine(item.OID);
					Console.WriteLine(item.Extent.ToJson());
				}

				if (selectedItem == null)
				{
					return;
				}

				CurrentWorkList.GoToOid(selectedItem.OID);
				CurrentWorkItem = new WorkItemVm(CurrentWorkList.Current);

			});
		}

		public void WorkListAdded(IWorkList workList)
		{
			//throw new NotImplementedException();
		}

		public void WorkListRemoved(IWorkList workList)
		{
			//throw new NotImplementedException();
		}

		public void WorkListModified(IWorkList workList)
		{
			//throw new NotImplementedException();
		}

		public void Show(IWorkList workList)
		{
			var view = new WorkListView(this);
			view.Owner = Application.Current.MainWindow;
			view.Title = workList.Name;
			view.Show();
			view.Closed += View_Closed;
		}

		private void View_Closed(object sender, EventArgs e)
		{
			//WorkListsModule.Current.RemoveWorkListLayer(CurrentWorkList);
			WorkListsModule.Current.UnregisterObserver(this);
		}

		[NotNull]
		private static Envelope GetEnvelope([NotNull] IWorkItem item)
		{
			item.QueryPoints(out double xmin, out double ymin,
			                 out double xmax, out double ymax,
			                 out double zmax);

			return EnvelopeBuilder.CreateEnvelope(new Coordinate3D(xmin, ymin, zmax),
			                                      new Coordinate3D(xmax, ymax, zmax),
			                                      item.Extent.SpatialReference);
		}
	}
}
