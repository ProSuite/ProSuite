using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource
{
	/// <summary>
	/// Create a subclass in the Plugin project (with the Config.xml file).
	/// This class is only abstract to force this subclass creation, because
	/// we need a class in the same project as the Config.xml file.
	/// </summary>
	public abstract class WorkListDatasourceBase : PluginDatasourceTemplate
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private IWorkList _workList;
		private IReadOnlyList<string> _tableNames;

		public override void Open([NotNull] Uri connectionPath) // "open workspace"
		{
			Assert.ArgumentNotNull(connectionPath, nameof(connectionPath));

			_msg.Debug($"Try to open {connectionPath}");

			// Empirical: when opening a project (.aprx) with a saved layer
			// using our Plugin Datasource, the connectionPath will be
			// prepended with the project file's directory path and
			// two times URL encoded (e.g., ' ' => %20 => %2520)!

			var path = connectionPath.IsAbsoluteUri
				           ? connectionPath.LocalPath
				           : connectionPath.ToString();

			path = HttpUtility.UrlDecode(path);
			path = HttpUtility.UrlDecode(path);

			string name = WorkListUtils.GetName(path);

			_tableNames = new ReadOnlyCollection<string>(
				new List<string>
				{
					FormatTableName(name)
				});
		}

		public override void Close()
		{
			_workList = null;
		}

		public override PluginTableTemplate OpenTable([NotNull] string name)
		{
			Assert.ArgumentNotNull(name, nameof(name));
			Assert.ArgumentCondition(_tableNames.Contains(name), $"Unknown table name {name}");

			// The given name is one of those returned by GetTableNames()
			_msg.Debug($"Open table '{name}'");

			ParseLayer(name, out string listName);

			_workList = WorkListRegistry.Instance.Get(name);

			if (_workList != null)
			{
				return new WorkItemTable(_workList, listName);
			}

			var message = $"Cannot find data source of work list: {name}";
			_msg.Error(message);

			// The exception is not going to crash Pro. It results in a broken
			// data source of the work list layer.
			throw new ArgumentException(message);
		}

		public override IReadOnlyList<string> GetTableNames()
		{
			return _tableNames ?? new string[0];
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

		private static void ParseLayer([NotNull] string tableName, out string listName)
		{
			// for now table name *is* the list name
			listName = tableName;
		}
	}
}
