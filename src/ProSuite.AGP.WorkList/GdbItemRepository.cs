using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	// todo daro: SetStatusDone !!!!
	// Note maybe all SDK code, like open workspace, etc. should be in here. Not in DatabaseSourceClass for instance.
	public abstract class GdbItemRepository : IWorkItemRepository
	{
		protected GdbItemRepository(Dictionary<Geodatabase, List<Table>> tablesByGeodatabase)
		{
			RegisterDatasets(tablesByGeodatabase);
		}

		public Dictionary<ISourceClass, Geodatabase> GeodatabaseBySourceClasses { get; } = new Dictionary<ISourceClass, Geodatabase>();

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true)
		{
			foreach (ISourceClass sourceClass in GeodatabaseBySourceClasses.Keys)
			{
				foreach (Row row in GetRowsCore(sourceClass, filter, recycle))
				{
					yield return CreateWorkItemCore(row, sourceClass);
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

		public void UpdateItem(IWorkItem item)
		{
			ISourceClass sourceClass = GeodatabaseBySourceClasses.Keys.FirstOrDefault(sc => sc.Uses(item.Proxy.Table));
			// todo daro: log message
			Assert.NotNull(sourceClass);

			var filter = new QueryFilter {ObjectIDs = new List<long> {item.Proxy.ObjectId}};

			Row row = GetRowsCore(sourceClass, filter, recycle: true).FirstOrDefault();
			// todo daro: log message
			Assert.NotNull(row);

			item.Status = sourceClass.GetStatus(row);

			if (row is Feature feature)
			{
				((WorkItem) item).SetGeometryFromFeature(feature);
			}
		}

		public void UpdateVolatileState(IEnumerable<IWorkItem> items)
		{
			throw new NotImplementedException();
		}

		public void Commit()
		{
			throw new NotImplementedException();
		}

		public void Discard()
		{
			throw new NotImplementedException();
		}

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
		private Table OpenFeatureClass([NotNull] ISourceClass sourceClass)
		{

			return GeodatabaseBySourceClasses.TryGetValue(sourceClass, out Geodatabase gdb)
				       ? gdb.OpenDataset<Table>(sourceClass.Name)
				       : null;
		}

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
	}
}
