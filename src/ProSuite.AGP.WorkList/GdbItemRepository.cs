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
	protected GdbItemRepository(IList<ISourceClass> sourceClasses,
	                            IWorkItemStateRepository workItemStateRepository)
	{
		SourceClasses = sourceClasses;
		WorkItemStateRepository = workItemStateRepository;
	}

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

		return GetRows(sourceClass, filter).FirstOrDefault();
	}

	public async Task SetStatusAsync(IWorkItem item, WorkItemStatus status)
	{
		item.Status = status;

		GdbTableIdentity tableId = item.GdbRowProxy.Table;

		ISourceClass source = SourceClasses.FirstOrDefault(s => s.Uses(tableId));
		Assert.NotNull(source);

		await SetStatusCoreAsync(item, source);
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

	public void UpdateState(IWorkItem item)
	{
		WorkItemStateRepository.UpdateState(item);
	}

	protected virtual Task SetStatusCoreAsync([NotNull] IWorkItem item,
	                                          [NotNull] ISourceClass source)
	{
		UpdateState(item);

		return Task.CompletedTask;
	}

	[NotNull]
	protected abstract IWorkItem CreateWorkItemCore([NotNull] Row row,
	                                                [NotNull] ISourceClass sourceClass);

	[CanBeNull]
	protected abstract Table OpenTable([NotNull] ISourceClass sourceClass);

	private IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
		ISourceClass sourceClass,
		QueryFilter filter,
		WorkItemStatus? statusFilter = null,
		bool excludeGeometry = false)
	{
		var count = 0;

		Stopwatch watch = _msg.IsVerboseDebugEnabled ? _msg.DebugStartTiming() : null;

		sourceClass.EnsureValidFilter(filter, statusFilter, excludeGeometry);

		foreach (Row row in GetRows(sourceClass, filter))
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

		sourceClass.EnsureValidFilter(filter, statusFilter, excludeGeometry);

		foreach (Row row in GdbQueryUtils.GetRows<Row>(table, filter))
		{
			IWorkItem item = CreateWorkItemCore(row, sourceClass);

			Geometry geometry = row is Feature feature ? feature.GetShape() : null;

			count += 1;
			yield return KeyValuePair.Create(item, geometry);
		}

		_msg.DebugStopTiming(
			watch, $"GetItems() {sourceClass.Name}: {count} items");
	}

	private IEnumerable<Row> GetRows([NotNull] ISourceClass sourceClass,
	                                 [CanBeNull] QueryFilter filter)
	{
		Table table = OpenTable(sourceClass);

		if (table == null)
		{
			_msg.Warn($"No items for {sourceClass.Name} can be loaded.");
			yield break;
		}

		try
		{
			foreach (Row row in GdbQueryUtils.GetRows<Row>(table, filter))
			{
				yield return row;
			}
		}
		finally
		{
			table.Dispose();
		}
	}

	private long Count([NotNull] ISourceClass sourceClass, [NotNull] QueryFilter filter)
	{
		Stopwatch watch = _msg.IsVerboseDebugEnabled ? _msg.DebugStartTiming() : null;

		sourceClass.EnsureValidFilter(filter, null, true);

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

	[CanBeNull]
	protected static IAttributeReader CreateAttributeReader(
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
}
