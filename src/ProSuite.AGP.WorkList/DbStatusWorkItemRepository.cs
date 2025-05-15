using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

public class DbStatusWorkItemRepository : GdbItemRepository
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	/// <summary>
	/// SDE file path to the single, current workspace in which all source tables reside.
	/// Pro creates a SDE file in C:\Users\USER\AppData\local\temp
	/// </summary>
	private readonly Uri _sdeFilePath;

	public DbStatusWorkItemRepository(IList<ISourceClass> sourceClasses,
	                                  IWorkItemStateRepository workItemStateRepository,
	                                  Uri sdeFilePath) : base(
		sourceClasses, workItemStateRepository)
	{
		// Cannot inject geodatabase type here because it's created in a QueuedTask
		// and used in a pluggable datasource worker thread. This throws a Pro
		// CalledOnWrongThread exception.
		_sdeFilePath = sdeFilePath;
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

					sourceClass.AttributeReader =
						CreateAttributeReader(definition, tableSchemaInfo);
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

		return new DbStatusWorkItem(sourceClass.GetUniqueTableId(), row, status);
	}

	protected override async Task SetStatusCoreAsync(IWorkItem item,
	                                                 ISourceClass source)
	{
		Table table = OpenTable(source);
		Assert.NotNull(table, $"Cannot set status for missing table {source.Name}");

		try
		{
			var databaseSourceClass = (DatabaseSourceClass) source;

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
			                 databaseSourceClass.StatusField,
			                 databaseSourceClass.GetValue(item.Status));

			await operation.ExecuteAsync();
		}
		catch (Exception e)
		{
			_msg.Error($"Error set status of work item {item.OID}, {item.GdbRowProxy}", e);
			throw;
		}
		finally
		{
			table.Dispose();
		}
	}

	protected override Table OpenTable(ISourceClass sourceClass)
	{
		Table table = null;
		try
		{
			Geodatabase geodatabase = OpenGeodatabase(_sdeFilePath);
			table = geodatabase.OpenDataset<Table>(sourceClass.Name);
		}
		catch (Exception e)
		{
			_msg.Warn($"Error opening source table {sourceClass.Name}: {e.Message}.", e);
		}

		return table;
	}

	[NotNull]
	private static Geodatabase OpenGeodatabase(Uri path)
	{
		string filePath = path.AbsolutePath;

		if (filePath.EndsWith(".sde"))
		{
			// NOTE: This ensures that no stale table instance is opened from no stale workspace instance.
			var file = new DatabaseConnectionFile(new Uri(filePath));
			return new Geodatabase(file);
		}

		if (filePath.EndsWith(".gdb"))
		{
			var fgdbPath =
				new FileGeodatabaseConnectionPath(new Uri(filePath, UriKind.Absolute));
			return new Geodatabase(fgdbPath);
		}

		throw new ArgumentOutOfRangeException($"Unknown PATH from {path}");
	}

	private static string GetOperationDescription(IWorkItem item)
	{
		switch (item.Status)
		{
			case WorkItemStatus.Todo:
				 return $"Set status of work item OID={item.OID} to 'Not Corrected'";

			case WorkItemStatus.Done:
				return $"Set status of work item OID={item.OID} to 'Corrected'";

			default:
				throw new ArgumentException($"Invalid status for operation: {item}");
		}
	}
}
