using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	// todo daro: SetStatusDone !!!!
	// Note maybe all SDK code, like open workspace, etc. should be in here. Not in DatabaseSourceClass for instance.
	public abstract class GdbItemRepository : IWorkItemRepository
	{
		protected GdbItemRepository(Dictionary<Geodatabase, List<Table>> tablesByGeodatabase, IRepository workItemStateRepository)
		{
			RegisterDatasets(tablesByGeodatabase);

			WorkItemStateRepository = workItemStateRepository;
		}

		public IRepository WorkItemStateRepository { get; }

		public Dictionary<ISourceClass, Geodatabase> GeodatabaseBySourceClasses { get; } = new Dictionary<ISourceClass, Geodatabase>();

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true)
		{
			foreach (ISourceClass sourceClass in GeodatabaseBySourceClasses.Keys)
			{
				foreach (Row row in GetRowsCore(sourceClass, filter, recycle))
				{
					IWorkItem item = CreateWorkItemCore(row, sourceClass);

					yield return WorkItemStateRepository.Refresh(item);
				}
			}

			// return GeodatabaseBySourceClasses.Keys.SelectMany(sourceClass => GetItemsCore(sourceClass, filter, recycle));
		}

		public IEnumerable<IWorkItem> GetItems(GdbTableIdentity tableId, QueryFilter filter, bool recycle = true)
		{
			foreach (ISourceClass sourceClass in GeodatabaseBySourceClasses.Keys.Where(source => source.Uses(tableId)))
			{
				foreach (Row row in GetRowsCore(sourceClass, filter, recycle))
				{
					yield return CreateWorkItemCore(row, sourceClass);
				}
			}

			// return GeodatabaseBySourceClasses.Keys.Where(source => source.Uses(table)).SelectMany(sourceClass => GetItemsCore(sourceClass, filter, recycle));
		}

		public void Refresh(IWorkItem item)
		{
			ISourceClass sourceClass = GeodatabaseBySourceClasses.Keys.FirstOrDefault(sc => sc.Uses(item.Proxy.Table));
			// todo daro: log message
			Assert.NotNull(sourceClass);

			var filter = new QueryFilter { ObjectIDs = new List<long> { item.Proxy.ObjectId } };

			Row row = GetRowsCore(sourceClass, filter, recycle: true).FirstOrDefault();
			// todo daro: log message
			Assert.NotNull(row);

			// todo daro: really needed here? Only geometry is updated but
			//			  the work itmes's state remains the same.
			item.Status = sourceClass.GetStatus(row);

			if (row is Feature feature)
			{
				((WorkItem) item).SetGeometryFromFeature(feature);
			}
		}

		public void Update(IWorkItem item)
		{
			// selection work list: stores visited, status in work list definition file
			// issue work list: stores status in db
			WorkItemStateRepository.Update(item);

			GdbTableIdentity tableId = item.Proxy.Table;

			ISourceClass source = GeodatabaseBySourceClasses.Keys.FirstOrDefault(s => s.Uses(tableId));
			Assert.NotNull(source);

			UpdateCore(source, item);
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

		protected virtual void UpdateCore([NotNull] ISourceClass source, [NotNull] IWorkItem item) { }

		protected virtual IEnumerable<Row> GetRowsCore([NotNull] ISourceClass sourceClass, [CanBeNull] QueryFilter filter, bool recycle)
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
		protected virtual DatabaseStatusSchema CreateStatusSchemaCore(FeatureClassDefinition definition)
		{
			return null;
		}

		[NotNull]
		protected abstract IAttributeReader CreateAttributeReaderCore([NotNull] FeatureClassDefinition definition);

		[NotNull]
		protected abstract IWorkItem CreateWorkItemCore([NotNull] Row row, ISourceClass source);

		[NotNull]
		protected abstract ISourceClass CreateSourceClassCore(GdbTableIdentity identity,
		                                                      [NotNull] IAttributeReader attributeReader,
		                                                      [CanBeNull] DatabaseStatusSchema statusSchema = null);

		private void RegisterDatasets(Dictionary<Geodatabase, List<Table>> tablesByGeodatabase)
		{
			foreach (var pair in tablesByGeodatabase)
			{
				Geodatabase geodatabase = pair.Key;
				var definitions = geodatabase.GetDefinitions<FeatureClassDefinition>().ToLookup(d => d.GetName());

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

		private ISourceClass CreateSourceClass(GdbTableIdentity identity, FeatureClassDefinition definition)
		{
			IAttributeReader attributeReader = CreateAttributeReaderCore(definition);

			DatabaseStatusSchema statusSchema = CreateStatusSchemaCore(definition);

			ISourceClass sourceClass = CreateSourceClassCore(identity, attributeReader, statusSchema);

			return sourceClass;
		}

		#region unused

		public int GetCount(QueryFilter filter = null)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<PluginField> GetFields(IEnumerable<string> fieldNames = null)
		{
			throw new NotImplementedException();
		}

		#endregion

		protected virtual int CreateItemIDCore(Row row, ISourceClass source)
		{
			long oid = row.GetObjectID();

			// oid = 666, tableId = 42 => 42666
			return (int) (Math.Pow(10, Math.Floor(Math.Log10(oid) + 1)) * source.Id + oid);
		}

		protected IWorkItem RefreshState(IWorkItem item)
		{
			return WorkItemStateRepository.Refresh(item);
		}
	}
}
