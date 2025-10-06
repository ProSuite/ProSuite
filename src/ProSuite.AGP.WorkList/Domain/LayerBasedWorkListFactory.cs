using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Domain;

public class LayerBasedWorkListFactory : WorkListFactoryBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly string _typeName;
	private readonly string _path;

	private TaskCompletionSource<IWorkList> _workListCreationTaskCompletionSource;

	public LayerBasedWorkListFactory(string tableName, string typeName, string path)
	{
		_typeName = typeName;
		_path = path;
		Name = tableName;
	}

	public override string Name { get; }

	public override IWorkList Get()
	{
		try
		{
			if (WorkList != null)
			{
				return WorkList;
			}

			if (MapView.Active == null)
			{
				return null;
			}

			IWorkEnvironment workEnvironment =
				WorkListEnvironmentFactory.Instance.CreateWorkEnvironment(_path, _typeName);

			if (workEnvironment == null)
			{
				return null;
			}

			// TODO: register AOI in WorkListEnvironmentFactory? Like item store?
			// TODO: AreaOfInterest pull to base and as ctor paramter?
			// TODO: (daro) consider storing extent in definition file.
			//workEnvironment.AreaOfInterest

			Assert.NotNull(workEnvironment);

			if (_workListCreationTaskCompletionSource == null)
			{
				StartCreatingWorklist(workEnvironment);
			}
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}

		return WorkList;
	}

	public override async Task<IWorkList> GetAsync()
	{
		try
		{
			if (WorkList != null)
			{
				return WorkList;
			}

			if (MapView.Active == null)
			{
				return null;
			}

			if (_workListCreationTaskCompletionSource != null)
			{
				// The work list is already being created. Wait for it:
				return await _workListCreationTaskCompletionSource.Task;
			}

			IWorkEnvironment workEnvironment =
				await WorkListEnvironmentFactory.Instance.CreateWorkEnvironmentAsync(
					_path, _typeName);

			if (workEnvironment == null)
			{
				return null;
			}

			// TODO: register AOI in WorkListEnvironmentFactory? Like item store?
			// TODO: AreaOfInterest pull to base and as ctor paramter?
			// TODO: (daro) consider storing extent in definition file.
			//workEnvironment.AreaOfInterest

			return await StartCreatingWorklist(workEnvironment).Task;
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}

		return WorkList;
	}

	private TaskCompletionSource<IWorkList> StartCreatingWorklist(IWorkEnvironment workEnvironment)
	{
		_workListCreationTaskCompletionSource = new TaskCompletionSource<IWorkList>();

		Task.Run(async () =>
		{
			try
			{
				WorkList = await workEnvironment.CreateWorkListAsync(Name, _path);
				_workListCreationTaskCompletionSource.SetResult(WorkList);
			}
			catch (Exception e)
			{
				_msg.Error("Error preparing work list.", e);
				_workListCreationTaskCompletionSource.SetException(e);
			}
			finally
			{
				_workListCreationTaskCompletionSource = null;
			}
		});

		return _workListCreationTaskCompletionSource;
	}
}
