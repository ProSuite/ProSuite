using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	// todo daro: SetStatusDone !!!!
	// Note maybe all SDK code, like open workspace, etc. should be in here. Not in DatabaseSourceClass for instance.
	public abstract class GdbItemRepository : IWorkItemRepository
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private int _lastUsedOid;

		protected GdbItemRepository(IEnumerable<Table> tables,
		                            IRepository workItemStateRepository,
		                            [CanBeNull] IWorkListItemDatastore tableSchema = null,
		                            string definitionQuery = null)
		{
			WorkItemStateRepository = workItemStateRepository;
			TableSchema = tableSchema;

			foreach (Table table in tables)
			{
				ISourceClass sourceClass =
					CreateSourceClass(new GdbTableIdentity(table), table.GetDefinition(),
					                  tableSchema, definitionQuery);

				//if (! string.IsNullOrEmpty(definitionQuery) && WorkItemStateRepository.)
				//{
				//	// todo daro: log message
				//	_msg.Debug($"Definition query: {definitionQuery}");
				//}

				SourceClasses.Add(sourceClass);
			}
		}

		// TODO: Refactor to use ISourceClass created by (virtual) method in environment!
		// -> This allows for adaptive definition query depending on db source class
		protected GdbItemRepository(IEnumerable<Tuple<Table, string>> tableWithDefinitionQuery,
		                            IRepository workItemStateRepository)
		{
			foreach ((Table table, string definitionQuery) in tableWithDefinitionQuery)
			{
				ISourceClass sourceClass =
					CreateSourceClass(new GdbTableIdentity(table), table.GetDefinition(),
					                  null, definitionQuery);
				SourceClasses.Add(sourceClass);
			}

			WorkItemStateRepository = workItemStateRepository;
		}

		protected IRepository WorkItemStateRepository { get; }

		[CanBeNull]
		public IWorkListItemDatastore TableSchema { get; private set; }

		public List<ISourceClass> SourceClasses { get; } = new();

		public void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo)
		{
			TableSchema = tableSchemaInfo;

			foreach (ISourceClass sourceClass in SourceClasses)
			{
				Table table = sourceClass.OpenDataset<Table>();
				sourceClass.AttributeReader = CreateAttributeReaderCore(
					table.GetDefinition(), tableSchemaInfo);
			}
		}

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true)
		{
			foreach (ISourceClass sourceClass in SourceClasses)
			{
				int count = 0;

				Stopwatch watch = _msg.DebugStartTiming();

				foreach (Row row in GetRowsCore(sourceClass, filter, recycle))
				{
					IWorkItem item = CreateWorkItemCore(row, sourceClass);

					count += 1;
					yield return WorkItemStateRepository.Refresh(item);
				}

				_msg.DebugStopTiming(
					watch, $"GetItems() {sourceClass.Name}: {count} items");
			}
		}

		public IEnumerable<IWorkItem> GetItems(Geometry areaOfInterest,
		                                       WorkItemStatus? statusFilter,
		                                       bool recycle = true)
		{
			foreach (ISourceClass sourceClass in SourceClasses)
			{
				int count = 0;

				Stopwatch watch = _msg.DebugStartTiming();

				QueryFilter filter = areaOfInterest != null
					                     ? GdbQueryUtils.CreateSpatialFilter(areaOfInterest)
					                     : new QueryFilter();

				filter.WhereClause = sourceClass.CreateWhereClause(statusFilter);

				foreach (Row row in GetRowsCore(sourceClass, filter, recycle))
				{
					IWorkItem item = CreateWorkItemCore(row, sourceClass);

					count += 1;
					yield return WorkItemStateRepository.Refresh(item);
				}

				_msg.DebugStopTiming(
					watch, $"GetItems() {sourceClass.Name}: {count} items");
			}
		}

		public void Refresh(IWorkItem item)
		{
			GdbTableIdentity tableId = item.Proxy.Table;

			// todo daro: log message
			ISourceClass source =
				SourceClasses.FirstOrDefault(sc => sc.Uses(tableId));
			Assert.NotNull(source);

			Row row = GetRow(source, item.Proxy.ObjectId);
			Assert.NotNull(row);

			if (row is Feature feature)
			{
				((WorkItem) item).SetGeometryFromFeature(feature);
			}

			RefreshCore(item, source, row);
		}

		// todo daro reorder members
		[CanBeNull]
		private Row GetRow([NotNull] ISourceClass sourceClass, long oid)
		{
			var filter = new QueryFilter { ObjectIDs = new List<long> { oid } };

			// todo daro: log message
			return GetRowsCore(sourceClass, filter, recycle: true).FirstOrDefault();
		}

		protected virtual void RefreshCore([NotNull] IWorkItem item,
		                                   [NotNull] ISourceClass sourceClass,
		                                   [NotNull] Row row) { }

		public void SetVisited(IWorkItem item)
		{
			WorkItemStateRepository.Update(item);
		}

		public async Task SetStatus(IWorkItem item, WorkItemStatus status)
		{
			item.Status = status;

			GdbTableIdentity tableId = item.Proxy.Table;

			ISourceClass source =
				SourceClasses.FirstOrDefault(s => s.Uses(tableId));
			Assert.NotNull(source);

			// todo daro: read / restore item again from db? restore pattern in case of failure?
			await SetStatusCoreAsync(item, source);
		}

		public void UpdateStateRepository(string path)
		{
			UpdateStateRepositoryCore(path);
		}

		public Task UpdateAsync(IWorkItem item)
		{
			// todo daro: revise
			return Task.FromResult(0);
		}

		// todo daro: rename?
		public void UpdateVolatileState(IEnumerable<IWorkItem> items)
		{
			WorkItemStateRepository.UpdateVolatileState(items);
		}

		public void Commit()
		{
			WorkItemStateRepository.Commit(SourceClasses);
		}

		public void Discard()
		{
			WorkItemStateRepository.Discard();
		}

		public void SetCurrentIndex(int currentIndex)
		{
			WorkItemStateRepository.CurrentIndex = currentIndex;
		}

		public int GetCurrentIndex()
		{
			return WorkItemStateRepository.CurrentIndex ?? -1;
		}

		protected virtual void UpdateStateRepositoryCore(string path) { }

		protected virtual Task SetStatusCoreAsync([NotNull] IWorkItem item,
		                                          [NotNull] ISourceClass source)
		{
			return Task.FromResult(0);
		}

		protected virtual IEnumerable<Row> GetRowsCore([NotNull] ISourceClass sourceClass,
		                                               [CanBeNull] QueryFilter filter, bool recycle)
		{
			Table table = OpenTable(sourceClass);

			if (table == null)
			{
				yield break;
			}

			try
			{
				// Todo daro: check recycle
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
		protected virtual WorkListStatusSchema CreateStatusSchemaCore(
			[NotNull] TableDefinition definition)
		{
			return null;
		}

		[CanBeNull]
		protected virtual IAttributeReader CreateAttributeReaderCore(
			[NotNull] TableDefinition definition,
			[CanBeNull] IWorkListItemDatastore tableSchema)
		{
			return null;
		}

		[NotNull]
		protected abstract IWorkItem CreateWorkItemCore([NotNull] Row row, ISourceClass source);

		[NotNull]
		protected abstract ISourceClass CreateSourceClassCore(
			GdbTableIdentity identity,
			[CanBeNull] IAttributeReader attributeReader,
			[CanBeNull] WorkListStatusSchema statusSchema,
			string definitionQuery = null);

		[CanBeNull]
		protected static Table OpenTable([NotNull] ISourceClass sourceClass)
		{
			return sourceClass.OpenDataset<Table>();
		}

		private ISourceClass CreateSourceClass(GdbTableIdentity identity,
		                                       TableDefinition definition,
		                                       [CanBeNull] IWorkListItemDatastore tableSchema,
		                                       string definitionQuery = null)
		{
			IAttributeReader attributeReader =
				CreateAttributeReaderCore(definition, tableSchema);

			WorkListStatusSchema statusSchema = CreateStatusSchemaCore(definition);

			return CreateSourceClassCore(identity, attributeReader, statusSchema, definitionQuery);
		}

		#region unused

		public int GetCount(QueryFilter filter = null)
		{
			throw new NotImplementedException();
		}

		#endregion

		protected virtual long GetNextOid([NotNull] Row row)
		{
			return ++_lastUsedOid;
		}

		protected IWorkItem RefreshState(IWorkItem item)
		{
			return WorkItemStateRepository.Refresh(item);
		}
	}
}
