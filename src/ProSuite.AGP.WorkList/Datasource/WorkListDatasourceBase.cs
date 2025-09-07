using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource;

public class WorkListDatasourceBase : PluginDatasourceTemplate
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private IReadOnlyList<string> _tableNames;
	private string _path;

	private XmlWorkListDefinition _xmlWorkListDefinition;

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

		_msg.DebugFormat("Reading work list definition {0}", _path);

		// Read the work list definition only once - this is the expensive part (but still about
		// more one or two orders of magnitude faster than opening a real work list):
		_xmlWorkListDefinition = XmlWorkItemStateRepository.Import(_path);

		string tableName = _xmlWorkListDefinition.Name;

		// the following situation: when work list layer is already in TOC
		// and its data source (work list definition file) is renamed
		// Pro still opens the old data source (old file name) which
		// doesn't exist anymore
		// TODO: Is this still relevant?
		if (string.IsNullOrEmpty(tableName))
		{
			return;
		}

		_msg.DebugFormat("Read work list definition {0} with {1} items.",
		                 tableName, _xmlWorkListDefinition.Items.Count);

		IWorkListRegistry registry = WorkListRegistry.Instance;

		if (! registry.WorklistExists(tableName))
		{
			IWorkEnvironment workEnvironment = null;
			try
			{
				workEnvironment = WorkListEnvironmentFactory.Instance.CreateWorkEnvironment(
					_path, _xmlWorkListDefinition.TypeName);
			}
			catch (Exception e)
			{
				_msg.Debug($"Error creating work environment for {tableName}", e);
			}

			// Mechanism to start creating the work list and loading all items in the background
			// so that it is ready when the user opens the navigator. This is currently opt-in by
			// the <see	cref="IWorkEnvironment.AllowBackgroundLoading"/> flat. However, the items
			// loaded in the background come from the saved workspace state and can be outdated.
			// Only use for read-only work list items.
			bool allowBackgroundLoading = workEnvironment?.AllowBackgroundLoading == true;

			if (allowBackgroundLoading)
			{
				var layerBasedWorkListFactory =
					new LayerBasedWorkListFactory(tableName, _xmlWorkListDefinition.TypeName,
					                              _path);
				registry.TryAdd(layerBasedWorkListFactory);
			}

			// In all other cases, the proper work list is created and registered when the user
			// opens it in the navigator.
		}

		_tableNames =
			new ReadOnlyCollection<string>(new List<string> { tableName });
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
		Assert.NotNullOrEmpty(name, nameof(name));

		WorkItemTable result = null;
		try
		{
			// The given name is one of those returned by GetTableNames()
			_msg.Debug($"Open table '{name}'");

			// The work list could already be registered before the layer is made visible in the TOC:
			CachedWorkItemData cachedWorkItemData =
				WorkListRegistry.Instance.WorklistExists(name)
					? null
					: new CachedWorkItemData(_xmlWorkListDefinition);

			result = new WorkItemTable(name, cachedWorkItemData, Service);
		}
		catch (Exception ex)
		{
			_msg.Debug(
				$"Error opening work item table: {ex.Message}. Definition: {_xmlWorkListDefinition}",
				ex);
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
