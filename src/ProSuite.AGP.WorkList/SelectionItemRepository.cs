using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

public class SelectionItemRepository : GdbItemRepository
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public SelectionItemRepository(IList<SelectionSourceClass> sourceClasses,
	                               IWorkItemStateRepository stateRepository) : base(
		sourceClasses.OfType<ISourceClass>().ToList(), stateRepository) { }

	public override IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
		QueryFilter filter,
		WorkItemStatus? statusFilter,
		bool excludeGeometry = false)
	{
		return base.GetItems(filter, statusFilter, excludeGeometry)
		           .Where(kvp => FilterByStatus(kvp, statusFilter));
	}

	public override IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(Table table,
		QueryFilter filter,
		WorkItemStatus? statusFilter,
		bool excludeGeometry = false)
	{
		return base.GetItems(table, filter, statusFilter, excludeGeometry)
		           .Where(kvp => FilterByStatus(kvp, statusFilter));
	}

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

	public override bool CanUseTableSchema(IWorkListItemDatastore workListItemSchema)
	{
		// We can use anything, no schema dependency:
		return true;
	}

	public override void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo)
	{
		// No specific schema info is necessary/available
	}

	private bool FilterByStatus(KeyValuePair<IWorkItem, Geometry> kvp, WorkItemStatus? status)
	{
		if (status == null)
		{
			// return all items
			return true;
		}

		IWorkItem item = kvp.Key;

		WorkItemStateRepository.Refresh(item);

		return Equals(status, item.Status);
	}
}
