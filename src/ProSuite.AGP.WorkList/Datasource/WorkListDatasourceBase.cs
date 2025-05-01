using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Web;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource;

public class WorkListDatasourceBase : PluginDatasourceTemplate
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private IReadOnlyList<string> _tableNames;
	private string _path;

	[CanBeNull]
	private static WorkListGeometryService _service;

	[NotNull]
	private static WorkListGeometryService Service
	{
		get
		{
			if (_service == null)
			{
				_service = new WorkListGeometryService();
				_service.Start();
			}

			return _service;
		}
	}

	public override void Open([NotNull] Uri connectionPath) // "open workspace"
	{
		try
		{
			Assert.ArgumentNotNull(connectionPath, nameof(connectionPath));

			_msg.Debug($"Try to open {connectionPath}");

			// Empirical: when opening a project (.aprx) with a saved layer
			// using our Plugin Datasource, the connectionPath will be
			// prepended with the project file's directory path and
			// two times URL encoded (e.g., ' ' => %20 => %2520)!

			_path = connectionPath.IsAbsoluteUri
				        ? connectionPath.LocalPath
				        : connectionPath.ToString();

			_path = HttpUtility.UrlDecode(_path);
			_path = HttpUtility.UrlDecode(_path);

			if (! File.Exists(_path))
			{
				_msg.Debug($"{_path} does not exists");
			}

			string name = WorkListUtils.GetWorklistName(_path);

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
		catch (Exception ex)
		{
			_msg.Debug("Error opening work list data source", ex);
		}
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
		Assert.ArgumentNotNull(name, nameof(name));

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
