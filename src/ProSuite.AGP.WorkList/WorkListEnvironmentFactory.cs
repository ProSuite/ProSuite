using System;
using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

public class WorkListEnvironmentFactory : IWorkListEnvironmentFactory
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly IDictionary<Type, Func<string, IWorkEnvironment>>
		_factoryMethodsWithStringParam = new Dictionary<Type, Func<string, IWorkEnvironment>>();

	private readonly IDictionary<Type, Func<IWorkListItemDatastore, IWorkEnvironment>>
		_factoryMethodsWithItemDatastoreParam =
			new Dictionary<Type, Func<IWorkListItemDatastore, IWorkEnvironment>>();

	private readonly IDictionary<Type, IWorkListItemDatastore> _datastoresByWorkListType =
		new Dictionary<Type, IWorkListItemDatastore>();

	private Type _currentWorkListType;

	private WorkListEnvironmentFactory() { }

	public static IWorkListEnvironmentFactory Instance { get; set; } = new WorkListEnvironmentFactory();

	public void WithPath(Func<string, IWorkEnvironment> createEnvironment)
	{
		try
		{
			_factoryMethodsWithStringParam.Add(_currentWorkListType, createEnvironment);
		}
		finally
		{
			_currentWorkListType = null;
		}
	}

	public void WithItemStore(Func<IWorkListItemDatastore, IWorkEnvironment> createEnvironment)
	{
		try
		{
			_factoryMethodsWithItemDatastoreParam.Add(_currentWorkListType, createEnvironment);
		}
		finally
		{
			_currentWorkListType = null;
		}
	}

	public void AddStore<T>(IWorkListItemDatastore store) where T : IWorkList
	{
		Assert.False(_datastoresByWorkListType.ContainsKey(typeof(T)),
		             $"Work list of type {typeof(T).Name} already has a work item datastore registered.");

		_datastoresByWorkListType.Add(typeof(T), store);
	}

	public IWorkListEnvironmentFactory RegisterEnvironment<T>() where T : IWorkList
	{
		if (typeof(SelectionWorkList).IsAssignableFrom(typeof(T)))
		{
			_currentWorkListType = typeof(SelectionWorkList);
			return this;
		}

		if (typeof(ProductionModelIssueWorkList).IsAssignableFrom(typeof(T)))
		{
			_currentWorkListType = typeof(ProductionModelIssueWorkList);
			return this;
		}

		if (typeof(IssueWorkList).IsAssignableFrom(typeof(T)))
		{
			_currentWorkListType = typeof(IssueWorkList);
			return this;
		}

		if (typeof(ConflictWorkList).IsAssignableFrom(typeof(T)))
		{
			_currentWorkListType = typeof(ConflictWorkList);
			return this;
		}

		throw new ArgumentOutOfRangeException();
	}

	[CanBeNull]
	public IWorkEnvironment CreateWorkEnvironment(string path)
	{
		if (path.EndsWith(".iwl", StringComparison.OrdinalIgnoreCase))
		{
			return CreateWorkEnvironment(path, "ProSuite.AGP.WorkList.Domain.IssueWorkList");
		}

		// TODO: (daro) introduce new ending for ProductionModelIssueWorkList
		if (path.EndsWith(".ewl", StringComparison.OrdinalIgnoreCase))
		{
			throw new NotImplementedException(".ewl");
			//return CreateWorkEnvironment(path, "ProSuite.AGP.WorkList.Domain.ErrorWorkList");
		}

		if (path.EndsWith(".swl", StringComparison.OrdinalIgnoreCase))
		{
			return CreateWorkEnvironment(path, "ProSuite.AGP.WorkList.Domain.SelectionWorkList");
		}

		throw new InvalidOperationException(
			$"No work environment for {path} has been registered yet.");
	}

	[CanBeNull]
	public IWorkEnvironment CreateWorkEnvironment(string path, string typeName)
	{
		int index = typeName.LastIndexOf('.');
		Assert.True(index >= 0, $"no valid type name: {typeName}");

		Type type;
		string typeString = typeName.Substring(index + 1);

		if (typeString.Equals("IssueWorkList", StringComparison.OrdinalIgnoreCase))
		{
			type = typeof(IssueWorkList);
		}
		else if (typeString.Equals("SelectionWorkList", StringComparison.OrdinalIgnoreCase))
		{
			type = typeof(SelectionWorkList);
		}
		else if (typeString.Equals("ProductionModelIssueWorkList",
		                           StringComparison.OrdinalIgnoreCase))
		{
			type = typeof(ProductionModelIssueWorkList);
		}
		else if (typeString.Equals("ConflictWorkList",
		                           StringComparison.OrdinalIgnoreCase))
		{
			type = typeof(ConflictWorkList);
		}
		else
		{
			throw new ArgumentOutOfRangeException(
				$"Cannot associate {typeString} with any know work list type");
		}

		if (_factoryMethodsWithStringParam.TryGetValue(
			    type, out Func<string, IWorkEnvironment> createFromPath))
		{
			return createFromPath(path);
		}

		if (_datastoresByWorkListType.TryGetValue(type, out var itemStore))
		{
			if (_factoryMethodsWithItemDatastoreParam.TryGetValue(
				    type, out Func<IWorkListItemDatastore, IWorkEnvironment> createFromItemStore))
			{
				return createFromItemStore(itemStore);
			}
			else
			{
				_msg.Debug($"Work list of type {type.Name} has no factory method registered.");
			}
		}
		else
		{
			_msg.Debug($"Work list of type {type.Name} has no work item datastore registered.");
		}

		throw new InvalidOperationException(
			$"No work environment for {typeName} has been registered yet.");
	}
}
