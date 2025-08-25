using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
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

			QueuedTask.Run(async () =>
			{
				WorkList = await workEnvironment.CreateWorkListAsync(Name, _path);
			});
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

			WorkList = await QueuedTask.Run(() => workEnvironment.CreateWorkListAsync(Name, _path));
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}

		return WorkList;
	}
}
