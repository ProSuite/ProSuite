using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Web;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource;

public class SelectionWorkListDatasourceBase : PluginDatasourceTemplate
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private string _path;
	private IWorkList _workList;
	private IReadOnlyList<string> _tableNames;

	public override void Open([NotNull] Uri connectionPath) // "open workspace"
	{
		Try(() =>
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

			_tableNames = new ReadOnlyCollection<string>(
				new List<string>
				{
					FormatTableName(name)
				});
		}, "Error opening work list data source");
	}

	public override void Close()
	{
		_workList = null;
		_msg.VerboseDebug(() => "WorkListDataSource.Close()");
	}

	public override PluginTableTemplate OpenTable([NotNull] string name)
	{
		Assert.ArgumentNotNull(name, nameof(name));

		WorkItemTable result = null;
		try
		{
			// The given name is one of those returned by GetTableNames()
			_msg.Debug($"Open table '{name}'");

			ParseTableName(name, out string listName);

			_workList = WorkListRegistry.Instance.Get(name);
			Assert.NotNull(_workList);

			if (_workList == null &&
			    ! _path.EndsWith("swl") && ! _path.EndsWith("iwl"))
			{
				throw new ArgumentException();

				// Work lists not registered as project items. Auto-register (consider always?):
				var xmlBasedWorkListFactory = new XmlBasedWorkListFactory(_path, name);
				WorkListRegistry.Instance.TryAdd(xmlBasedWorkListFactory);
				_workList = xmlBasedWorkListFactory.Get();
			}

			if (_workList != null)
			{
				if (QueuedTask.OnWorker)
				{
					var service = new WorkListGeometryService();
					service.Start(_workList);
				}

				result = new WorkItemTable(_workList, listName);
			}
			else
			{
				// TODO: Can we just auto-register?
				string fileName = Path.GetFileName(_path);
				var message =
					$"Cannot find data source of work list {fileName}. It is likely not part of the Work List project items.";

				// The exception is not going to crash Pro. Or is it?
				// It might depend on the application state.
				// It results in a broken data source of the work list layer.
				_msg.Warn(message);
				_msg.DebugFormat("File location: {0}. Work list unique name: {1}",
				                 _path, name);
			}
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}

		return result;
	}

	public override IReadOnlyList<string> GetTableNames()
	{
		return _tableNames ?? Array.Empty<string>();
	}

	public override bool IsQueryLanguageSupported()
	{
		// TODO Pro calls this before Open(), i.e., when _workList is still null!
		return _workList?.QueryLanguageSupported ?? false;
	}

	private static string FormatTableName([NotNull] string listName)
	{
		// for now just the list name; later we *may* have separate "layers" for different geometry types
		return Assert.NotNull(listName);
	}

	private static void ParseTableName([NotNull] string tableName, out string listName)
	{
		// for now table name *is* the list name
		listName = tableName;
	}

	private static void Try([NotNull] Action action,
	                        [NotNull] string message,
	                        [CallerMemberName] string caller = null)
	{
		Assert.ArgumentNotNull(action, nameof(action));

		try
		{
			_msg.VerboseDebug(() => $"WorkListDataSource.{caller}");

			action();
		}
		catch (Exception e)
		{
			_msg.Warn(message, e);
		}
	}
}
