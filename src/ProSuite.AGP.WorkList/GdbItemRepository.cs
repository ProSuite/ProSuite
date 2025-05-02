using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

public abstract class GdbItemRepository : IWorkItemRepository
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private int _lastUsedOid;

	// TODO: Create basic record for each source class: Table, DefinitionQuery, Schema
	//		 re-use DbStatusSourceClassDefinition?
	protected GdbItemRepository(
		IList<ISourceClass> sourceClasses,
		IWorkItemStateRepository workItemStateRepository)
	{
		SourceClasses = sourceClasses;

		WorkItemStateRepository = workItemStateRepository;
	}

	// TODO: (daro) still needed? check usage.
	[CanBeNull]
	public IWorkListItemDatastore TableSchema { get; protected set; }

	[NotNull]
	public IWorkItemStateRepository WorkItemStateRepository { get; }

	[NotNull]
	public IList<ISourceClass> SourceClasses { get; }

	public IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(QueryFilter filter)
	{
		return SourceClasses.SelectMany(sourceClass => GetItems(sourceClass, filter));
	}

	public virtual IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
		QueryFilter filter,
		WorkItemStatus? statusFilter,
		bool excludeGeometry = false)
	{
		return SourceClasses.SelectMany(sourceClass =>
			                                GetItems(sourceClass, filter, statusFilter,
			                                         excludeGeometry));
	}

	public virtual IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(Table table,
		QueryFilter filter,
		WorkItemStatus? statusFilter,
		bool excludeGeometry = false)
	{
		return SourceClasses.Where(sc => sc.Uses(new GdbTableIdentity(table)))
		                    .SelectMany(sourceClass =>
			                                GetItems(sourceClass, table, filter, statusFilter,
			                                         excludeGeometry));
	}

	public abstract void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo);

	public abstract bool CanUseTableSchema(
		[CanBeNull] IWorkListItemDatastore workListItemSchema);

	public long Count()
	{
		return SourceClasses.Sum(sourceClass => Count(sourceClass, new QueryFilter()));
	}

	[CanBeNull]
	public Row GetSourceRow(ISourceClass sourceClass, long oid)
	{
		var filter = new QueryFilter { ObjectIDs = new List<long> { oid } };

		return GetRows(sourceClass, filter, false).FirstOrDefault();
	}

	// TODO: Rename to Update?
	public void SetVisited(IWorkItem item)
	{
		UpdateState(item);
	}

	public async Task SetStatusAsync(IWorkItem item, WorkItemStatus status)
	{
		item.Status = status;

		GdbTableIdentity tableId = item.GdbRowProxy.Table;

		ISourceClass source =
			SourceClasses.FirstOrDefault(s => s.Uses(tableId));
		Assert.NotNull(source);

		// todo: daro read / restore item again from db? restore pattern in case of failure?
		await SetStatusCoreAsync(item, source);
	}

	private void UpdateState(IWorkItem item)
	{
		WorkItemStateRepository.UpdateState(item);
	}

	public void Refresh(IWorkItem item)
	{
		WorkItemStateRepository.Refresh(item);
	}

	public void Commit()
	{
		WorkItemStateRepository.Commit(SourceClasses);
	}

	public void SetCurrentIndex(int currentIndex)
	{
		WorkItemStateRepository.CurrentIndex = currentIndex;
	}

	public int GetCurrentIndex()
	{
		return WorkItemStateRepository.CurrentIndex ?? -1;
	}

	public long GetNextOid()
	{
		return ++_lastUsedOid;
	}

	private IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
		ISourceClass sourceClass,
		QueryFilter filter,
		WorkItemStatus? statusFilter = null,
		bool excludeGeometry = false)
	{
		var count = 0;

		Stopwatch watch = _msg.IsVerboseDebugEnabled ? _msg.DebugStartTiming() : null;

		filter = sourceClass.EnsureValidFilter(filter, excludeGeometry);

		// Source classes can set the respective filters / definition queries
		// TODO: (daro) drop todo below?
		// TODO: Consider getting only the right status, but that means
		// extra round trips:

		// TODO: (daro) should look like:
		// AdaptSourceFilter(filter, sourceClass);
		filter.WhereClause = sourceClass.CreateWhereClause(statusFilter);

		// Selection Item ObjectIDs to filter out, or change of SearchOrder:
		AdaptSourceFilter(filter, sourceClass);

		foreach (Row row in GetRows(sourceClass, filter, recycle: true))
		{
			IWorkItem item = CreateWorkItemCore(row, sourceClass);

			Geometry geometry = row is Feature feature ? feature.GetShape() : null;

			count += 1;
			yield return KeyValuePair.Create(item, geometry);
		}

		_msg.DebugStopTiming(
			watch, $"GetItems() {sourceClass.Name}: {count} items");
	}

	private IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
		ISourceClass sourceClass, Table table,
		QueryFilter filter,
		WorkItemStatus? statusFilter = null,
		bool excludeGeometry = false)
	{
		var count = 0;

		Stopwatch watch = _msg.IsVerboseDebugEnabled ? _msg.DebugStartTiming() : null;

		filter = sourceClass.EnsureValidFilter(filter, excludeGeometry);

		// Source classes can set the respective filters / definition queries
		// TODO: (daro) drop todo below?
		// TODO: Consider getting only the right status, but that means
		// extra round trips:
		filter.WhereClause = sourceClass.CreateWhereClause(statusFilter);

		// Selection Item ObjectIDs to filter out, or change of SearchOrder:
		AdaptSourceFilter(filter, sourceClass);

		foreach (Row row in GetRows(table, filter, recycle: true))
		{
			IWorkItem item = CreateWorkItemCore(row, sourceClass);

			Geometry geometry = row is Feature feature ? feature.GetShape() : null;

			count += 1;
			yield return KeyValuePair.Create(item, geometry);
		}

		_msg.DebugStopTiming(
			watch, $"GetItems() {sourceClass.Name}: {count} items");
	}

	private long Count([NotNull] ISourceClass sourceClass, [NotNull] QueryFilter filter)
	{
		Stopwatch watch = _msg.IsVerboseDebugEnabled ? _msg.DebugStartTiming() : null;

		// include done and todo
		filter.WhereClause = sourceClass.CreateWhereClause(null);

		AdaptSourceFilter(filter, sourceClass);

		// TODO: (daro) pass in name
		using Table table = OpenTable(sourceClass);

		if (table == null)
		{
			_msg.Warn($"No items for {sourceClass.Name} can be loaded.");
			return 0;
		}

		using TableDefinition definition = table.GetDefinition();
		filter.SubFields = definition.GetObjectIDField();

		long count = table.GetCount(filter);

		_msg.DebugStopTiming(
			watch, $"Count() {sourceClass.Name}: {count} items");

		return count;
	}

	// todo: (daro) move to ISourceClass?
	protected abstract void AdaptSourceFilter([NotNull] QueryFilter filter,
	                                          [NotNull] ISourceClass sourceClass);

	protected virtual Task SetStatusCoreAsync([NotNull] IWorkItem item,
	                                          [NotNull] ISourceClass source)
	{
		UpdateState(item);

		return Task.CompletedTask;
	}

	private IEnumerable<Row> GetRows([NotNull] ISourceClass sourceClass,
	                                 [CanBeNull] QueryFilter filter,
	                                 bool recycle)
	{
		// TODO: (daro) pass in name
		Table table = OpenTable(sourceClass);

		if (table == null)
		{
			_msg.Warn($"No items for {sourceClass.Name} can be loaded.");
			yield break;
		}

		try
		{
			// Todo: daro check recycle
			foreach (Row row in GdbQueryUtils.GetRows<Row>(table, filter, recycle))
			{
				yield return row;
			}
		}
		finally
		{
			table.Dispose();
		}
	}

	private IEnumerable<Row> GetRows([NotNull] Table table,
	                                 [CanBeNull] QueryFilter filter,
	                                 bool recycle)
	{
		// Todo: daro check recycle
		foreach (Row row in GdbQueryUtils.GetRows<Row>(table, filter, recycle))
		{
			yield return row;
		}
	}

	// TODO: (daro) still needed?
	[CanBeNull]
	protected virtual IAttributeReader CreateAttributeReaderCore(
		[NotNull] TableDefinition definition,
		[CanBeNull] IWorkListItemDatastore tableSchema)
	{
		// TODO: Make independent of attribute list, use standard AttributeRoles
		var attributes = new[]
		                 {
			                 Attributes.QualityConditionName,
			                 Attributes.IssueCodeDescription,
			                 Attributes.InvolvedObjects,
			                 Attributes.IssueSeverity,
			                 Attributes.IssueCode,
			                 Attributes.IssueDescription
		                 };

		return tableSchema?.CreateAttributeReader(definition, attributes);
	}

	[NotNull]
	protected abstract IWorkItem CreateWorkItemCore([NotNull] Row row,
	                                                [NotNull] ISourceClass sourceClass);

	[CanBeNull]
	protected abstract Table OpenTable([NotNull] ISourceClass sourceClass);
}
