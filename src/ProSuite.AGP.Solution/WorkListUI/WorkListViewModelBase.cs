using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Events;
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

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private bool? _autoZoomMode = true;
		private string _count;
		private string _previouslyActiveTool;
		private IWorkList _currentWorkList;
		private WorkItemVisibility _visibility;
		private InvolvedObjectRow _selectedInvolvedObject;
		private WorkItemViewModelBase _currentItemViewModel;

		protected WorkListViewModelBase([NotNull] IWorkList workList)
		{
			Assert.ArgumentNotNull(workList, nameof(workList));

			CurrentWorkList = workList;
			_visibility = workList.Visibility;

			SetCurrent(workList.Current);

			RememberCurrentTool(FrameworkApplication.CurrentTool);

			WireEvents();
		}

		private void SetCurrent([CanBeNull] IWorkItem item)
		{
			if (item == null)
			{
				CurrentItemViewModel = new NoWorkItemViewModel();
			}
			else
			{
				SetCurrentItemCore(item);
			}
		}

		protected abstract void SetCurrentItemCore([NotNull] IWorkItem item);

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

		private async Task ZoomToAllAsync()
		{
			await MapView.Active.ZoomToAsync(CurrentWorkList.Extent, TimeSpan.FromSeconds(_seconds));
		}

		private async Task PanToAsync()
		{
			IWorkItem item = CurrentWorkList.Current;

			if (item == null)
			{
				return;
			}

			Envelope envelope = await QueuedTask.Run(() => GetEnvelope(item));

			await MapView.Active.PanToAsync(envelope, TimeSpan.FromSeconds(_seconds));
		}

		private static bool ShiftPressed()
		{
			return (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
		}

		private async void PickItemAsync()
		{
			await ViewUtils.TryAsync(() =>
			{
				// only current worklist will select invoked picker
				WorkListsModule.Current.ActiveWorkListlayer = CurrentWorkList;

				return FrameworkApplication.SetCurrentToolAsync(ConfigIDs.Editing_PickWorkItemTool);
			}, _msg);
		}

		protected virtual async void SelectCurrentFeatureAsync()
		{
			await ViewUtils.TryAsync(() =>
			{
				return QueuedTask.Run(() =>
				{
					IWorkItem item = Assert.NotNull(CurrentWorkList.Current);

					string tableName = item.Proxy.Table.Name;

					FeatureLayer layer =
						MapUtils.GetLayers<FeatureLayer>(
							lyr => string.Equals(
								lyr.GetFeatureClass().GetName(),
								tableName,
								StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

					if (layer == null)
					{
						return;
					}

					long oid = CurrentWorkList.Current.Proxy.ObjectId;

					SelectionUtils.ClearSelection();
					
					layer.Select(new QueryFilter {ObjectIDs = new List<long> {oid}},
					             SelectionCombinationMethod.Add);
				});
			}, _msg);
		}

		private async Task FlashCurrentFeatureAsync()
		{
			await ViewUtils.TryAsync(() =>
			{
				return QueuedTask.Run(() =>
				{
					IWorkItem item = Assert.NotNull(CurrentWorkList.Current);

					string tableName = item.Proxy.Table.Name;

					FeatureLayer layer =
						MapUtils.GetLayers<FeatureLayer>(
							lyr => string.Equals(
								lyr.GetFeatureClass().GetName(),
								tableName,
								StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

					if (layer == null)
					{
						return;
					}

					long oid = CurrentWorkList.Current.Proxy.ObjectId;

					MapView.Active.FlashFeature(layer, oid);
				});
			}, _msg);
		}

		// todo daro can featureClassName be null?

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

		private async Task SetVisiblityAsync()
		{
			await ViewUtils.TryAsync(
				() =>
				{
					return QueuedTask.Run(() => { CurrentWorkList.Visibility = _visibility; });
				}, _msg);
		}

		#region Navigation

		// todo daro refactor
		private string GetCount()
		{
			int all = CurrentWorkList.Count(null, true);
			int toDo = CurrentWorkList
			           .GetItems(null, true).Count(item => item.Status == WorkItemStatus.Todo);
			return $"{CurrentIndex + 1} of {all} ({toDo} todo, {all} total)";
		}

		private void GoFirstItem()
		{
			ViewUtils.Try(() =>
			{
				QueuedTask.Run(() =>
				{
					CurrentWorkList.GoFirst();

					SetCurrent(CurrentWorkList.Current);
				});
			}, _msg);
		}

		private async Task GoPreviousAsync()
		{
			await ViewUtils.TryAsync(() =>
			{
				return QueuedTask.Run(() =>
				{
					CurrentWorkList.GoPrevious();

					SetCurrent(CurrentWorkList.Current);
				});
			}, _msg);
		}

		private async Task GoNextAsync()
		{
			await ViewUtils.TryAsync(() =>
			{
				return QueuedTask.Run(() =>
				{
					CurrentWorkList.GoNext();

					SetCurrent(CurrentWorkList.Current);
				});
			}, _msg);
		}

		private async Task GoNearestAsync()
		{
			await ViewUtils.TryAsync(() =>
			{
				return QueuedTask.Run(() =>
				{
					CurrentWorkList.GoNearest(GetReferenceGeometry(MapView.Active.Extent));

					SetCurrent(CurrentWorkList.Current);

					if (_autoZoomMode == null || ! _autoZoomMode.Value)
					{
						return;
					}

					IWorkItem item = CurrentWorkList.Current;

					if (item == null)
					{
						return;
					}

					MapView.Active.ZoomTo(GetEnvelope(item),
					                      TimeSpan.FromSeconds(_seconds));
				});
			}, _msg);
		}

		[NotNull]
		private Geometry GetReferenceGeometry([NotNull] Envelope visibleExtent,
		                                      Geometry candidate = null)
		{
			Assert.ArgumentNotNull(visibleExtent, nameof(visibleExtent));
			Assert.ArgumentCondition(! visibleExtent.IsEmpty, "visible extent is empty");

			if (TryGetReferenceGeometry(visibleExtent, candidate, out Geometry reference))
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
			[NotNull] Geometry visibleExtent,
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

		#endregion

		#region Loaded / unloaded UserControl

		private async Task OnUnloadedAsync()
		{
			UnwireEvents();

			if (string.Equals(FrameworkApplication.CurrentTool,
			                  ConfigIDs.Editing_PickWorkItemTool) &&
			    ! string.IsNullOrEmpty(_previouslyActiveTool))
			{
				await FrameworkApplication.SetCurrentToolAsync(_previouslyActiveTool);
			}

			// TODO this for test only
			if (WorkListsModule.Current.ActiveWorkListlayer?.Name == CurrentWorkList.Name)
			{
				WorkListsModule.Current.ActiveWorkListlayer = null;
			}
		}

		#endregion

		#region Events

		private void WireEvents()
		{
			ActiveToolChangedEvent.Subscribe(OnActiveToolChanged);
			WorkListsModule.Current.WorkItemPicked += Current_WorkItemPicked;
		}

		private void UnwireEvents()
		{
			ActiveToolChangedEvent.Unsubscribe(OnActiveToolChanged);
			WorkListsModule.Current.WorkItemPicked -= Current_WorkItemPicked;
		}

		private async void Current_WorkItemPicked(object sender, WorkItemPickArgs e)
		{
			await ViewUtils.TryAsync(() =>
			{
				return QueuedTask.Run(() =>
				{
					long OID = e.features.First().GetObjectID();

					QueryFilter filter = GdbQueryUtils.CreateFilter(new[] {OID});
					IWorkItem selectedItem = CurrentWorkList.GetItems(filter).FirstOrDefault();
					foreach (IWorkItem item in CurrentWorkList.GetItems())
					{
						Console.WriteLine(item.OID);
						Console.WriteLine(item.Extent.ToJson());
					}

					if (selectedItem == null)
					{
						return;
					}

					CurrentWorkList.GoToOid(selectedItem.OID);

					SetCurrent(CurrentWorkList.Current);
				});
			}, _msg);
		}

		private void OnActiveToolChanged(ToolEventArgs e)
		{
			// do not remember pick work item tool
			if (string.Equals(FrameworkApplication.CurrentTool, ConfigIDs.Editing_PickWorkItemTool))
			{
				return;
			}

			RememberCurrentTool(e.CurrentID);
		}

		private void RememberCurrentTool([NotNull] string currentTool)
		{
			Assert.ArgumentNotNullOrEmpty(currentTool, nameof(currentTool));

			Assert.False(
				string.Equals(FrameworkApplication.CurrentTool, ConfigIDs.Editing_PickWorkItemTool),
				"don't remember {0}", ConfigIDs.Editing_PickWorkItemTool);

			_previouslyActiveTool = currentTool;
		}

		#endregion

		#region Commands

		public ICommand ClearSelectionCommand =>
			FrameworkApplication.GetPlugInWrapper(
				DAML.Button.esri_mapping_clearSelectionButton) as ICommand;

		public ICommand PreviousExtentCommand =>
			FrameworkApplication.GetPlugInWrapper(
				DAML.Button.esri_mapping_prevExtentButton) as ICommand;

		public ICommand NextExtentCommand =>
			FrameworkApplication.GetPlugInWrapper(
				DAML.Button.esri_mapping_nextExtentButton) as ICommand;

		public ICommand ZoomInCommand =>
			FrameworkApplication.GetPlugInWrapper(
				DAML.Button.esri_mapping_fixedZoomInButton) as ICommand;

		public ICommand ZoomOutCommand =>
			FrameworkApplication.GetPlugInWrapper(
				DAML.Button.esri_mapping_fixedZoomOutButton) as ICommand;

		public ICommand UnloadedCommand =>
			new RelayCommand(OnUnloadedAsync);

		public ICommand GoNextItemCommand =>
			new RelayCommand(GoNextAsync, () => CurrentWorkList.CanGoNext());

		public ICommand GoNearestItemCommand =>
			new RelayCommand(GoNearestAsync, () => CurrentWorkList.CanGoNearest());

		public ICommand GoFirstItemCommand =>
			new RelayCommand(GoFirstItem, () => CurrentWorkList.CanGoFirst());

		public ICommand ZoomToAllCommand =>
			new RelayCommand(ZoomToAllAsync, () => CurrentWorkList.Extent != null);

		public ICommand GoPreviousItemCommand =>
			new RelayCommand(GoPreviousAsync, () => CurrentWorkList.CanGoPrevious());

		public ICommand ZoomToCommand =>
			new RelayCommand(ZoomToAsync, () => CurrentWorkList.Current != null);

		public ICommand PanToCommand =>
			new RelayCommand(PanToAsync, () => CurrentWorkList.Current != null);

		public ICommand PickWorkItemCommand =>
			new RelayCommand(PickItemAsync, () => true);

		public ICommand SelectCurrentFeatureCommand =>
			new RelayCommand(SelectCurrentFeatureAsync,
			                 () => CurrentWorkList.Current != null);

		public ICommand FlashCurrentFeatureCmd =>
			new RelayCommand(FlashCurrentFeatureAsync, () => CurrentWorkList.Current != null);

		public ICommand VisibilityChangedCommand => new RelayCommand(SetVisiblityAsync, () => true);

		#endregion

		#region Properties

		[NotNull]
		public IWorkList CurrentWorkList
		{
			get => _currentWorkList;

			private set => SetProperty(ref _currentWorkList, value, () => CurrentWorkList);
		}

		public WorkItemViewModelBase CurrentItemViewModel
		{
			get => _currentItemViewModel;
			protected set
			{
				SetProperty(ref _currentItemViewModel, value, () => CurrentItemViewModel);

				Count = GetCount();
			}
		}

		public IEnumerable<WorkItemVisibility> VisibilityItemsSource =>
			new[] {WorkItemVisibility.Todo, WorkItemVisibility.All};

		public InvolvedObjectRow SelectedInvolvedObject
		{
			get => _selectedInvolvedObject;
			set => SetProperty(ref _selectedInvolvedObject, value, () => SelectedInvolvedObject);
		}

		public string Count
		{
			get => _count;
			set => SetProperty(ref _count, value, () => Count);
		}

		public int CurrentIndex
		{
			get => CurrentWorkList.CurrentIndex;
		}

		public virtual string ToolTip => "Select Current Work Item";

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

		public WorkItemVisibility Visibility
		{
			get => _visibility;
			set => SetProperty(ref _visibility, value, () => Visibility);
		}

		#endregion
	}
}
