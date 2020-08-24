using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.AGP.WorkList
{
	public class SelectionItemRepository : GdbItemRepository
	{
		private readonly Dictionary<ISourceClass, List<long>> _oidsBySource =
			new Dictionary<ISourceClass, List<long>>();

		// todo daro: rafactor SelectionItemRepository(Dictionary<IWorkspaceContext, GdbTableIdentity>, Dictionary<GdbTableIdentity, List<long>>)
		public SelectionItemRepository(Dictionary<Geodatabase, List<Table>> tablesByGeodatabase,
		                               Dictionary<Table, List<long>> selection) : base(tablesByGeodatabase)
		{
			foreach (var pair in selection)
			{
				Table table = pair.Key;
				ISourceClass sourceClass = GeodatabaseBySourceClasses.Keys.FirstOrDefault(s => s.Uses(table));

				if (sourceClass == null)
				{
					// todo daro: assert?
					continue;
				}

				if (_oidsBySource.TryGetValue(sourceClass, out List<long> ids))
				{
					// todo daro: assert?
					//			  should never be the case because values of SourceClassesByGeodatabase should be distinct
					ids.AddRange(ids);
				}
				else
				{
					_oidsBySource.Add(sourceClass, pair.Value);
				}
			}
		}

		protected override IAttributeReader CreateAttributeReaderCore(
			FeatureClassDefinition definition)
		{
			return new AttributeReader(definition);
		}

		protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass source)
		{
			int id = CreateItemIDCore(row, source);

			return new SelectionItem(id, row, source.AttributeReader);
		}

		protected override ISourceClass CreateSourceClassCore(GdbTableIdentity identity,
		                                                      IAttributeReader attributeReader,
		                                                      DatabaseStatusSchema statusSchema =
			                                                      null)
		{
			return new SelectionSourceClass(identity, attributeReader);
		}

		protected override IEnumerable<IWorkItem> GetItemsCore(ISourceClass sourceClass,
		                                                       QueryFilter filter, bool recycle)
		{
			Assert.True(_oidsBySource.TryGetValue(sourceClass, out List<long> oids),
			            "unexpected source class");

			if (filter == null)
			{
				filter = new QueryFilter {ObjectIDs = new ReadOnlyCollection<long>(oids)};
			}

			if (filter is SpatialQueryFilter spatialFilter)
			{
				spatialFilter.SearchOrder = SearchOrder.Attribute;
			}

			return base.GetItemsCore(sourceClass, filter, recycle);
		}
	}
}
