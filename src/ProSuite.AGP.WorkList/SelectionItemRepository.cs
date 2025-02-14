using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.AGP.WorkList
{
	public class SelectionItemRepository : GdbItemRepository
	{
		private readonly IDictionary<ISourceClass, List<long>> _oidsBySource =
			new Dictionary<ISourceClass, List<long>>();

		public SelectionItemRepository(IEnumerable<Table> tables,
		                               Dictionary<Table, List<long>> selection,
		                               IWorkItemStateRepository stateRepository) : base(
			tables, stateRepository)
		{
			foreach (var pair in selection)
			{
				var gdbTableIdentity = new GdbTableIdentity(pair.Key);
				ISourceClass sourceClass =
					SourceClasses.FirstOrDefault(s => s.Uses(gdbTableIdentity));

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

		protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass sourceClass)
		{
			long rowId = GetNextOid(row);

			long tableId = sourceClass.GetUniqueTableId();

			var item = new SelectionItem(rowId, tableId, row);

			return RefreshState(item);
		}

		protected override ISourceClass CreateSourceClassCore(GdbTableIdentity identity,
		                                                      IAttributeReader attributeReader,
		                                                      WorkListStatusSchema statusSchema,
		                                                      string definitionQuery = null)
		{
			return new SelectionSourceClass(identity);
		}

		protected override async Task SetStatusCoreAsync(IWorkItem item, ISourceClass source)
		{
			await Task.Run(() => WorkItemStateRepository.Update(item));
		}

		public override bool CanUseTableSchema(IWorkListItemDatastore workListItemSchema)
		{
			// We can use anything, no schema dependency:
			return true;
		}

		protected override void AdaptSourceFilter(QueryFilter filter,
		                                          ISourceClass sourceClass)
		{
			Assert.True(_oidsBySource.TryGetValue(sourceClass, out List<long> oids),
			            "unexpected source class");

			filter.ObjectIDs = oids;

			if (filter is SpatialQueryFilter spatialFilter)
			{
				// Probably depends on the count of OIDs vs. the spatial filter's selectivity:
				spatialFilter.SearchOrder = SearchOrder.Attribute;
			}
		}

		protected override void UpdateStateRepositoryCore(string path)
		{
			var xmlStateRepo = (XmlWorkItemStateRepository) WorkItemStateRepository;
			xmlStateRepo.WorkListDefinitionFilePath = path;
		}

		public override void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo)
		{
			// No specific schema info is necessary/available
			return;
		}
	}
}
