using System;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList;

public interface IWorkListEnvironmentFactory
{
	void WithPath(Func<string, IWorkEnvironment> createEnvironment);

	void WithItemStore(Func<IWorkListItemDatastore, IWorkEnvironment> createEnvironment);

	void AddStore(IWorkListItemDatastore store);

	IWorkListEnvironmentFactory RegisterEnvironment<T>() where T : IWorkList;

	IWorkEnvironment CreateWorkEnvironment(string path, string typeName);

	IWorkEnvironment CreateWorkEnvironment(string path);
}
