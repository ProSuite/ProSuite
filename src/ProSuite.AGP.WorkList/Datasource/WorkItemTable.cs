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
		private readonly WorkItemLayer _itemLayer;
		private readonly string _tableName;
		private readonly IReadOnlyList<PluginField> _fields;

		// todo daro: how many times invoked?
		public WorkItemTable(IWorkList workList, WorkItemLayer itemLayer, string tableName)
		{
			_workList = workList ?? throw new ArgumentNullException(nameof(workList));
			_itemLayer = itemLayer;
			_tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
			_fields = new ReadOnlyCollection<PluginField>(GetSchema());
		}

		public override string GetName()
		{
			return _tableName;
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
					return GeometryType.Polygon; // (we convert Envelope to Polygon)
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
			var list = _workList.GetItems(queryFilter)
			                    .Select(item => GetValues(item, _workList.Current))
			                    .ToList(); // TODO drop ToList, inline
			return new WorkItemCursor(list);
		}

		public override PluginCursorTemplate Search(SpatialQueryFilter spatialQueryFilter)
		{
			return Search((QueryFilter) spatialQueryFilter);
		}

		private object[] GetValues([NotNull] IWorkItem item, IWorkItem current = null)
		{
			var values = new object[6];
			values[0] = item.OID;
			values[1] = item.Description;
			values[2] = item.Status == WorkItemStatus.Done ? 1 : 0;
			values[3] = item.Visited == WorkItemVisited.Visited ? 1 : 0;
			values[4] = item == current ? 1 : 0;
			values[5] = _itemLayer == WorkItemLayer.Extent
				            ? CreatePolygon(item.Extent)
				            : item.Shape;
			return values;
		}

		private static PluginField[] GetSchema()
		{
			var fields = new List<PluginField>(8);
			fields.Add(new PluginField("OBJECTID", "ObjectID", FieldType.OID));
			fields.Add(new PluginField("TEXT", "Text", FieldType.String));
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
