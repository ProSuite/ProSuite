using System;
using System.Collections.Generic;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using EsriDE.ProSuite.AGP.WorkLists;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.PluginDatasource;

namespace EsriDE.ModelPluginDatasource
{
	public class WorkItemDatasource : PluginDatasourceTemplate
	{
		private IWorkItemService<IWorkItem> _service;

		public override void Open(Uri connectionPath) // "open workspace"
		{
			if (connectionPath == null)
				throw new ArgumentNullException(nameof(connectionPath));

			// Allg: method://host:port/path?param#part
			// z.B.: worklist://localhost/workListName?unused&for#now

			string workListName = connectionPath.AbsolutePath;

			//_service = WorkItemRegistry.Instance.GetService(workListName); // registry is singleton!
		}

		public override void Close()
		{
		}

		public override PluginTableTemplate OpenTable(string name) // Point, Multipoint, Polyline, Polygon, Envelope
		{
			GeometryType geometryType = GetGeometryType(name);
			return new WorkItemTable(_service, geometryType);
		}

		public override IReadOnlyList<string> GetTableNames()
		{
			return new List<string> {"Point", "Multipoint", "Polyline", "Polygon", "Envelope"}; // multipatch?
		}

		public override bool IsQueryLanguageSupported()
		{
			return _service.IsQueryLanguageSupported;
		}

		private static GeometryType GetGeometryType(string name)
		{
			switch (name?.ToLowerInvariant())
			{
				case "point":
					return GeometryType.Point;
				case "multipoint":
					return GeometryType.Multipoint;
				// TODO
			}

			return GeometryType.Unknown;
		}
	}
}
