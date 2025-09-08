using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource;

public class WorkListDatasourceBase : PluginDatasourceTemplate
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private IReadOnlyList<string> _tableNames;
	private string _path;

	[CanBeNull] private static WorkListGeometryService _service;

	/// <summary>
	/// Subclasses can globally enable/disable the background service.
	/// </summary>
	[CanBeNull]
	protected virtual WorkListGeometryService Service
	{
		get
		{
			if (_service == null)
			{
				_service = new WorkListGeometryService();
			}

			return _service;
		}
	}

	public override void Open([NotNull] Uri connectionPath) // "open workspace"
	{
		if (connectionPath is null)
			throw new ArgumentNullException(nameof(connectionPath));

		_msg.Debug($"Try to open {connectionPath}");

		_path = connectionPath.IsAbsoluteUri
			        ? connectionPath.LocalPath
			        : connectionPath.ToString();

		if (! File.Exists(_path))
		{
			throw new FileNotFoundException(
				$"Work list definition file not found: {_path}");
		}

		string name = WorkListUtils.GetWorklistName(_path, out string typeName);

		IWorkListRegistry registry = WorkListRegistry.Instance;

		if (! registry.WorklistExists(name))
		{
			registry.TryAdd(new LayerBasedWorkListFactory(name, typeName, _path));
		}

		// the following situation: when work list layer is already in TOC
		// and its data source (work list definition file) is renamed
		// Pro still opens the old data source (old file name) which
		// doesn't exist anymore
		if (string.IsNullOrEmpty(name))
		{
			return;
		}

		_tableNames = new ReadOnlyCollection<string>(new List<string> { name });
	}

	/// <summary>
	/// Is called on removing work list layer
	/// </summary>
	public override void Close()
	{
		_service?.Stop();
		_service = null;

		_msg.Debug("WorkListDataSource.Close()");
	}

	public override PluginTableTemplate OpenTable([NotNull] string name)
	{
		if (name is null)
			throw new ArgumentNullException(nameof(name));

		WorkItemTable result = null;
		try
		{
			// The given name is one of those returned by GetTableNames()
			_msg.Debug($"Open table '{name}'");

			result = new WorkItemTable(name, Service);
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}

		return result;
	}

	public override IReadOnlyList<string> GetTableNames()
	{
		return _tableNames ?? Array.Empty<string>();
	}

	public override bool IsQueryLanguageSupported()
	{
		// TODO: Pro calls this before Open(), i.e., when _workList is still null!
		return false;
		//return _workList?.QueryLanguageSupported ?? false;
	}
}
