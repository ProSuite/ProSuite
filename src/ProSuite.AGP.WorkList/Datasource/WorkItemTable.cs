using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource
{
	public class WorkItemTable : PluginTableTemplate
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IReadOnlyList<PluginField> _fields;
		private readonly WorkListGeometryService _service;
		private readonly string _tableName;

		[CanBeNull] private IWorkList _workList;

		public WorkItemTable(string tableName, WorkListGeometryService service)
		{
			_tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
			_service = service ?? throw new ArgumentNullException(nameof(service));
			_fields = new ReadOnlyCollection<PluginField>(GetSchema());
		}

		public override string GetName()
		{
			return _tableName;
		}

		[NotNull]
		public override IReadOnlyList<PluginField> GetFields()
		{
			return _fields;
		}

		[CanBeNull]
		public override Envelope GetExtent()
		{
			try
			{
				// Do return not an empty envelope.
				// Pluggable Datasource cannot handle an empty envelope.
				_workList ??= WorkListRegistry.Instance.GetAsync(_tableName).Result;

				return _workList?.GetExtent();
			}
			catch (Exception ex)
			{
				_msg.Warn(ex.Message, ex);
				return null;
			}
		}

		public override GeometryType GetShapeType()
		{
			return GeometryType.Polygon;
		}

		[NotNull]
		public override PluginCursorTemplate Search(QueryFilter filter)
		{
			// This is called on open table. Check QueryFilter.ObjectIDs.
			try
			{
				_workList ??= WorkListRegistry.Instance.GetAsync(_tableName).Result;

				if (_workList == null)
				{
					return new WorkItemCursor(Enumerable.Empty<object[]>());
				}

				IEnumerable<object[]> items =
					_workList.Search(filter)
					         .Select(item => GetValues(item, _workList, _workList.Current));

				return new WorkItemCursor(items);
			}
			catch (Exception ex)
			{
				_msg.Warn(ex.Message, ex);
				return new WorkItemCursor(Enumerable.Empty<object[]>());
			}
		}

		[NotNull]
		public override PluginCursorTemplate Search(SpatialQueryFilter filter)
		{
			try
			{
				_workList ??= WorkListRegistry.Instance.GetAsync(_tableName).Result;

				if (_workList == null)
				{
					return new WorkItemCursor(Enumerable.Empty<object[]>());
				}

				_service.UpdateItemGeometries(_tableName, filter);

				IEnumerable<object[]> items =
					_workList.GetItems(filter)
					         .Select(item => GetValues(item, _workList, _workList.Current));

				return new WorkItemCursor(items);
			}
			catch (Exception ex)
			{
				_msg.Warn(ex.Message, ex);
				return new WorkItemCursor(Enumerable.Empty<object[]>());
			}
		}

		[NotNull]
		private static object[] GetValues([NotNull] IWorkItem item,
		                                  IWorkList workList,
		                                  IWorkItem current = null)
		{
			var values = new object[5];
			try
			{
				values[0] = item.OID;
				values[1] = item.Status == WorkItemStatus.Done ? 1 : 0;
				values[2] = item.Visited ? 1 : 0;
				values[3] = item == current ? 1 : 0;
				values[4] = workList.GetItemGeometry(item);
			}
			catch (Exception ex)
			{
				_msg.Warn(ex.Message, ex);
			}

			return values;
		}

		private static PluginField[] GetSchema()
		{
			var fields = new List<PluginField>(8)
			             {
				             new("OBJECTID", "ObjectID", FieldType.OID),
				             new("STATUS", "Status", FieldType.Integer),
				             new("VISITED", "Visited", FieldType.Integer),
				             new("CURRENT", "Is Current", FieldType.Integer),
				             new("SHAPE", "Shape", FieldType.Geometry)
			             };
			return fields.ToArray();
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
