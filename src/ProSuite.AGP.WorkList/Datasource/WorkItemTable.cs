using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource
{
	public class WorkItemTable : PluginTableTemplate
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IReadOnlyList<PluginField> _fields;
		private readonly string _tableName;

		private readonly IWorkList _workList;

		public WorkItemTable(IWorkList workList, string tableName)
		{
			_workList = workList ?? throw new ArgumentNullException(nameof(workList));
			_tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
			_fields = new ReadOnlyCollection<PluginField>(GetSchema());

			// Now that the table is likely used by a layer, make sure its work list is initialized
			// and correctly updated when source rows change.
			// TODO: In order to avoid duplicate item caching (once the work list navigator is open and re-creates its work list)
			//       try updating this work list rather than replacing it (just replace its repository?)
			// Also, consider not a-priory caching the work list items but query through the source tables.
			// this would likely also fix changing definition queries or updates that make the item disappear (set allowed).
			_workList.EnsureRowCacheSynchronized();
		}

		public override string GetName()
		{
			return _tableName;
		}

		public override IReadOnlyList<PluginField> GetFields()
		{
			return _fields;
		}

		public override Envelope GetExtent()
		{
			// Do return not an empty envelope.
			// Pluggable Datasource cannot handle an empty envelope.
			return _workList.Extent;
		}

		public override GeometryType GetShapeType()
		{
			return GeometryType.Polygon;
		}

		public override PluginCursorTemplate Search(QueryFilter queryFilter)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			const bool ignoreStatusFilter = false;
			List<object[]> list = _workList.GetItems(queryFilter, ignoreStatusFilter)
			                               .Select(item => GetValues(item, _workList.Current))
			                               .ToList(); // TODO drop ToList, inline

			_msg.DebugStopTiming(
				watch, $"{nameof(WorkItemTable)}.{nameof(Search)}(): {list.Count} items");

			return new WorkItemCursor(list);
		}

		public override PluginCursorTemplate Search(SpatialQueryFilter spatialQueryFilter)
		{
			return Search((QueryFilter) spatialQueryFilter);
		}

		private static object[] GetValues([NotNull] IWorkItem item, IWorkItem current = null)
		{
			var values = new object[5];
			values[0] = item.OID;
			values[1] = item.Status == WorkItemStatus.Done ? 1 : 0;
			values[2] = item.Visited ? 1 : 0;
			values[3] = item == current ? 1 : 0;
			values[4] = CreatePolygon(item);
			return values;
		}

		private static PluginField[] GetSchema()
		{
			var fields = new List<PluginField>(8)
			             {
				             new PluginField("OBJECTID", "ObjectID", FieldType.OID),
				             new PluginField("STATUS", "Status", FieldType.Integer),
				             new PluginField("VISITED", "Visited", FieldType.Integer),
				             new PluginField("CURRENT", "Is Current", FieldType.Integer),
				             new PluginField("SHAPE", "Shape", FieldType.Geometry)
			             };
			return fields.ToArray();
		}

		[CanBeNull]
		private static Polygon CreatePolygon(IWorkItem item)
		{
			if (item?.Extent == null)
			{
				return null;
			}

			Envelope extent = item.Extent;

			if (UseExtent(item))
			{
				if (item.HasFeatureGeometry)
				{
					return (Polygon) item.Geometry;
				}

				return PolygonBuilderEx.CreatePolygon(extent, extent.SpatialReference);
			}

			item.QueryPoints(out double xmin, out double ymin,
			                 out double xmax, out double ymax,
			                 out double zmax);

			return PolygonBuilderEx.CreatePolygon(EnvelopeBuilderEx.CreateEnvelope(
				                                      new Coordinate3D(xmin, ymin, zmax),
				                                      new Coordinate3D(xmax, ymax, zmax),
				                                      extent.SpatialReference));
		}

		private static bool UseExtent([NotNull] IWorkItem item)
		{
			switch (item.GeometryType)
			{
				case GeometryType.Polyline:
				case GeometryType.Polygon:
					return true;

				default:
					return false;
			}
		}

		#region Native RowCount

		// First shot: not supported; but we probably could easily!

		public override bool IsNativeRowCountSupported()
		{
			return false;
		}

		public override long GetNativeRowCount()
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}
