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
		private readonly string _tableName;
		private readonly WorkListGeometryService _service;

		[CanBeNull] private IWorkList _workList;

		public WorkItemTable(string tableName)
		{
			_tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
			_fields = new ReadOnlyCollection<PluginField>(GetSchema());
		}

		public WorkItemTable(string tableName, WorkListGeometryService service) : this(tableName)
		{
			_service = service;
		}

		//public WorkItemTable(IWorkList workList, string tableName)
		//{
		//	_workList = workList ?? throw new ArgumentNullException(nameof(workList));
		//	_tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
		//	_fields = new ReadOnlyCollection<PluginField>(GetSchema());

		//	// Now that the table is likely used by a layer, make sure its work list is initialized
		//	// and correctly updated when source rows change.
		//	// TODO: In order to avoid duplicate item caching (once the work list navigator is open and re-creates its work list)
		//	//       try updating this work list rather than replacing it (just replace its repository?)
		//	// Also, consider not a-priory caching the work list items but query through the source tables.
		//	// this would likely also fix changing definition queries or updates that make the item disappear (set allowed).
		//	_workList.EnsureRowCacheSynchronized();
		//}

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

			if (_workList == null)
			{
				TryCreateWorkListAsync();
			}

			return _workList?.Extent;
		}

		public override GeometryType GetShapeType()
		{
			return GeometryType.Polygon;
		}

		public override PluginCursorTemplate Search(QueryFilter filter)
		{
			// This is called on open table. Check QueryFilter.ObjectIDs.

			if (_workList == null)
			{
				TryCreateWorkListAsync();
			}

			if (_workList == null)
			{
				return new WorkItemCursor([]);
			}

			IEnumerable<object[]> items =
				_workList.Search(filter)
				         .Select(item => GetValues(item, _workList, _workList.Current));

			return new WorkItemCursor(items);
		}

		public override PluginCursorTemplate Search(SpatialQueryFilter filter)
		{
			if (_workList == null)
			{
				TryCreateWorkListAsync();
			}

			if (_workList == null)
			{
				return new WorkItemCursor([]);
			}

			_service.HydrateItemGeometries(_tableName, filter);

			IEnumerable<object[]> items =
				_workList.GetItems(filter, false)
				         .Select(item => GetValues(item, _workList, _workList.Current));

			return new WorkItemCursor(items);
		}

		private async void TryCreateWorkListAsync()
		{
			IWorkList workList = await WorkListRegistry.Instance.GetAsync(_tableName);

			if (workList == null)
			{
				return;
			}

			workList?.EnsureRowCacheSynchronized();
			_workList = workList;
		}

		private static object[] GetValues([NotNull] IWorkItem item, IWorkList workList,
		                                  IWorkItem current = null)
		{
			var values = new object[5];
			values[0] = item.OID;
			values[1] = item.Status == WorkItemStatus.Done ? 1 : 0;
			values[2] = item.Visited ? 1 : 0;
			values[3] = item == current ? 1 : 0;
			values[4] = workList.GetItemGeometry(item);
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
