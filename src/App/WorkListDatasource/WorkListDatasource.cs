using System;
using System.Collections.Generic;
using System.Reflection;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkListDatasource
{
	public class WorkListDatasource : PluginDatasourceTemplate
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private WorkList.Contracts.WorkList _workList;

		public override void Open(Uri connectionPath) // "open workspace"
		{
			_msg.DebugFormat("{0}: Open {1}", nameof(WorkListDatasource), connectionPath);

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
		}

		public override void Close()
		{
			_workList = null;
		}

		public override PluginTableTemplate OpenTable(string name)
		{
			// The given name is one of those returned by GetTableNames()

			_msg.DebugFormat("{0}: OpenTable '{1}'", nameof(WorkListDatasource), name);

			var layer = GetWorkItemLayer(name);

			if (layer == WorkItemLayer.None)
			{
				throw new ArgumentException($"Datasource has no such table: {name}");
			}

			if (_workList == null)
				throw new InvalidOperationException("Datasource is not open");
			return new WorkItemTable(_workList, layer);
		}

		public override IReadOnlyList<string> GetTableNames()
		{
			return new List<string> {"Extent", "Shape"};
		}

		public override bool IsQueryLanguageSupported()
		{
			return false; // TODO consider supporting it, but not today
		}

		private static WorkItemLayer GetWorkItemLayer(string name)
		{
			switch (name?.ToLowerInvariant())
			{
				case "extent":
				case "envelope":
					return WorkItemLayer.Extent;
				case "shape":
				case "geometry":
					return WorkItemLayer.Shape;
			}

			return WorkItemLayer.None;
		}
	}
}
