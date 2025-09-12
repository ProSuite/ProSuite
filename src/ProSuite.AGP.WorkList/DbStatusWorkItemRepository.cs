using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

public class DbStatusWorkItemRepository : GdbItemRepository
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	/// <summary>
	/// SDE or GDB file path to the single, current workspace in which all source tables reside.
	/// Pro creates a SDE file in C:\Users\USER\AppData\local\temp
	/// </summary>
	private readonly string _catalogPath;

	public DbStatusWorkItemRepository(IList<ISourceClass> sourceClasses,
	                                  IWorkItemStateRepository workItemStateRepository,
	                                  string catalogPath) : base(
		sourceClasses, workItemStateRepository)
	{
		// Cannot inject geodatabase type here because it's created in a QueuedTask
		// and used in a pluggable datasource worker thread. This throws a Pro
		// CalledOnWrongThread exception.
		_catalogPath = catalogPath;
	}

	public override bool CanUseTableSchema(IWorkListItemDatastore workListItemSchema)
	{
		return workListItemSchema != null &&
		       SourceClasses.Any(workListItemSchema.ContainsSourceClass);
	}

	public override void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo)
	{
		try
		{
			foreach (ISourceClass sourceClass in SourceClasses)
			{
				using Table table = OpenTable(sourceClass);

				if (table != null)
				{
					using TableDefinition definition = table.GetDefinition();

					// TODO: Make independent of attribute list, use standard AttributeRoles
					var attributes = new[]
					                 {
						                 Attributes.QualityConditionName,
						                 Attributes.IssueCodeDescription,
						                 Attributes.InvolvedObjects,
						                 Attributes.IssueSeverity,
						                 Attributes.IssueCode,
						                 Attributes.IssueDescription
					                 };

					sourceClass.AttributeReader =
						tableSchemaInfo?.CreateAttributeReader(definition, attributes);
				}
				else
				{
					_msg.Warn(
						$"Cannot prepare table schema due to missing source table {sourceClass.Name}");
				}
			}
		}
		catch (Exception ex)
		{
			_msg.Debug(ex.Message, ex);
		}
	}

	protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass sourceClass)
	{
		var dbSourceClass = (DatabaseSourceClass) sourceClass;

		WorkItemStatus status = dbSourceClass.GetStatus(row);

		// Create table identity only once for better performance:
		GdbTableIdentity tableIdentity = dbSourceClass.TableIdentity;

		var rowIdentity = new GdbRowIdentity(row.GetObjectID(), tableIdentity);

		return new DbStatusWorkItem(sourceClass.GetUniqueTableId(), rowIdentity, status);
	}

	protected override async Task SetStatusCoreAsync(IWorkItem item,
	                                                 WorkItemStatus status)
	{
		Table table = null;
		try
		{
			GdbTableIdentity tableId = item.GdbRowProxy.Table;

			var source = SourceClasses.OfType<DatabaseSourceClass>()
			                          .FirstOrDefault(s => s.Uses(tableId));
			Assert.NotNull(source);

			table = OpenTable(source);
			Assert.NotNull(table, $"Cannot set status for missing table {source.Name}");

			string description = GetOperationDescription(item);

			_msg.Info($"{description}, {item.GdbRowProxy}");

			var operation = new EditOperation { Name = description };
			operation.Callback(context =>
			{
				// ReSharper disable once AccessToDisposedClosure
				Row row = GdbQueryUtils.GetRow(table, item.ObjectID);

				context.Invalidate(row);
			}, table);

			operation.Modify(table, item.ObjectID,
			                 source.StatusField,
			                 source.GetValue(status));

			await operation.ExecuteAsync();

			// NOTE: Important to call base.SetStatusCoreAsync() after operation.ExececuteAsync() because this triggers
			//		 IRowCache.ProcessChanges > ProcessUpdates where the edit is evaluated whether it's only
			//		 a status edit or the geometry has changed. The latter requires to update
			//		 the work list's SpatialHashSearcher.
			await base.SetStatusCoreAsync(item, status);
		}
		catch (Exception e)
		{
			_msg.Error($"Error set status of work item {item.OID}, {item.GdbRowProxy}", e);
			throw;
		}
		finally
		{
			table?.Dispose();
		}
	}

	protected override Table OpenTable(ISourceClass sourceClass)
	{
		Table table = null;
		try
		{
			Geodatabase geodatabase = WorkspaceUtils.OpenGeodatabase(_catalogPath);
			table = geodatabase.OpenDataset<Table>(sourceClass.Name);
		}
		catch (Exception e)
		{
			_msg.Warn($"Error opening source table {sourceClass.Name}: {e.Message}.", e);
		}

		return table;
	}

	private static string GetOperationDescription(IWorkItem item)
	{
		WorkItemStatus oldState = item.Status;

		switch (oldState)
		{
			case WorkItemStatus.Todo:
				return $"Set status of work item OID={item.OID} to 'Corrected'";

			case WorkItemStatus.Done:
				return $"Set status of work item OID={item.OID} to 'Not Corrected'";

			default:
				throw new ArgumentException($"Invalid status for operation: {item}");
		}
	}
}
