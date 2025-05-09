using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
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

		protected GdbItemRepository(IEnumerable<Table> tables,
		                            IWorkItemStateRepository workItemStateRepository,
		                            // ReSharper disable once UnusedParameter.Local because it is required by dynamic instantiation
		                            [CanBeNull] IWorkListItemDatastore tableSchema = null)
		{
			WorkItemStateRepository = workItemStateRepository;

			foreach (Table table in tables)
			{
				ISourceClass sourceClass =
					CreateSourceClass(new GdbTableIdentity(table), table.GetDefinition(),
					                  null);

				SourceClasses.Add(sourceClass);
			}

			int distinctIds =
				SourceClasses.Select(sc => sc.GetUniqueTableId()).Distinct().Count();

			if (distinctIds != SourceClasses.Count)
			{
				// TODO: Extract problematic tables
				_msg.Warn("Some source classes have duplicate table ids. " +
				          "Please ensure they have unique table names if they are not registered");
			}
		}

		// TODO: Create basic record for each source class: Table, DefinitionQuery, StatusSchema
		protected GdbItemRepository(IEnumerable<Tuple<Table, string>> tableWithDefinitionQuery,
		                            IWorkItemStateRepository workItemStateRepository,
		                            [CanBeNull] IWorkListItemDatastore tableSchema = null)
		{
			foreach ((Table table, string definitionQuery) in tableWithDefinitionQuery)
			{
				ISourceClass sourceClass =
					CreateSourceClass(new GdbTableIdentity(table), table.GetDefinition(),
					                  tableSchema, definitionQuery);
				SourceClasses.Add(sourceClass);
			}

			WorkItemStateRepository = workItemStateRepository;
		}

		protected GdbItemRepository(
			IList<DbStatusSourceClassDefinition> sourceClassDefinitions,
			IWorkItemStateRepository workItemStateRepository)
		{
			HashSet<nint> datastoreHandles = new HashSet<IntPtr>();
			foreach (DbStatusSourceClassDefinition sourceDefinition in sourceClassDefinitions)
			{
				ISourceClass sourceClass = new DatabaseSourceClass(
					new GdbTableIdentity(sourceDefinition.Table), sourceDefinition.StatusSchema,
					sourceDefinition.AttributeReader, sourceDefinition.DefinitionQuery);

				SourceClasses.Add(sourceClass);

				IntPtr datastoreHandle = sourceDefinition.Table.GetDatastore().Handle;
				datastoreHandles.Add(datastoreHandle);
			}

			Assert.True(datastoreHandles.Count < 2,
			            "Multiple geodatabases are referenced by the work list's source classes.");

			CurrentWorkspace =
				sourceClassDefinitions.FirstOrDefault()?.Table.GetDatastore() as Geodatabase;

			if (CurrentWorkspace == null)
			{
				_msg.Warn("No workspace found for the work list.");
			}

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

		public List<ISourceClass> SourceClasses { get; } = new();

		public string WorkListDefinitionFilePath
		{
			get => WorkItemStateRepository.WorkListDefinitionFilePath;
			set => WorkItemStateRepository.WorkListDefinitionFilePath = value;
		}

		public abstract void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo);

		public abstract bool CanUseTableSchema(
			[CanBeNull] IWorkListItemDatastore workListItemSchema);

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

				// Source classes can set the respective filters / definition queries
				// TODO: Consider getting only the right status, but that means
				// extra round trips:
				statusFilter = null;
				filter.WhereClause = sourceClass.CreateWhereClause(statusFilter);

				// Selection Item ObjectIDs to filter out, or change of SearchOrder:
				AdaptSourceFilter(filter, sourceClass);

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
			ITableReference tableId = item.GdbRowProxy.Table;

			// todo daro: log message
			ISourceClass source =
				SourceClasses.FirstOrDefault(sc => sc.Uses(tableId));
			Assert.NotNull(source);

			Row row = GetSourceRow(source, item.ObjectID);
			Assert.NotNull(row);

			if (row is Feature feature)
			{
				((WorkItem) item).SetGeometryFromFeature(feature);
			}

			RefreshCore(item, source, row);
		}

		public void RefreshGeometry(IWorkItem item)
		{
			ITableReference tableId = item.GdbRowProxy.Table;

			// todo daro: log message
			ISourceClass source =
				SourceClasses.FirstOrDefault(sc => sc.Uses(tableId));
			Assert.NotNull(source);

			Row row = GetSourceRow(source, item.ObjectID);
			Assert.NotNull(row);

			if (row is Feature feature)
			{
				item.Geometry = GeometryUtils.Buffer(feature.GetShape(), 10);
			}
		}

		[CanBeNull]
		public Row GetSourceRow(ISourceClass sourceClass, long oid)
		{
			var filter = new QueryFilter { ObjectIDs = new List<long> { oid } };

			return GetRowsCore(sourceClass, filter, recycle: false).FirstOrDefault();
		}

		protected virtual void RefreshCore([NotNull] IWorkItem item,
		                                   [NotNull] ISourceClass sourceClass,
		                                   [NotNull] Row row) { }

		// TODO: Rename to Update?
		public void SetVisited(IWorkItem item)
		{
			WorkItemStateRepository.Update(item);
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

		// TODO: Workspace property, the source class references the table
		[CanBeNull]
		public Row GetGdbItemRow(IWorkItem workItem)
		{
			Geodatabase workspace = workItem.GdbRowProxy.Table.Workspace.OpenGeodatabase();

			return workItem.GdbRowProxy.GetRow(workspace);
		}

		protected abstract void AdaptSourceFilter([NotNull] QueryFilter filter,
		                                          [NotNull] ISourceClass sourceClass);

		protected virtual void UpdateStateRepositoryCore(string path) { }

		protected virtual Task SetStatusCoreAsync([NotNull] IWorkItem item,
		                                          [NotNull] ISourceClass source)
		{
			return Task.FromResult(0);
		}

		private IEnumerable<Row> GetRowsCore([NotNull] ISourceClass sourceClass,
		                                     [CanBeNull] QueryFilter filter,
		                                     bool recycle)
		{
			Table table = OpenTable(sourceClass);

			if (table == null)
			{
				_msg.Warn($"No items for {sourceClass.Name} can be loaded.");
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

		[NotNull]
		protected abstract ISourceClass CreateSourceClassCore(
			GdbTableIdentity identity,
			[CanBeNull] IAttributeReader attributeReader,
			[CanBeNull] WorkListStatusSchema statusSchema,
			string definitionQuery = null);

		[CanBeNull]
		protected Table OpenTable([NotNull] ISourceClass sourceClass)
		{
			Table table = null;
			try
			{
				if (CurrentWorkspace == null)
				{
					// NOTE: This can lead to using a different instance of the same workspace
					// because opening a new Geodatabase with the Connector of an existing
					// Geodatabase can in some cases result in a different instance!
					table = sourceClass.OpenDataset<Table>();
				}
				else
				{
					// Therefore try using the (single) workspace of the repository:
					table = CurrentWorkspace.OpenDataset<Table>(sourceClass.Name);
				}
			}
			catch (Exception e)
			{
				_msg.Warn($"Error opening source table {sourceClass.Name}: {e.Message}.", e);
			}

			return table;
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
