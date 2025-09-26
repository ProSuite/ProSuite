using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

public class LoadWorkListLayersOperation : Operation
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly IWorkEnvironment _workEnvironment;
	private readonly string _workListName;
	private readonly bool _loadInAllMaps;

	public LoadWorkListLayersOperation([NotNull] IWorkEnvironment workEnvironment,
									   [NotNull] string workListName,
									   bool loadInAllMaps)
	{
		_workEnvironment = workEnvironment;
		_workListName = workListName;
		_loadInAllMaps = loadInAllMaps;
	}

	public override string Name => GetName();
	public override string Category => "Mapping";
	public override bool DirtiesProject => true;

	public event EventHandler<WorkListLayersArgs> WorkListLayersAdded;
	public event EventHandler<WorkListLayersArgs> WorkListLayersRemoved;

	protected override async Task DoAsync()
	{
		try
		{
			await QueuedTask.Run(() =>
			{
				List<MapView> mapViews = _loadInAllMaps
											 ? MapViewUtils.GetAllMapViews().ToList()
											 : new List<MapView>() { MapView.Active };

				foreach (MapView mapView in mapViews)
				{
					OperationManager manager = mapView.Map.OperationManager;

					Gateway.CompositeOperation(
						manager, "Load Work List layers", () => LoadLayers(mapView));

					Operation loadWorkListLayer = manager.PeekUndo();
					manager.RemoveUndoOperation(loadWorkListLayer);

					OnWorkListLayersAdded(mapView, _workListName);
				}
			});
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}
	}

	protected override async Task UndoAsync()
	{
		try
		{
			IEnumerable<MapView> mapViews = _loadInAllMaps
												? MapViewUtils.GetAllMapViews()
												: new[] { MapView.Active };

			await QueuedTask.Run(() =>
			{
				foreach (MapView mapView in mapViews)
				{
					OperationManager manager = mapView.Map.OperationManager;

					// ReSharper disable once AsyncVoidLambda
					Action removeLayers = async () =>
					{
						IWorkList workList =
							await WorkListRegistry.Instance.GetAsync(_workListName);
						Assert.NotNull(workList);

						await WorkListUtils.RemoveWorkListLayersAsync(mapView, workList);
					};

					Gateway.CompositeOperation(
						manager, "Doesn't matter as long as the operation is removed.",
						removeLayers);

					Operation operation = manager.PeekUndo();
					manager.RemoveUndoOperation(operation);

					OnWorkListLayersRemoved(mapView, _workListName);
				}
			});
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}
	}

	protected override async Task RedoAsync()
	{
		await DoAsync();
	}

	private string GetName()
	{
		try
		{
			IWorkList workList = WorkListRegistry.Instance.Get(_workListName);
			Assert.NotNull(workList);
			return $"Add Work List: {workList.DisplayName}";
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}

		return _workListName;
	}

	private void LoadLayers(MapView mapView)
	{
		IWorkList workList = WorkListRegistry.Instance.Get(_workListName);
		Assert.NotNull(workList);

		string workListFile = workList.Repository.WorkItemStateRepository
									  .WorkListDefinitionFilePath;
		Assert.NotNullOrEmpty(workListFile);

		_workEnvironment.LoadWorkListLayer(mapView, workList, workListFile);
		_workEnvironment.LoadAssociatedLayers(mapView, workList);
	}

	private void OnWorkListLayersAdded(MapView mapView, string workListName)
	{
		WorkListLayersAdded?.Invoke(this, new WorkListLayersArgs(workListName, mapView));
	}

	private void OnWorkListLayersRemoved(MapView mapView, string workListName)
	{
		WorkListLayersRemoved?.Invoke(this, new WorkListLayersArgs(workListName, mapView));
	}
}

public class WorkListLayersArgs : EventArgs
{
	public string WorkListName { get; }
	public MapView MapView { get; }

	public WorkListLayersArgs(string workListName, MapView mapView)
	{
		WorkListName = workListName;
		MapView = mapView;
	}
}
