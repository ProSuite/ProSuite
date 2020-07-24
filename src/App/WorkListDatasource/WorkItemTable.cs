using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkListDatasource
{
	public class WorkItemTable : PluginTableTemplate
	{
		private readonly WorkList.Contracts.WorkList _workList;
		private readonly WorkItemLayer _itemLayer;
		private readonly IReadOnlyList<PluginField> _fields;

		// todo daro: how many times invoked?
		public WorkItemTable(WorkList.Contracts.WorkList workList, WorkItemLayer itemLayer)
		{
			_workList = workList;
			_itemLayer = itemLayer;

			_fields = new ReadOnlyCollection<PluginField>(GetSchema());

			//_fields = new ReadOnlyCollection<PluginField>(
			//	new[]
			//	{
			//		CreateField("OBJECTID", FieldType.OID),
			//		CreateField("ORIGINAL_OID", FieldType.Integer),
			//		CreateField("CURRENT", FieldType.Integer),
			//		CreateField("STATUS", FieldType.Integer),
			//		CreateField("VISITED", FieldType.Integer),
			//		CreateField("SHAPE", FieldType.Geometry)
			//	});
		}

		public override string GetName()
		{
			return $"{_workList.Name}:${_itemLayer}";
		}

		/// <summary>
		/// Get the collection of fields accessible on the plugin table
		/// </summary>
		/// <remarks>The order of returned columns in any rows must match the
		/// order of the fields specified from GetFields()</remarks>
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
			switch (_itemLayer)
			{
				case WorkItemLayer.Shape:
					return _workList.GeometryType;
				case WorkItemLayer.Extent:
					return GeometryType.Polygon; // TODO or would Envelope be fine with Pro?
				default:
					return GeometryType.Unknown;
			}
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
			// TODO for now we ignore the (spatial) queryFilter

			IEnumerable<WorkItem> query = _workList.Items;

			if (_workList.Visibility == WorkItemVisibility.Todo)
			{
				query = query.Where(item => item.Status == WorkItemStatus.Todo);
			}

			if (_workList.AreaOfInterest != null)
			{
				query = query.Where(item => GeometryEngine.Instance.Intersects(_workList.AreaOfInterest, item.Extent));
			}

			return new WorkItemCursor(query.Select(item => GetValues(item, _workList.Current)));
		}

		public override PluginCursorTemplate Search(SpatialQueryFilter spatialQueryFilter)
		{
			return Search((QueryFilter) spatialQueryFilter);
		}

		private object[] GetValues([NotNull] WorkItem item, WorkItem current = null)
		{
			var values = new object[6];
			values[0] = item.OID;
			values[1] = _itemLayer == WorkItemLayer.Extent ? item.Extent : item.Shape;
			values[2] = item.Status == WorkItemStatus.Done ? 1 : 0;
			values[3] = item.Visited == WorkItemVisited.Visited ? 1 : 0;
			values[4] = item == current ? 1 : 0;
			values[5] = item.Description;
			return values;
		}

		private static PluginField[] GetSchema()
		{
			var fields = new List<PluginField>(8);
			fields.Add(new PluginField("OBJECTID", "ObjectID", FieldType.OID));
			fields.Add(new PluginField("SHAPE", "Shape", FieldType.Geometry));
			fields.Add(new PluginField("STATUS", "Status", FieldType.Integer));
			fields.Add(new PluginField("VISITED", "Visited", FieldType.Integer));
			fields.Add(new PluginField("CURRENT", "Is Current", FieldType.Integer));
			fields.Add(new PluginField("TEXT", "Text", FieldType.String));
			return fields.ToArray();
		}
	}
}
