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
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public abstract class WorkListViewModelBase<TWorklist> : PropertyChangedBase
		where TWorklist : class, IWorkList
	{
		protected readonly double Seconds = 0.3;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private bool? _autoZoomMode = true;
		private string _previouslyActiveTool;
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

		private async Task SetCurrentAsync([NotNull] IWorkItem item)
		{
			await ViewUtils.TryAsync(Task.Run(() => SetCurrent(item)), _msg);
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

			await MapView.Active.ZoomToAsync(envelope, TimeSpan.FromSeconds(Seconds));
		}

		private async Task ZoomToAllAsync()
		{
			await MapView.Active.ZoomToAsync(CurrentWorkList.Extent, TimeSpan.FromSeconds(Seconds));
		}

		private async Task PanToAsync()
		{
			IWorkItem item = CurrentWorkList.Current;

			if (item == null)
			{
				return;
			}

			Envelope envelope = await QueuedTask.Run(() => GetEnvelope(item));

			await MapView.Active.PanToAsync(envelope, TimeSpan.FromSeconds(Seconds));
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

					// todo daro can featureClassName be null, e.g. if data source is broken?
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

		private string GetCount()
		{
			int all = 0;
			int toDo = 0;

			foreach (IWorkItem item in CurrentWorkList.GetItems(null, true))
			{
				if (item.Status == WorkItemStatus.Todo)
				{
					toDo += 1;
				}

				all += 1;
			}

			return $"{CurrentWorkList.CurrentIndex + 1} of {all} ({toDo} todo, {all} total)";
		}

		private void GoFirstItem()
		{
			ViewUtils.Try(() =>
			{
				QueuedTask.Run(() =>
				{
					CurrentWorkList.GoFirst();
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
					                      TimeSpan.FromSeconds(Seconds));
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

			WorkListChangedEvent.Subscribe(WorklistChanged, this);

			WorkListsModule.Current.WorkItemPicked += Current_WorkItemPicked;
		}

		private void UnwireEvents()
		{
			ActiveToolChangedEvent.Unsubscribe(OnActiveToolChanged);

			WorkListChangedEvent.Unsubscribe(WorklistChanged);

			WorkListsModule.Current.WorkItemPicked -= Current_WorkItemPicked;
		}

		private async Task WorklistChanged(WorkListChangedEventArgs e)
		{
			if (e.Sender is TWorklist workList)
			{
				if (workList.Current == null)
				{
					return;
				}

				await SetCurrentAsync(workList.Current);
			}
		}

		private async void Current_WorkItemPicked(object sender, WorkItemPickArgs e)
		{
			await ViewUtils.TryAsync(() =>
			{
				return QueuedTask.Run(() =>
				{
					long OID = e.Features.First().GetObjectID();

					QueryFilter filter = GdbQueryUtils.CreateFilter(new[] {OID});
					IWorkItem selectedItem = CurrentWorkList.GetItems(filter).FirstOrDefault();

					if (selectedItem == null)
					{
						return;
					}

					CurrentWorkList.GoTo(selectedItem.OID);

					SetCurrent(CurrentWorkList.Current);
				});
			}, _msg);
		}

		private void OnActiveToolChanged(ToolEventArgs e)
		{
			// do not remember pick work item tool
			if (string.Equals(FrameworkApplication.CurrentTool, ConfigIDs.Editing_PickWorkItemTool) ||
			    string.IsNullOrEmpty(e.CurrentID))
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

		private IPlugInWrapper _previousExtentWrapper;
		private IPlugInWrapper _nextExtentWrapper;
		private IPlugInWrapper _zoomInWrapper;
		private IPlugInWrapper _zoomOutWrapper;
		private IPlugInWrapper _clearSelectionWrapper;

		public ICommand ClearSelectionCommand
		{
			get
			{
				if (_clearSelectionWrapper == null)
				{
					_clearSelectionWrapper =
						FrameworkApplication.GetPlugInWrapper(
							DAML.Button.esri_mapping_clearSelectionButton);
				}

				return (ICommand) _clearSelectionWrapper;
			}
		}

		public ICommand PreviousExtentCommand
		{
			get
			{
				if (_previousExtentWrapper == null)
				{
					_previousExtentWrapper =
						FrameworkApplication.GetPlugInWrapper(
							DAML.Button.esri_mapping_prevExtentButton);
				}

				return (ICommand) _previousExtentWrapper;
			}
		}

		public ICommand NextExtentCommand
		{
			get
			{
				if (_nextExtentWrapper == null)
				{
					_nextExtentWrapper =
						FrameworkApplication.GetPlugInWrapper(
							DAML.Button.esri_mapping_nextExtentButton);
				}

				return (ICommand) _nextExtentWrapper;
			}
		}

		public ICommand ZoomInCommand
		{
			get
			{
				if (_zoomInWrapper == null)
				{
					_zoomInWrapper =
						FrameworkApplication.GetPlugInWrapper(
							DAML.Button.esri_mapping_fixedZoomInButton);
				}

				return (ICommand) _zoomInWrapper;
			}
		}

		public ICommand ZoomOutCommand
		{
			get
			{
				if (_zoomOutWrapper == null)
				{
					_zoomOutWrapper =
						FrameworkApplication.GetPlugInWrapper(
							DAML.Button.esri_mapping_fixedZoomOutButton);
				}

				return (ICommand) _zoomOutWrapper;
			}
		}

		public string ClearSelectionTooltipHeading => _clearSelectionWrapper.TooltipHeading;
		public string ClearSelectionTooltip => _clearSelectionWrapper.Tooltip;

		public string PreviousExtentTooltipHeading => _previousExtentWrapper.TooltipHeading;
		public string PreviousExtentTooltip => _previousExtentWrapper.Tooltip;

		public string NextExtentTooltipHeading => _nextExtentWrapper.TooltipHeading;
		public string NextExtentTooltip => _nextExtentWrapper.Tooltip;

		public string ZoomInTooltipHeading => _zoomInWrapper.TooltipHeading;
		public string ZoomInTooltip => _zoomInWrapper.Tooltip;

		public string ZoomOutTooltipHeding => _zoomOutWrapper.TooltipHeading;
		public string ZoomOutTooltip => _zoomOutWrapper.Tooltip;

		public ICommand UnloadedCommand => new RelayCommand(OnUnloadedAsync);

		public ICommand GoNextCommand =>
			new RelayCommand(GoNextAsync, () => CurrentWorkList.CanGoNext());

		public ICommand GoNearestCommand =>
			new RelayCommand(GoNearestAsync, () => CurrentWorkList.CanGoNearest());

		public ICommand GoFirstCommand =>
			new RelayCommand(GoFirstItem, () => CurrentWorkList.CanGoFirst());

		public ICommand ZoomToAllCommand =>
			new RelayCommand(ZoomToAllAsync, () => CurrentWorkList.Extent != null);

		public ICommand GoPreviousCommand =>
			new RelayCommand(GoPreviousAsync, () => CurrentWorkList.CanGoPrevious());

		public ICommand ZoomToCommand =>
			new RelayCommand(ZoomToAsync, () => CurrentWorkList.Current != null);

		public ICommand PanToCommand =>
			new RelayCommand(PanToAsync, () => CurrentWorkList.Current != null);

		public ICommand PickItemCommand =>
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
		public IWorkList CurrentWorkList { get; }

		public WorkItemViewModelBase CurrentItemViewModel
		{
			get => _currentItemViewModel;
			protected set
			{
				SetProperty(ref _currentItemViewModel, value, () => CurrentItemViewModel);

				NotifyPropertyChanged(nameof(Count));
			}
		}

		public IEnumerable<WorkItemVisibility> VisibilityItemsSource =>
			new[] {WorkItemVisibility.Todo, WorkItemVisibility.All};

		public InvolvedObjectRow SelectedInvolvedObject
		{
			get => _selectedInvolvedObject;
			set => SetProperty(ref _selectedInvolvedObject, value, () => SelectedInvolvedObject);
		}

		public string Count => GetCount();

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
