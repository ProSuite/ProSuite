using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource
{
	/// <summary>
	/// Represents a work list as a read-only table which is the source behind the work list layer.
	/// </summary>
	public class WorkItemTable : PluginTableTemplate
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly string _tableName;

		[NotNull] private readonly IReadOnlyList<PluginField> _fields;

		[CanBeNull] private readonly IWorkItemData _workItemData;

		[CanBeNull] private readonly WorkListGeometryService _service;

		public WorkItemTable([NotNull] string tableName,
		                     [CanBeNull] IWorkItemData workItemData,
		                     WorkListGeometryService service)
		{
			Assert.ArgumentNotNullOrEmpty(nameof(tableName));

			_tableName = tableName;
			_workItemData = workItemData;

			_fields = new ReadOnlyCollection<PluginField>(GetSchema());

			_service = service;
		}

		/// <summary>
		/// Provides access to non-null work item data at all times even when the map is being
		/// initialized and no data source or metadata other than the work list definition file
		/// is available yet.
		/// </summary>
		private IWorkItemData WorkItems
		{
			get
			{
				IWorkList workList = WorkListRegistry.Instance.Get(_tableName);

				if (workList != null)
				{
					// If the work list has been registered, use it, otherwise fall back to the
					return workList;
				}

				// cached work item data
				return _workItemData;
			}
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
				// NOTE: Do return not an empty envelope. Pluggable Datasource cannot handle an
				// empty envelope.
				// NOTE: But also, do not return null, as the feature classes spatial reference is 
				// determined by the envelope's spatial reference! Without spatial reference,
				// downstream layer queries will fail (GOTOP-621).
				return WorkItems?.Extent;
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

		// TODO: (daro) use creation date of definition file?
		public override DateTime GetLastModifiedTime()
		{
			return DateTime.Now;
		}

		[NotNull]
		public override PluginCursorTemplate Search(QueryFilter filter)
		{
			// This is called on open table. Check QueryFilter.ObjectIDs.
			try
			{
				// NOTE: If the Extent is null, the spatial reference will be null as well and all
				//       filters with null OutputSpatialReference will fail in MoveNext with a
				//       null-pointer from deep inside the Pro SDK
				_msg.VerboseDebug(() =>
				{
					bool willFail = filter.OutputSpatialReference == null &&
					                WorkItems.Extent == null;

					return "Querying WorkItemTable '" + _tableName +
					       (willFail
						        ? "' with null OutputSpatialReference and null Extent (will likely fail in MoveNext)"
						        : "...");
				});

				IWorkItemData workItems = WorkItems;

				if (workItems == null)
				{
					// Even if unexpectedly no work items are available, return an empty cursor instead of throwing.
					_msg.DebugFormat("No work items available! Returning no item for {0}",
					                 _tableName);
					return new WorkItemCursor(Enumerable.Empty<object[]>());
				}

				// NOTE: The spatial filtering is implemented significantly different. It matters which overload
				//       of Search is called!
				IEnumerable<IWorkItem> resultItems =
					filter is SpatialQueryFilter spatialFilter
						? workItems.Search(spatialFilter)
						: workItems.Search(filter);

				// NOTE: If an exception is thrown from GetValues (i.e. cursor.MoveNext()), the
				//       a notification appears in the Notifications pane:
				//       "<layer name>: Invalid pointer function parameter"
				// -> Deliberately not catch to make this state visible to the user (and explain
				//    why nothing is drawn).
				IEnumerable<object[]> resultRows =
					resultItems.Select(item => GetValues(item, workItems, workItems.CurrentItem));

				return new WorkItemCursor(resultRows);
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
			if (WorkItems is IWorkList workList && workList.CacheBufferedItemGeometries)
			{
				_service?.UpdateItemGeometries(_tableName, filter);
			}

			return Search((QueryFilter) filter);
		}

		[NotNull]
		private static object[] GetValues([NotNull] IWorkItem item,
		                                  IWorkItemData workListItems,
		                                  IWorkItem current = null)
		{
			var values = new object[5];
			try
			{
				values[0] = item.OID;
				values[1] = item.Status == WorkItemStatus.Done ? 1 : 0;
				values[2] = item.Visited ? 1 : 0;
				values[3] = item == current ? 1 : 0;
				values[4] = workListItems.GetItemDisplayGeometry(item);
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
