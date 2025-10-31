using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

public abstract class GdbItemRepository : IWorkItemRepository
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private int _lastUsedOid;

	// TODO: Create basic record for each source class: Table, DefaultDefinitionQuery, Schema
	//		 re-use DbStatusSourceClassDefinition?
	protected GdbItemRepository(IList<ISourceClass> sourceClasses,
	                            IWorkItemStateRepository workItemStateRepository)
	{
		SourceClasses = sourceClasses;
		WorkItemStateRepository = workItemStateRepository;
	}

	/// <summary>
	/// The current filter definition to be used if one or more
	/// filter expressions (<see cref="WorkListFilterDefinitionExpression"/>) are configured on the
	/// source classes.
	/// </summary>
	public WorkListFilterDefinition CurrentFilterDefinition { get; set; }

	[NotNull]
	public IWorkItemStateRepository WorkItemStateRepository { get; }

	[CanBeNull]
	public SpatialReference SpatialReference
	{
		get
		{
			foreach (ISourceClass sourceClass in SourceClasses)
			{
				var featureClass = OpenTable(sourceClass) as FeatureClass;

				if (featureClass == null)
				{
					continue;
				}

				using (featureClass)
				{
					return featureClass.GetDefinition().GetSpatialReference();
				}
			}

			return null;
		}
	}

	public Geometry AreaOfInterest { get; set; }

	public Envelope Extent { get; set; }

	[NotNull]
	public IList<ISourceClass> SourceClasses { get; }

	public virtual IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
		QueryFilter filter,
		WorkItemStatus? statusFilter)
	{
		// - Consider re-naming to ReadItems (implying DB-Access)
		// - Try get rid of QueryFilter (use filterGeometry, whereClause or more dedicated filter object.)
		return SourceClasses.SelectMany(sourceClass =>
			                                GetItems(sourceClass, filter, statusFilter));
	}

	public virtual IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
		Table table,
		QueryFilter filter,
		WorkItemStatus? statusFilter)
	{
		return SourceClasses.Where(sc => sc.Uses(new GdbTableIdentity(table)))
		                    .SelectMany(sourceClass =>
			                                GetItems(sourceClass, table, filter, statusFilter));
	}

	public abstract void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo);

	public abstract bool CanUseTableSchema(
		[CanBeNull] IWorkListItemDatastore workListItemSchema);

	public long Count()
	{
		return SourceClasses.Sum(sourceClass => Count(sourceClass, new QueryFilter()));
	}

	[CanBeNull]
	public Row GetSourceRow(ISourceClass sourceClass, long oid, bool recycle = true)
	{
		var filter = new QueryFilter { ObjectIDs = new List<long> { oid } };

		return GetRows(sourceClass, filter, recycle).FirstOrDefault();
	}

	public async Task SetStatusAsync(IWorkItem item, WorkItemStatus status)
	{
		await SetStatusCoreAsync(item, status);
	}

	public void Refresh(IWorkItem item)
	{
		WorkItemStateRepository.Refresh(item);
	}

	public void Commit()
	{
		Envelope extent = Extent ?? AreaOfInterest?.Extent;
		WorkItemStateRepository.Commit(SourceClasses, extent);
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
	                                          WorkItemStatus status)
	{
		item.Status = status;

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
		WorkItemStatus? statusFilter = null)
	{
		var count = 0;

		Stopwatch watch = _msg.IsVerboseDebugEnabled ? _msg.DebugStartTiming() : null;

		sourceClass.EnsureValidFilter(ref filter, statusFilter, false);

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
		ISourceClass sourceClass,
		Table table,
		QueryFilter filter,
		WorkItemStatus? statusFilter = null)
	{
		var count = 0;

		Stopwatch watch = _msg.IsVerboseDebugEnabled ? _msg.DebugStartTiming() : null;

		sourceClass.EnsureValidFilter(ref filter, statusFilter, false);

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
	                                 [CanBeNull] QueryFilter filter, bool recycle = true)
	{
		Table table = OpenTable(sourceClass);

		if (table == null)
		{
			_msg.Warn($"No items for {sourceClass.Name} can be loaded.");
			yield break;
		}

		if (CurrentFilterDefinition != null &&
		    sourceClass is DatabaseSourceClass dbSourceClass)
		{
			WorkListFilterDefinitionExpression workListDefinitionExpression =
				dbSourceClass.GetExpression(CurrentFilterDefinition);

			filter = AdaptQueryFilter(filter, workListDefinitionExpression);
		}

		try
		{
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

	[CanBeNull]
	private static QueryFilter AdaptQueryFilter(
		[CanBeNull] QueryFilter filter,
		[CanBeNull] WorkListFilterDefinitionExpression forDefinitionExpression)
	{
		string expression = forDefinitionExpression?.Expression;

		if (string.IsNullOrEmpty(expression))
		{
			return filter;
		}

		filter = GdbQueryUtils.CloneFilter(filter);

		if (string.IsNullOrEmpty(filter.WhereClause))
		{
			filter.WhereClause = expression;
		}
		else
		{
			filter.WhereClause += $" AND ({expression})";
		}

		return filter;
	}

	private long Count([NotNull] ISourceClass sourceClass, [NotNull] QueryFilter filter)
	{
		Stopwatch watch = _msg.IsVerboseDebugEnabled ? _msg.DebugStartTiming() : null;

		sourceClass.EnsureValidFilter(ref filter, null, true);

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
}
