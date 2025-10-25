using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.ProjectItem;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.AGP.WorkList.Domain;

public abstract class WorkListFactoryBase : IWorkListFactory
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[CanBeNull] private IWorkList _workList;
	private EditEventsRowCacheSynchronizer _synchronizer;

	protected WorkListFactoryBase()
	{
		MapClosedEvent.Subscribe(OnMapClosed);
		ProjectSavingEvent.Subscribe(OnProjectSaving);
		LayersRemovingEvent.Subscribe(OnLayerRemovingAsync);
		ProjectItemRemovingEvent.Subscribe(OnProjectItemRemovingAsync);
	}

	protected WorkListFactoryBase([CanBeNull] IWorkList workList) : this()
	{
		WorkList = workList;
	}

	[CanBeNull]
	protected IWorkList WorkList
	{
		get => _workList;
		set
		{
			_workList = value;

			Assert.NotNull(_workList);

			_synchronizer = new EditEventsRowCacheSynchronizer(_workList);
			_workList.WorkListChanged += WorkList_WorkListChanged;
		}
	}

	public bool IsWorkListCreated => _workList != null;

	public abstract string Name { get; }

	public void UnWire()
	{
		_synchronizer?.Dispose();

		if (_workList != null)
		{
			_workList.WorkListChanged -= WorkList_WorkListChanged;
			// persist work list state
			_workList.Commit();
		}

		MapClosedEvent.Unsubscribe(OnMapClosed);
		ProjectSavingEvent.Unsubscribe(OnProjectSaving);
		LayersRemovingEvent.Unsubscribe(OnLayerRemovingAsync);
		ProjectItemRemovingEvent.Unsubscribe(OnProjectItemRemovingAsync);
	}

	public abstract IWorkList Get();

	public abstract Task<IWorkList> GetAsync();

	private async void WorkList_WorkListChanged(object sender, WorkListChangedEventArgs e)
	{
		_msg.Debug("WorkList_WorkListChanged");

		try
		{
			await QueuedTask.Run(() =>
			{
				var workList = (IWorkList) sender;

				var layers = MapUtils.GetActiveMap().GetLayersAsFlattenedList();

				var worklistLayers = WorkListUtils.GetWorklistLayers(layers, workList).ToList();

				if (worklistLayers.Count == 0)
				{
					// There are no work list layers in the map. Nothing to invalidate.
					return;
				}

				foreach (MapView mapView in FrameworkApplication.Panes.OfType<IMapPane>()
				                                                .Select(mapPane => mapPane.MapView))
				{
					if (mapView == null || ! mapView.IsReady)
					{
						continue;
					}

					foreach (Layer worklistLayer in worklistLayers)
					{
						List<long> oids = e.Items;

						if (oids != null)
						{
							// invalidate with OIDs
							var dictionary = new Dictionary<Layer, List<long>>
							                 { { worklistLayer, oids } };
							mapView.Invalidate(SelectionSet.FromDictionary(dictionary));
							continue;
						}

						Envelope extent = e.Extent ?? mapView.Extent;

						if (extent != null)
						{
							// alternatively invalidate with Envelope
							mapView.Invalidate(worklistLayer, extent);
						}
					}
				}
			});
		}
		catch (Exception exc)
		{
			_msg.Error("Error invalidating work list layer", exc);
		}
	}

	private void OnMapClosed(MapClosedEventArgs e)
	{
		try
		{
			if (_workList != null)
			{
				_msg.Debug($"Close map, commit and remove {_workList}");

				_workList.Commit();

				WorkListRegistry.Instance.Remove(_workList);
			}
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}
	}

	private Task OnProjectSaving(ProjectEventArgs e)
	{
		try
		{
			if (_workList != null)
			{
				_msg.Debug($"Save project, commit {_workList}");

				_workList.Commit();
			}
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}

		return Task.CompletedTask;
	}

	private async Task OnLayerRemovingAsync(LayersRemovingEventArgs e)
	{
		try
		{
			if (_workList == null)
			{
				return;
			}

			IEnumerable<Layer> worklistLayers =
				await QueuedTask.Run(() => WorkListUtils.GetWorklistLayers(e.Layers, _workList)
				                                        .ToList());

			if (worklistLayers.Any())
			{
				string layers = StringUtils.Concatenate(worklistLayers, lyr => lyr.Name, ", ");
				_msg.Debug($"Removing work list layers {layers}");

				WorkListRegistry.Instance.Remove(_workList);
			}
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}
	}

	private async Task OnProjectItemRemovingAsync(ProjectItemRemovingEventArgs e)
	{
		try
		{
			await QueuedTask.Run(() =>
			{
				foreach (WorkListProjectItem item in e.ProjectItems
				                                      .OfType<WorkListProjectItem>()
				                                      .Where(item => File.Exists(item.Path)))
				{
					// TablePaneViewModel implements IMapPane as well! Not only "real" map panes.
					foreach (Map map in FrameworkApplication.Panes
					                                        .OfType<IMapPane>()
					                                        .Where(mapPane =>
							                                        mapPane.MapView != null)
					                                        .Select(mapPane => mapPane.MapView.Map))
					{
						if (map == null)
						{
							continue;
						}

						// Removing a layer that's not part of a layer container throws an exception.
						// Check whether the layers are part of the map.
						foreach (Layer layer in WorkListUtils.GetWorklistLayersByPath(
							         map, item.Path))
						{
							_msg.Debug(
								$"Remove project item {item.Name} and work list layer {layer.Name}");

							// this does NOT call the OnLayerRemovingAsync event handler!!
							// OnLayerRemovingAsync is called when the layer is removes manually
							map.RemoveLayer(layer);

							IWorkList workList =
								WorkListUtils.GetLoadedWorklist(WorkListRegistry.Instance, layer);
							Assert.NotNull(workList);

							// no need to persist work list state, work list gets deleted
							Assert.True(WorkListRegistry.Instance.Remove(workList),
							            $"Cannot remove work list {workList} from registry");
						}
					}
				}
			});
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}
	}
}
