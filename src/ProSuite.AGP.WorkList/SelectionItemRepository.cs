using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	public class SelectionItemRepository : GdbItemRepository
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public SelectionItemRepository(IList<SelectionSourceClass> sourceClasses,
		                               IWorkItemStateRepository stateRepository) : base(
			sourceClasses.OfType<ISourceClass>().ToList(), stateRepository) { }

		protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass sourceClass)
		{
			long tableId = sourceClass.GetUniqueTableId();

			return new SelectionItem(tableId, row);
		}

		protected override Table OpenTable(ISourceClass sourceClass)
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

		protected override Task SetStatusCoreAsync(IWorkItem item, ISourceClass source)
		{
			WorkItemStateRepository.UpdateState(item);

			return Task.CompletedTask;
		}

		public override bool CanUseTableSchema(IWorkListItemDatastore workListItemSchema)
		{
			// We can use anything, no schema dependency:
			return true;
		}

		protected override void AdaptSourceFilter(QueryFilter filter,
		                                          ISourceClass sourceClass)
		{
			filter.ObjectIDs = ((SelectionSourceClass) sourceClass).Oids;

			if (filter is SpatialQueryFilter spatialFilter)
			{
				// Probably depends on the count of OIDs vs. the spatial filter's selectivity:
				spatialFilter.SearchOrder = SearchOrder.Attribute;
			}
		}

		public override void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo)
		{
			// No specific schema info is necessary/available
			return;
		}
	}
}
