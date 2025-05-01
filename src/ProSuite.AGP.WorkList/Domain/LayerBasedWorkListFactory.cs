using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain;

public class LayerBasedWorkListFactory : WorkListFactoryBase
{
	public LayerBasedWorkListFactory(string tableName)
	{
		Name = tableName;
	}

	public override string Name { get; }

	public override IWorkList Get()
	{
		return WorkList;
	}

	public override async Task<IWorkList> GetAsync()
	{
		if (WorkList == null)
		{
			Func<WorkEnvironmentBase> factoryMethod =
				WorkListEnvironmentFactory.Instance.CreateModelBasedEnvironment;

			if (factoryMethod != null)
			{
				WorkEnvironmentBase workEnvironment = factoryMethod();

				WorkList = await QueuedTask.Run(() => workEnvironment.CreateWorkListAsync(Name));

				if (WorkList != null)
				{
					WorkList.Name = Name;
				}
			}
		}

		return WorkList;
	}
}
