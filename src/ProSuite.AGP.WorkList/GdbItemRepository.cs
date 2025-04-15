using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using Geometry = ArcGIS.Core.Geometry.Geometry;
using QueryFilter = ArcGIS.Core.Data.QueryFilter;

namespace ProSuite.AGP.WorkList
{
	// Note maybe all SDK code, like open workspace, etc. should be in here. Not in DatabaseSourceClass for instance.
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

		/// <summary>
		/// The single, current workspace in which all source tables reside. Not null for DbStatus
		/// work lists.
		/// </summary>
		[CanBeNull]
		public Geodatabase CurrentWorkspace { get; set; }

		public IWorkItemStateRepository WorkItemStateRepository { get; }

		[CanBeNull]
		public IWorkListItemDatastore TableSchema { get; protected set; }

		public IList<ISourceClass> SourceClasses { get; set; }

		public abstract void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo);

		public abstract bool CanUseTableSchema(
			[CanBeNull] IWorkListItemDatastore workListItemSchema);

		public IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
			QueryFilter filter = null,
			WorkItemStatus? statusFilter = null,
			bool recycle = true,
			bool excludeGeometry = false)
		{
			return SourceClasses.SelectMany(sourceClass =>
				                                GetItems(sourceClass, filter,
				                                         statusFilter, recycle,
				                                         excludeGeometry));
		}

		private IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(ISourceClass sourceClass,
			QueryFilter filter,
			WorkItemStatus? statusFilter = null,
			bool recycle = true, bool excludeGeometry = false)
		{
			int count = 0;

			Stopwatch watch = _msg.DebugStartTiming();

			filter ??= new QueryFilter();

			if (string.IsNullOrEmpty(filter.SubFields) || string.Equals(filter.SubFields, "*"))
			{
				filter.SubFields = sourceClass.GetRelevantSubFields(excludeGeometry);
			}

			// Source classes can set the respective filters / definition queries

			// TODO: (daro) drop todo below?
			// TODO: Consider getting only the right status, but that means
			// extra round trips:
			filter.WhereClause = sourceClass.CreateWhereClause(statusFilter);

			// Selection Item ObjectIDs to filter out, or change of SearchOrder:
			AdaptSourceFilter(filter, sourceClass);

			foreach (Row row in GetRows(sourceClass, filter, recycle))
			{
				IWorkItem item = CreateWorkItemCore(row, sourceClass);
				WorkItemStateRepository.Refresh(item);

				Geometry geometry = row is Feature feature ? feature.GetShape() : null;

				count += 1;
				yield return KeyValuePair.Create(item, geometry);
			}

			_msg.DebugStopTiming(
				watch, $"GetItems() {sourceClass.Name}: {count} items");
		}

		[CanBeNull]
		public Row GetSourceRow(ISourceClass sourceClass, long oid)
		{
			var filter = new QueryFilter { ObjectIDs = new List<long> { oid } };

			return GetRows(sourceClass, filter, recycle: false).FirstOrDefault();
		}

		// TODO: Rename to Update?
		public void SetVisited(IWorkItem item)
		{
			WorkItemStateRepository.UpdateState(item);
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
		
		public void UpdateState(IWorkItem item)
		{
			WorkItemStateRepository.UpdateState(item);
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
		
		// todo: (daro) move to ISourceClass?
		protected abstract void AdaptSourceFilter([NotNull] QueryFilter filter,
		                                          [NotNull] ISourceClass sourceClass);

		protected virtual Task SetStatusCoreAsync([NotNull] IWorkItem item,
		                                          [NotNull] ISourceClass source)
		{
			return Task.FromResult(0);
		}

		private static IEnumerable<Row> GetRows([NotNull] ISourceClass sourceClass,
		                                        [CanBeNull] QueryFilter filter,
		                                        bool recycle)
		{
			Table table = OpenReadOnlyTable(sourceClass);

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

		// TODO: (daro) still needed?
		[CanBeNull]
		protected virtual IAttributeReader CreateAttributeReaderCore(
			[NotNull] TableDefinition definition,
			[CanBeNull] IWorkListItemDatastore tableSchema)
		{
			// TODO: Make independent of attribute list, use standard AttributeRoles
			Attributes[] attributes = new[]
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
		private static Table OpenReadOnlyTable([NotNull] ISourceClass sourceClass)
		{
			Table table = null;
			try
			{
				// NOTE: This can lead to using a different instance of the same workspace
				// because opening a new Geodatabase with the Connector of an existing
				// Geodatabase can in some cases result in a different instance!
				table = sourceClass.OpenDataset<Table>();
			}
			catch (Exception e)
			{
				_msg.Warn($"Error opening source table {sourceClass.Name}: {e.Message}.", e);
			}

			return table;
		}

		#region unused

		public int GetCount(QueryFilter filter = null)
		{
			throw new NotImplementedException();
		}

		#endregion

		public long GetNextOid()
		{
			return ++_lastUsedOid;
		}
	}
}
