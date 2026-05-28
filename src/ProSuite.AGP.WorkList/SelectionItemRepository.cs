using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

public class SelectionItemRepository : GdbItemRepository
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public SelectionItemRepository(IList<ISourceClass> sourceClasses,
	                               IWorkItemStateRepository stateRepository) : base(
		sourceClasses, stateRepository) { }

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
}
