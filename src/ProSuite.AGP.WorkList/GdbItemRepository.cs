using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
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

		protected GdbItemRepository(Dictionary<Geodatabase, List<Table>> tablesByGeodatabase,
		                            IRepository workItemStateRepository)
		{
			RegisterDatasets(tablesByGeodatabase);

			WorkItemStateRepository = workItemStateRepository;
		}

		protected IRepository WorkItemStateRepository { get; }

		public Dictionary<ISourceClass, Geodatabase> GeodatabaseBySourceClasses { get; } =
			new Dictionary<ISourceClass, Geodatabase>();

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true)
		{
			foreach (ISourceClass sourceClass in GeodatabaseBySourceClasses.Keys)
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
					watch, $"{nameof(GetItems)}() {sourceClass.Name}: {count} items");
			}

			// return GeodatabaseBySourceClasses.Keys.SelectMany(sourceClass => GetItemsCore(sourceClass, filter, recycle));
		}

		public IEnumerable<IWorkItem> GetItems(GdbTableIdentity tableId, QueryFilter filter,
		                                       bool recycle = true)
		{
			foreach (ISourceClass sourceClass in GeodatabaseBySourceClasses.Keys.Where(
				         source => source.Uses(tableId)))
			{
				int count = 0;

				Stopwatch watch = _msg.DebugStartTiming();

				foreach (Row row in GetRowsCore(sourceClass, filter, recycle))
				{
					count += 1;
					yield return CreateWorkItemCore(row, sourceClass);
				}

				_msg.DebugStopTiming(
					watch, $"{nameof(GetItems)}() {sourceClass.Name}: {count} items");
			}

			// return GeodatabaseBySourceClasses.Keys.Where(source => source.Uses(table)).SelectMany(sourceClass => GetItemsCore(sourceClass, filter, recycle));
		}

		public void Refresh(IWorkItem item)
		{
			GdbTableIdentity tableId = item.Proxy.Table;

			// todo daro: log message
			ISourceClass source =
				GeodatabaseBySourceClasses.Keys.FirstOrDefault(sc => sc.Uses(tableId));
			Assert.NotNull(source);

			Row row = GetRow(source, item.Proxy.ObjectId);
			Assert.NotNull(row);

			if (row is Feature feature)
			{
				((WorkItem) item).SetGeometryFromFeature(feature);
			}

			RefreshCore(item, source, row);
		}

		[CanBeNull]
		private Row GetRow([NotNull] ISourceClass sourceClass, long oid)
		{
			var filter = new QueryFilter {ObjectIDs = new List<long> {oid}};

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
				GeodatabaseBySourceClasses.Keys.FirstOrDefault(s => s.Uses(tableId));
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
			WorkItemStateRepository.Commit();
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

		public void Dispose()
		{
			foreach (Geodatabase gdb in GeodatabaseBySourceClasses.Values)
			{
				gdb?.Dispose();
			}
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
			Table table = OpenFeatureClass(sourceClass);

			if (table == null)
			{
				yield break;
			}

			// Todo daro: check recycle
			foreach (Feature feature in GdbQueryUtils.GetRows<Feature>(
				         table, filter, recycle))
			{
				yield return feature;
			}
		}

		[CanBeNull]
		protected virtual WorkListStatusSchema CreateStatusSchemaCore(
			[NotNull] FeatureClassDefinition definition)
		{
			return null;
		}

		[CanBeNull]
		protected virtual IAttributeReader CreateAttributeReaderCore(
			[NotNull] FeatureClassDefinition definition)
		{
			return null;
		}

		[NotNull]
		protected abstract IWorkItem CreateWorkItemCore([NotNull] Row row, ISourceClass source);

		[NotNull]
		protected abstract ISourceClass CreateSourceClassCore(GdbTableIdentity identity,
		                                                      [CanBeNull]
		                                                      IAttributeReader attributeReader,
		                                                      [CanBeNull]
		                                                      WorkListStatusSchema statusSchema);

		private void RegisterDatasets(Dictionary<Geodatabase, List<Table>> tablesByGeodatabase)
		{
			foreach (var pair in tablesByGeodatabase)
			{
				Geodatabase geodatabase = pair.Key;
				var definitions = geodatabase.GetDefinitions<FeatureClassDefinition>()
				                             .ToLookup(d => d.GetName());

				foreach (Table table in pair.Value)
				{
					var identity = new GdbTableIdentity(table);

					FeatureClassDefinition definition = definitions[identity.Name].FirstOrDefault();

					ISourceClass sourceClass = CreateSourceClass(identity, definition);

					GeodatabaseBySourceClasses.Add(sourceClass, geodatabase);
				}
			}
		}

		[CanBeNull]
		protected Table OpenFeatureClass([NotNull] ISourceClass sourceClass)
		{
			return GeodatabaseBySourceClasses.TryGetValue(sourceClass, out Geodatabase gdb)
				       ? gdb.OpenDataset<Table>(sourceClass.Name)
				       : null;
		}

		//[CanBeNull]
		//protected Table OpenFeatureClass2([NotNull] ISourceClass sourceClass)
		//{
		//	return GeodatabaseBySourceClasses.TryGetValue(sourceClass, out Geodatabase gdb)
		//		       ? sourceClass.OpenFeatureClass(gdb)
		//		       : null;
		//}

		private ISourceClass CreateSourceClass(GdbTableIdentity identity,
		                                       FeatureClassDefinition definition)
		{
			IAttributeReader attributeReader = CreateAttributeReaderCore(definition);

			WorkListStatusSchema statusSchema = CreateStatusSchemaCore(definition);

			ISourceClass sourceClass =
				CreateSourceClassCore(identity, attributeReader, statusSchema);

			return sourceClass;
		}

		#region unused

		public int GetCount(QueryFilter filter = null)
		{
			throw new NotImplementedException();
		}

		public int Count(WorkItemVisibility visibility)
		{
			int count = 0;

			foreach (ISourceClass sourceClass in GeodatabaseBySourceClasses.Keys)
			{
				//string whereClause = sourceClass.GetQuery(visibility);
				//var filter = new QueryFilter {WhereClause = whereClause};

				//count += GetRowsCore(sourceClass, filter, recycle: true).Count();
			}

			return count;
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
