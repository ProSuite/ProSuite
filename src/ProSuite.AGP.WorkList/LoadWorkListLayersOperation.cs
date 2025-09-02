using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
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

	public LoadWorkListLayersOperation([NotNull] IWorkEnvironment workEnvironment,
	                                   [NotNull] string workListName)
	{
		_workEnvironment = workEnvironment;
		_workListName = workListName;
	}

	public override string Name => GetName();
	public override string Category => "Mapping";
	public override bool DirtiesProject => true;

	protected override async Task DoAsync()
	{
		try
		{
			await QueuedTask.Run(() =>
			{
				OperationManager manager = MapView.Active.Map.OperationManager;

				Action loadLayers = () =>
				{
					IWorkList workList = WorkListRegistry.Instance.Get(_workListName);
					Assert.NotNull(workList);

					string workListFile = workList.Repository.WorkItemStateRepository
					                              .WorkListDefinitionFilePath;
					Assert.NotNullOrEmpty(workListFile);

					_workEnvironment.LoadWorkListLayer(workList, workListFile);
					_workEnvironment.LoadAssociatedLayers(workList);
				};

				Gateway.CompositeOperation(
					manager, "Doesn't matter as long as the operation is removed.",
					loadLayers);

				Operation loadWorkListLayer = manager.PeekUndo();
				manager.RemoveUndoOperation(loadWorkListLayer);
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
			await QueuedTask.Run(() =>
			{
				OperationManager manager = MapView.Active.Map.OperationManager;

				// ReSharper disable once AsyncVoidLambda
				Action removeLayers = async () =>
				{
					IWorkList workList = await WorkListRegistry.Instance.GetAsync(_workListName);
					Assert.NotNull(workList);

					WorkListRegistry.Instance.UnWire(workList);

					await WorkListUtils.RemoveWorkListLayersAsync(workList);
				};

				Gateway.CompositeOperation(
					manager, "Doesn't matter as long as the operation is removed.",
					removeLayers);

				Operation operation = manager.PeekUndo();
				manager.RemoveUndoOperation(operation);
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
}
