using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using WorkListRegistry = ProSuite.AGP.WorkList.Domain.WorkListRegistry;

namespace ProSuite.AGP.WorkList.Datasource
{
	/// <summary>
	/// Create a subclass in the Plugin project (with the Config.xml file).
	/// This class is only abstract to force this subclass creation, because
	/// we need a class in the same project as the Config.xml file.
	/// </summary>
	public abstract class WorkListDatasourceBase : PluginDatasourceTemplate
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private IWorkList _workList;
		private IReadOnlyList<string> _tableNames;

		public override void Open(Uri connectionPath) // "open workspace"
		{
			_msg.DebugFormat("{0}: Open {1}", nameof(WorkListDatasourceBase), connectionPath);

			if (connectionPath == null)
				throw new ArgumentNullException(nameof(connectionPath));

			if (!connectionPath.IsAbsoluteUri) // make absolute URI
				connectionPath = new Uri(new Uri("worklist://localhost"), connectionPath);

			// scheme://Host:Port/AbsolutePath?Query#Fragment
			// worklist://localhost/workListName?unused&for#now

			var name = connectionPath.LocalPath;
			if (name.Length > 0 && name[0] == '/')
				name = name.Substring(1);

			_workList = WorkListRegistry.Instance.Get(name);

			if (_workList == null)
				throw new ArgumentException($"No such work list: {connectionPath}");

			_tableNames = new ReadOnlyCollection<string>(
				new List<string>
				{
					FormatTableName(_workList.Name)
				});
		}

		public override void Close()
		{
			_workList = null;
		}

		public override PluginTableTemplate OpenTable(string name)
		{
			// The given name is one of those returned by GetTableNames()

			_msg.WarnFormat("{0}: OpenTable '{1}'", nameof(WorkListDatasourceBase), name);

			ParseLayer(name, out string listName);

			if (_workList == null)
				throw new InvalidOperationException("Datasource is not open");

			return new WorkItemTable(_workList, listName);
		}

		public override IReadOnlyList<string> GetTableNames()
		{
			return _tableNames ?? new string[0];
		}

		public override bool IsQueryLanguageSupported()
		{
			return _workList?.QueryLanguageSupported ?? false;
		}

		private static string FormatTableName(string listName)
		{
			// for now just the list name; later we *may* have separate "layers" for different geometry types
			return Assert.NotNull(listName);
		}

		private static void ParseLayer(string tableName, out string listName)
		{
			// for now table name *is* the list name
			listName = tableName;
		}
	}
}
