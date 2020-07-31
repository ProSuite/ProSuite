using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Datasource
{
	public class WorkItemTable : PluginTableTemplate
	{
		private readonly IWorkList _workList;
		private readonly string _tableName;
		private readonly IReadOnlyList<PluginField> _fields;

		public WorkItemTable(IWorkList workList, string tableName)
		{
			_workList = workList ?? throw new ArgumentNullException(nameof(workList));
			_tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
			_fields = new ReadOnlyCollection<PluginField>(GetSchema());
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
			return _workList.Extent;
		}

		public override GeometryType GetShapeType()
		{
			return GeometryType.Polygon;
		}

		#region Native RowCount

		// First shot: not supported; but we probably could easily!

		public override bool IsNativeRowCountSupported()
		{
			return false;
		}

		public override int GetNativeRowCount()
		{
			throw new NotSupportedException();
		}

		#endregion

		public override PluginCursorTemplate Search(QueryFilter queryFilter)
		{
			List<object[]> list = _workList.GetItems(queryFilter)
			                               .Select(item => GetValues(item, _workList.Current))
			                               .ToList(); // TODO drop ToList, inline
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
			values[2] = item.Visited == WorkItemVisited.Visited ? 1 : 0;
			values[3] = item == current ? 1 : 0;
			values[4] = CreatePolygon(item.Extent);
			return values;
		}

		private static PluginField[] GetSchema()
		{
			var fields = new List<PluginField>(8);
			fields.Add(new PluginField("OBJECTID", "ObjectID", FieldType.OID));
			fields.Add(new PluginField("STATUS", "Status", FieldType.Integer));
			fields.Add(new PluginField("VISITED", "Visited", FieldType.Integer));
			fields.Add(new PluginField("CURRENT", "Is Current", FieldType.Integer));
			fields.Add(new PluginField("SHAPE", "Shape", FieldType.Geometry));
			return fields.ToArray();
		}

		private static Polygon CreatePolygon(Envelope envelope)
		{
			return PolygonBuilder.CreatePolygon(envelope, envelope.SpatialReference);
		}
	}
}
