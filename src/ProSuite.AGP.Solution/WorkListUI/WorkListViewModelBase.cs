using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Solution.WorkLists;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public abstract class WorkListViewModelBase : PropertyChangedBase
	{
		private const double _seconds = 0.3;
		private IWorkList _currentWorkList;
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
		private RelayCommand _goNearestItemCmd;
		private RelayCommand _flashCurrentItemCmd;
		private RelayCommand _flashInvolvedRowCmd;
		private InvolvedObjectRow _selectedInvolvedObject;

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

		//Utility method to consolidate UI update logic
		public void RunOnUIThread(Action action)
		{
			if (FrameworkApplication.Current.Dispatcher.CheckAccess())
				action(); //No invoke needed
			else
				//We are not on the UI
				FrameworkApplication.Current.Dispatcher.BeginInvoke(action);
		}

		public RelayCommand GoNextItemCmd
		{
			get
			{
				_goNextItemCmd = new RelayCommand(() => GoNextItem(), () => true);
				return _goNextItemCmd;
			}
		}

		public RelayCommand GoNearestItemCmd
		{
			get
			{
				_goNearestItemCmd = new RelayCommand(() => GoNearestItem(), () => true);
				return _goNearestItemCmd;
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

		public RelayCommand FlashCurrentItemCmd
		{
			get
			{
				_flashCurrentItemCmd = new RelayCommand(Flash, () => true);
				return _flashCurrentItemCmd;
			}
		}


		public RelayCommand FlashInvolvedRowCmd
		{
			get
			{
				_flashInvolvedRowCmd = new RelayCommand(FlashInvolvedRow, () => SelectedInvolvedObject != null);
				return _flashInvolvedRowCmd;
			}
		}

		private void FlashInvolvedRow()
		{
			if (SelectedInvolvedObject.Name == null || SelectedInvolvedObject.ObjectId < 0)
			{
				return;
			}

			Layer targetLayer = GetLayerByName(SelectedInvolvedObject.Name);
			if (targetLayer == null)
			{
				return;
			}
			MapView.Active.FlashFeature(targetLayer as FeatureLayer, SelectedInvolvedObject.ObjectId);


		}

		public WorkItemStatus Status
		{
			get => CurrentWorkItem.Status;
			set
			{
				if (CurrentWorkItem.Status != value)
				{
					CurrentWorkItem.Status = value;

					// NOTE: has to run inside QueuedTask because it triggers an event
					//		 which does MapView.Active.Invalidate
					QueuedTask.Run(() =>
					{
						CurrentWorkList.SetStatus(CurrentWorkList.Current, value);
					});

					Count = GetCount();
				}

				SetProperty(ref _status, value, () => Status);
			}
		}

		// todo daro: of type IWorkList?
		public IWorkList CurrentWorkList
		{
			get => _currentWorkList;

			set { SetProperty(ref _currentWorkList, value, () => CurrentWorkList); }
		}

		public abstract WorkItemVmBase CurrentWorkItem { get; set; }

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

		public InvolvedObjectRow SelectedInvolvedObject {
			get
			{
				return _selectedInvolvedObject;
			}
			set
			{
				SetProperty(ref _selectedInvolvedObject, value, () => SelectedInvolvedObject);
			}
		}

		public string GetCount()
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
			get => CurrentWorkList.CurrentIndex;
			set { SetProperty(ref _currentIndex, value, () => CurrentIndex); }
		}

		protected void GoPreviousItem()
		{
			QueuedTask.Run(() =>
			{
				CurrentWorkList.GoPrevious();
				CurrentWorkItem = new WorkItemVmBase(CurrentWorkList.Current);
				ZoomTo();
			});
		}

		protected virtual void GoNearestItem()
		{
			QueuedTask.Run(() =>
			{
				CurrentWorkList.GoNearest(CurrentWorkList.Current.Extent);
				CurrentWorkItem = new WorkItemVmBase(CurrentWorkList.Current);
				ZoomTo();
			});
		}

		protected void ZoomTo()
		{
			IWorkItem item = CurrentWorkList.Current;

			if (item == null)
			{
				return;
			}

			MapView.Active.ZoomTo(GetEnvelope(item), TimeSpan.FromSeconds(_seconds));
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

		protected virtual void GoFirstItem()
		{
			QueuedTask.Run(() =>
			{
				CurrentWorkList.GoFirst();
				CurrentWorkItem = new WorkItemVmBase(CurrentWorkList.Current);
				ZoomTo();
			});
		}

		protected virtual void GoNextItem()
		{
			QueuedTask.Run(() =>
			{
				CurrentWorkList.GoNext();
				CurrentWorkItem = new WorkItemVmBase(CurrentWorkList.Current);
				ZoomTo();
			});
		}

		private void PickWorkItem()
		{
			WorkListsModule.Current.WorkItemPicked += Current_WorkItemPicked;
			FrameworkApplication.SetCurrentToolAsync(ConfigIDs.Editing_PickWorkListItemTool);
		}

		private void Current_WorkItemPicked(object sender, WorkItemPickArgs e)
		{
			QueuedTask.Run(() =>
			{
				var OID = e.features.First().GetObjectID();

				QueryFilter filter = GdbQueryUtils.CreateFilter(new[] {OID});
				IWorkItem selectedItem = CurrentWorkList.GetItems(filter).FirstOrDefault();
				foreach (var item in CurrentWorkList.GetItems(null, false))
				{
					Console.WriteLine(item.OID);
					Console.WriteLine(item.Extent.ToJson());
				}

				if (selectedItem == null)
				{
					return;
				}

				CurrentWorkList.GoToOid(selectedItem.OID);
				CurrentWorkItem = new WorkItemVmBase(CurrentWorkList.Current);
			});
		}

		private void Flash()
		{
			QueuedTask.Run(() =>
			{
				var layerName = CurrentWorkList.Current.Proxy.Table.Name;
				var Oid = CurrentWorkList.Current.Proxy.ObjectId;
				Layer targetLayer = GetLayerByName(layerName);
				
				if (targetLayer == null)
				{
					return;
				}

				MapView.Active.FlashFeature(targetLayer as BasicFeatureLayer, Oid);
			});
		}

		[CanBeNull]
		private Layer GetLayerByName(string name)
		{
			return MapView.Active.Map.GetLayersAsFlattenedList()
			                                .FirstOrDefault(layer => layer.Name == name);
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
