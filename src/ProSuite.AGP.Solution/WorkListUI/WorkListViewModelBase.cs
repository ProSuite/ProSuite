using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Solution.WorkLists;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

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
		private RelayCommand _flashCurrentFeatureCmd;
		private InvolvedObjectRow _selectedInvolvedObject;

		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private RelayCommand _selectCurrentFeatureCmd;
		private string _lastActiveTool = null;

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
				_goNextItemCmd = new RelayCommand(execute: () => GoNextItem(),
				                                  canExecute: () => CurrentWorkList.CanGoNext());
				return _goNextItemCmd;
			}
		}

		public RelayCommand GoNearestItemCmd
		{
			get
			{
				_goNearestItemCmd =
					new RelayCommand(() => GoNearestItem(), () => CurrentWorkList.CanGoNearest());
				return _goNearestItemCmd;
			}
		}

		public RelayCommand GoFirstItemCmd
		{
			get
			{
				_goFirstItemCmd =
					new RelayCommand(() => GoFirstItem(), () => CurrentWorkList.CanGoFirst());
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
				_goPreviousItemCmd =
					new RelayCommand(() => GoPreviousItem(), () => CurrentWorkList.CanGoPrevious());
				return _goPreviousItemCmd;
			}
		}

		public ICommand ZoomToCmd
		{
			get
			{
				_zoomToCmd = new RelayCommand(ZoomToAsync, () => CurrentWorkList.Current != null);
				return _zoomToCmd;
			}
		}

		public RelayCommand PanToCmd
		{
			get
			{
				_panToCmd = new RelayCommand(PanToAsync, () => CurrentWorkList.Current != null);
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

		public RelayCommand SelectCurrentFeatureCmd
		{
			get
			{
				_selectCurrentFeatureCmd =
					new RelayCommand(SelectCurrentFeature, () => CurrentWorkList.Current != null);
				return _selectCurrentFeatureCmd;
			}
		}

		public RelayCommand FlashCurrentFeatureCmd
		{
			get
			{
				_flashCurrentFeatureCmd =
					new RelayCommand(FlashCurrentFeature, () => CurrentWorkList.Current != null);
				return _flashCurrentFeatureCmd;
			}
		}

		public WorkItemStatus Status
		{
			get => CurrentWorkItem.Status;
			set
			{
				if (CurrentWorkItem.Status != value && CurrentWorkList.Current != null)
				{
					CurrentWorkItem.Status = value;

					// NOTE: has to run inside QueuedTask because it triggers an event
					//		 which does MapView.Active.Invalidate
					QueuedTask.Run(() =>
					{
						CurrentWorkList.SetStatus(CurrentWorkList.Current, value);
					});

					Project.Current.SetDirty();

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

		public InvolvedObjectRow SelectedInvolvedObject
		{
			get { return _selectedInvolvedObject; }
			set { SetProperty(ref _selectedInvolvedObject, value, () => SelectedInvolvedObject); }
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

		public virtual string ToolTip
		{
			get => "Select Current Work Item";
		}

		protected void GoPreviousItem()
		{
			ViewUtils.Try(() =>
			{
				QueuedTask.Run(() =>
				{
					CurrentWorkList.GoPrevious();
					CurrentWorkItem = new WorkItemVmBase(CurrentWorkList.Current);
				});
			}, _msg);
		}

		private bool? _autoZoomMode = true;

		public bool? AutoZoomMode
		{
			get => _autoZoomMode;
			set
			{
				if (! ShiftPressed())
				{
					return;
				}

				SetProperty(ref _autoZoomMode, value, () => AutoZoomMode);
			}
		}

		protected void GoNearestItem()
		{
			ViewUtils.Try(() =>
			{
				QueuedTask.Run(() =>
				{
					CurrentWorkList.GoNearest(GetReferenceGeometry(MapView.Active.Extent));
					CurrentWorkItem = new WorkItemVmBase(CurrentWorkList.Current);

					if (_autoZoomMode != null && _autoZoomMode.Value)
					{
						ZoomTo();
					}
				});
			}, _msg);
		}

		[NotNull]
		private Geometry GetReferenceGeometry([NotNull] Envelope visibleExtent,
		                                      Geometry candidate = null)
		{
			Assert.ArgumentNotNull(visibleExtent, nameof(visibleExtent));
			Assert.ArgumentCondition(! visibleExtent.IsEmpty, "visible extent is empty");

			Geometry reference;
			if (TryGetReferenceGeometry(visibleExtent, candidate, out reference))
			{
				return Assert.NotNull(reference, "reference is null");
			}

			Assert.NotNull(CurrentWorkList.Current);

			if (TryGetReferenceGeometry(visibleExtent, CurrentWorkList.Current.Extent,
			                            out reference))
			{
				return Assert.NotNull(reference, "reference is null");
			}

			MapPoint centroid = GeometryEngine.Instance.Centroid(visibleExtent);
			return Assert.NotNull(centroid, "centroid is null");
		}

		private static bool TryGetReferenceGeometry(
			[NotNull] Envelope visibleExtent,
			[CanBeNull] Geometry candidateReferenceGeometry,
			[CanBeNull] out Geometry referenceGeometry)
		{
			if (candidateReferenceGeometry == null || candidateReferenceGeometry.IsEmpty)
			{
				referenceGeometry = null;
				return false;
			}

			Map map = MapView.Active.Map;

			Geometry projected =
				GeometryUtils.EnsureSpatialReference(candidateReferenceGeometry,
				                                     map.SpatialReference);

			if (GeometryUtils.Contains(visibleExtent, projected))
			{
				referenceGeometry = projected;
				return true;
			}

			referenceGeometry = null;
			return false;
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
			if (ShiftPressed())
			{
				return;
			}

			IWorkItem item = CurrentWorkList.Current;

			if (item == null)
			{
				return;
			}

			Envelope envelope = await QueuedTask.Run(() => GetEnvelope(item));

			await MapView.Active.ZoomToAsync(envelope, TimeSpan.FromSeconds(_seconds));
		}

		private static bool ShiftPressed()
		{
			return (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
		}

		private async Task PanToAsync()
		{
			IWorkItem item = CurrentWorkList.Current;

			if (item == null)
			{
				return;
			}

			var envelope = await QueuedTask.Run(() => GetEnvelope(item));

			await MapView.Active.PanToAsync(envelope, TimeSpan.FromSeconds(_seconds));
		}

		private async Task ZoomToAllAsync()
		{
			await MapView.Active.ZoomToAsync(CurrentWorkList.Extent);
		}

		protected void GoFirstItem()
		{
			ViewUtils.Try(() =>
			{
				QueuedTask.Run(() =>
				{
					CurrentWorkList.GoFirst();
					CurrentWorkItem = new WorkItemVmBase(CurrentWorkList.Current);
				});
			}, _msg);
		}

		protected void GoNextItem()
		{
			ViewUtils.Try(() =>
			{
				QueuedTask.Run(() =>
				{
					CurrentWorkList.GoNext();
					CurrentWorkItem = new WorkItemVmBase(CurrentWorkList.Current);
				});
			}, _msg);
		}

		private async void PickWorkItem()
		{
			await ViewUtils.TryAsync(() =>
			{
				if (FrameworkApplication.CurrentTool != ConfigIDs.Editing_PickWorkListItemTool)
					_lastActiveTool = FrameworkApplication.CurrentTool;

				WorkListsModule.Current.WorkItemPicked += Current_WorkItemPicked;
				WorkListsModule.Current.ActiveWorkListlayer =
					CurrentWorkList; // only current worklist will select invoked picker
				return FrameworkApplication.SetCurrentToolAsync(
					ConfigIDs.Editing_PickWorkListItemTool);
			}, _msg);
		}

		private void Current_WorkItemPicked(object sender, WorkItemPickArgs e)
		{
			ViewUtils.Try(() =>
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
			}, _msg);
		}

		public virtual void SelectCurrentFeature()
		{
			ViewUtils.Try(() =>
			{
				QueuedTask.Run(() =>
				{
					Dictionary<BasicFeatureLayer, List<long>> featureSet =
						new Dictionary<BasicFeatureLayer, List<long>>();

					var oid = CurrentWorkList.Current.Proxy.ObjectId;
					var layers = GetLayersOfFeatureClass(CurrentWorkList.Current.Proxy.Table.Name);
					SelectionUtils.ClearSelection(MapView.Active.Map);
					foreach (var layer in layers)
					{
						var qf = new QueryFilter() {ObjectIDs = new List<long> {oid}};
						layer.Select(qf, SelectionCombinationMethod.Add);
					}
				});
			}, _msg);
		}

		public virtual void NavigatorUnloaded()
		{
			if (! string.IsNullOrEmpty(_lastActiveTool))
				FrameworkApplication.SetCurrentToolAsync(_lastActiveTool);
			// TODO this for test only
			if (WorkListsModule.Current.ActiveWorkListlayer?.Name == CurrentWorkList.Name)
				WorkListsModule.Current.ActiveWorkListlayer = null;
		}

		private void FlashCurrentFeature()
		{
			ViewUtils.Try(() =>
			{
				QueuedTask.Run(() =>
				{
					var fcName = CurrentWorkList.Current.Proxy.Table.Name;
					var oid = CurrentWorkList.Current.Proxy.ObjectId;

					IEnumerable<BasicFeatureLayer> layers = GetLayersOfFeatureClass(fcName);

					Dictionary<BasicFeatureLayer, List<long>> featureSet =
						new Dictionary<BasicFeatureLayer, List<long>>();

					foreach (var layer in layers)
					{
						if (featureSet.Keys.Contains(layer))
						{
							featureSet[layer].Add(oid);
						}
						else
						{
							featureSet.Add(layer, new List<long> {oid});
						}
					}

					MapView.Active.FlashFeature(featureSet);
				});
			}, _msg);
		}

		protected IEnumerable<FeatureLayer> GetLayersOfFeatureClass(string fcName)
		{
			IEnumerable<FeatureLayer> featureLayers = MapView.Active.Map.Layers
			                                                 .OfType<FeatureLayer>();

			return ! featureLayers.Any()
				       ? featureLayers
				       : featureLayers.Where(layer => layer.GetFeatureClass().GetName() == fcName);
		}

		[CanBeNull]
		protected FeatureLayer GetFeatureLayerByName(string name)
		{
			return MapView.Active.Map.GetLayersAsFlattenedList()
			              .Where(candidate => candidate is BasicFeatureLayer)
			              .FirstOrDefault(layer => layer.Name == name) as FeatureLayer;
		}

		[CanBeNull]
		protected async Task<FeatureLayer> GetFeatureLayerByFeatureClassName(string name)
		{
			return await QueuedTask.Run(() =>
			{
				var layers = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>();
				return layers.FirstOrDefault(layer => layer.GetFeatureClass().GetName() == name);
			});
		}

		[NotNull]
		private static Envelope GetEnvelope([NotNull] IWorkItem item)
		{
			item.QueryPoints(out double xmin, out double ymin,
			                 out double xmax, out double ymax,
			                 out double zmax);

			return EnvelopeBuilder.CreateEnvelope(new Coordinate3D(xmin, ymin, zmax),
			                                      new Coordinate3D(xmax, ymax, zmax),
			                                      item.Extent.SpatialReference)
			                      .Expand(1.1, 1.1, true);
		}
	}
}
