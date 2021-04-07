using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
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
		                               Dictionary<Table, List<long>> selection,
		                               IRepository stateRepository) : base(tablesByGeodatabase, stateRepository)
		{
			foreach (var pair in selection)
			{
				var id = new GdbTableIdentity(pair.Key);
				ISourceClass sourceClass = GeodatabaseBySourceClasses.Keys.FirstOrDefault(s => s.Uses(id));

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

		protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass source)
		{
			int id = CreateItemIDCore(row, source);

			return RefreshState(new SelectionItem(id, row));
		}

		protected override ISourceClass CreateSourceClassCore(
			GdbTableIdentity identity,
			IAttributeReader attributeReader,
			WorkListStatusSchema statusSchema)
		{
			return new SelectionSourceClass(identity);
		}

		protected override IEnumerable<Row> GetRowsCore(ISourceClass sourceClass, QueryFilter filter, bool recycle)
		{
			Assert.True(_oidsBySource.TryGetValue(sourceClass, out List<long> oids),
			            "unexpected source class");

			if (filter == null)
			{
				filter = new QueryFilter { ObjectIDs = oids };
			}

			if (filter is SpatialQueryFilter spatialFilter)
			{
				spatialFilter.SearchOrder = SearchOrder.Attribute;
			}

			return base.GetRowsCore(sourceClass, filter, recycle);
		}

		protected override async Task SetStatusCoreAsync(IWorkItem item, ISourceClass source)
		{
			await Task.Run(() => WorkItemStateRepository.Update(item));
		}
	}
}
