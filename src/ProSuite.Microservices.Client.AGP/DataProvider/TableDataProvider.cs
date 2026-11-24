using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase.AGP;
using ProSuite.GIS.Geodatabase.API;
using ProSuite.GIS.Geometry.AGP;
using ProSuite.GIS.Geometry.API;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.DataProvider;

/// <summary>
/// Answers the data requests of a microservice for the tables that were added with
/// <see cref="AddTable"/>.
/// </summary>
public class TableDataProvider
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	// The live Table instance registered for each class by the schema step, reused verbatim for
	// every subsequent data request: re-resolving by name would re-open the datastore for each of
	// the many chunk requests of a single route redraw.
	// A Pro Table may only be touched on the thread it was opened on, and the schema step does not
	// always run on the thread that answers data requests (see GetTable), hence the owning
	// thread and the name needed to re-open it elsewhere.
	private readonly IDictionary<ClassDef, (Table Table, int ThreadId, string Name)>
		_tablesByClassDef =
			new ConcurrentDictionary<ClassDef, (Table, int, string)>();

	private readonly IDictionary<long, DatastoreName> _datastoreNamesById =
		new ConcurrentDictionary<long, DatastoreName>();

	public TransferMode DefaultTransferMode { get; set; } = TransferMode.Rows;

	/// <summary>
	/// The maximum estimated serialized size (in bytes) of a single data batch. When a batch
	/// reaches this size, it is emitted with <see cref="GdbData.HasMoreData"/> set and a new
	/// batch is started. A value &lt;= 0 (the default) disables size-capped batching, so all
	/// matching rows are transferred in a single batch (used by the transformer/route path).
	/// </summary>
	public long MaxBatchSizeBytes { get; set; }

	public void AddTable([NotNull] ObjectClassMsg tableMsg, [NotNull] Table table)
	{
		ClassDef classDef = GdbObjectConversionUtils.CreateClassDef(tableMsg);

		_tablesByClassDef.Add(
			classDef, (table, Environment.CurrentManagedThreadId, table.GetName()));

		RememberDatastore(classDef.WorkspaceHandle, table.GetDatastore());
	}

	/// <summary>
	/// The datastore of the tables that were added with the given workspace handle.
	/// </summary>
	public bool TryGetDatastoreName(long workspaceHandle,
	                                out DatastoreName datastoreName)
	{
		return _datastoreNamesById.TryGetValue(workspaceHandle, out datastoreName);
	}

	/// <summary>
	/// Answers a data request for a table that was added with <see cref="AddTable"/>.
	/// </summary>
	public IEnumerable<GdbData> GetData([NotNull] DataRequest dataRequest)
	{
		Stopwatch watch = Stopwatch.StartNew();

		// The table is wrapped with the workspace cache, since plain class names are unique.
		Table table = dataRequest.TableCase switch
		{
			DataRequest.TableOneofCase.None =>
				throw new ArgumentException("No table type defined."),
			DataRequest.TableOneofCase.ClassDef => GetTable(dataRequest.ClassDef),
			_ => throw new ArgumentOutOfRangeException(nameof(dataRequest.TableCase),
			                                           $"Unsupported table type: {dataRequest.TableCase}")
		};

		ITable objectClass = ArcGeodatabaseUtils.ToArcTable(table);

		_msg.DebugStopTiming(watch, "Opened table {0}", table.GetName());

		foreach (GdbData data in GetData(objectClass, dataRequest))
		{
			yield return data;
		}
	}

	public IEnumerable<GdbData> GetData([NotNull] ITable objectClass,
	                                    [NotNull] DataRequest dataRequest)
	{
		// TODO: Cancellation
		var table = (Table) ((IObjectClass) objectClass).NativeImplementation;

		IQueryFilter filter = CreateFilter(objectClass, dataRequest.SubFields,
		                                   dataRequest.WhereClause,
		                                   dataRequest.SearchGeometry);

		if (dataRequest.CountOnly)
		{
			// Count rows according to the filter
			long count = objectClass.RowCount(filter);

			yield return new GdbData
			             {
				             GdbObjectCount = count
			             };
			yield break;
		}

		// Provide data rows
		long classHandle = table.GetID();

		QueryFilter proFilter = ArcGeodatabaseUtils.ToProQueryFilter(filter);

		var encoder = ChooseEncoder(DefaultTransferMode, table, proFilter);
		int maxRowsPerBatch = -1;

		// The where clause is logged at VerboseDebug only: for the M:N bridge table it is a ~2 KB
		// IN list, and this runs once per chunk per redraw.
		_msg.DebugFormat("Providing data from {0} (spatial filter: {1}, encoder: {2})",
		                 table.GetName(), GetSpatialFilterInfo(filter), encoder.GetType().Name);
		_msg.VerboseDebug(() => $"Where clause: '{dataRequest.WhereClause}'");

		Stopwatch watch = Stopwatch.StartNew();

		long totalRows = 0;
		int batchCount = 0;

		foreach (var batch in encoder.Encode(table, proFilter, classHandle, maxRowsPerBatch,
		                                     MaxBatchSizeBytes))
		{
			totalRows += batch.GdbColumnarData?.RowCount ?? batch.GdbObjects.Count;
			batchCount++;

			yield return batch;
		}

		_msg.DebugStopTiming(watch, "Provided {0} row(s) from {1} in {2} batch(es)",
		                     totalRows, table.GetName(), batchCount);
	}

	private static IGdbDataEncoder ChooseEncoder(TransferMode mode,
	                                             Table table,
	                                             QueryFilter filter)
	{
		return mode switch
		{
			TransferMode.Columns => new ColumnarGdbDataEncoder(),

			TransferMode.Rows => new RowWiseGdbDataEncoder(),

			// Estimate the column count from the filter (and ideally, from the table's statistics in the future)
			TransferMode.Auto => ! string.IsNullOrEmpty(filter.WhereClause) &&
			                     filter.WhereClause.Contains("=")
				                     ? new RowWiseGdbDataEncoder()
				                     : new ColumnarGdbDataEncoder(),
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode,
			                                           "Unsupported TransferMode")
		};
	}

	[NotNull]
	private static string GetSpatialFilterInfo([CanBeNull] IQueryFilter filter)
	{
		if (filter is not ISpatialFilter spatialFilter || spatialFilter.Geometry == null)
		{
			return "<none>";
		}

		IEnvelope envelope = spatialFilter.Geometry.Envelope;

		return string.Format("{0} of {1:0.##}, {2:0.##}, {3:0.##}, {4:0.##}",
		                     spatialFilter.SpatialRel,
		                     envelope.XMin, envelope.YMin, envelope.XMax, envelope.YMax);
	}

	private Table GetTable(ClassDef classDef)
	{
		_msg.DebugFormat("Handling data request for class ID {0} in workspace handle {1}",
		                 classDef.ClassHandle, classDef.WorkspaceHandle);

		if (_tablesByClassDef.TryGetValue(classDef, out var registered))
		{
			if (registered.ThreadId == Environment.CurrentManagedThreadId)
			{
				return registered.Table;
			}

			// The route path answers data requests on the thread that enumerates QueryRows, which
			// normally also built the schema; a consumer may still resume the enumeration on another
			// thread. Touching the foreign instance throws CalledOnWrongThreadException, so re-open
			// the table here and keep it for the remaining requests of this call.
			// The re-opened table still reads the edit session's uncommitted state: Pro shares the
			// underlying workspace between connections to the same version. On the COM side, a
			// workspace is a single instance per thread, so this costs only a Geodatabase object,
			// not a new connection.
			var geodatabase = (Geodatabase) _datastoreNamesById[classDef.WorkspaceHandle].Open();

			Table reopened = geodatabase.OpenDataset<Table>(registered.Name);

			_tablesByClassDef[classDef] =
				(reopened, Environment.CurrentManagedThreadId, registered.Name);

			return reopened;
		}

		throw new InvalidOperationException(
			$"Class {classDef.ClassHandle} in model {classDef.WorkspaceHandle} is not part of the known schema.");
	}

	private void RememberDatastore(long workspaceHandle, [NotNull] Datastore datastore)
	{
		if (! _datastoreNamesById.ContainsKey(workspaceHandle))
		{
			_datastoreNamesById.Add(workspaceHandle, new DatastoreName(datastore));
		}
	}

	private static IQueryFilter CreateFilter([NotNull] ITable objectClass,
	                                         [CanBeNull] string subFields,
	                                         [CanBeNull] string whereClause,
	                                         [CanBeNull] ShapeMsg searchGeometryMsg)
	{
		// TODO: Uncomment and send basic fields in schemaMsg back
		//// This is not strictly necessary for typical feature classes any more but probably
		//// still a good idea for exotic datasets (shapefiles, PostGIS, etc.)
		//subFields = EnsureOIDFieldName(subFields, objectClass);

		if (objectClass is not IFeatureClass featureClass)
		{
			return CreateFilter(subFields, whereClause);
		}

		var spatialReference =
			featureClass.SpatialReference.NativeImplementation as SpatialReference;

		IGeometry searchGeometry = ArcGeometry.Create(
			ProtobufConversionUtils.FromShapeMsg(searchGeometryMsg, spatialReference));

		if (searchGeometry == null)
		{
			return CreateFilter(subFields, whereClause);
		}

		ISpatialFilter result =
			new ArcSpatialFilter
			{
				Geometry = searchGeometry,
				SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects
			};

		SetSubfieldsAndWhereClause(result, subFields, whereClause);

		return result;
	}

	private static IQueryFilter CreateFilter([CanBeNull] string subFields,
	                                         [CanBeNull] string whereClause)
	{
		IQueryFilter result = new ArcQueryFilter();

		SetSubfieldsAndWhereClause(result, subFields, whereClause);

		return result;
	}

	private static void SetSubfieldsAndWhereClause([NotNull] ITableFilter result,
	                                               [CanBeNull] string subFields,
	                                               [CanBeNull] string whereClause)
	{
		if (! string.IsNullOrEmpty(subFields))
		{
			result.SubFields = subFields;
		}

		if (! string.IsNullOrEmpty(whereClause))
		{
			result.WhereClause = whereClause;
		}
	}
}
