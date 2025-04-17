using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	public class DbStatusWorkItemRepository : GdbItemRepository
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// The single, current workspace in which all source tables reside. Not null for DbStatus
		/// </summary>
		private readonly Uri _path;

		#region Overrides of GdbItemRepository

		public DbStatusWorkItemRepository(IList<ISourceClass> sourceClasses,
		                                  IWorkItemStateRepository workItemStateRepository,
		                                  Geodatabase geodatabase) : base(
			sourceClasses, workItemStateRepository)
		{
			// TODO: (daro) inject path?
			_path = geodatabase.GetPath();
		}

		public override bool CanUseTableSchema(IWorkListItemDatastore workListItemSchema)
		{
			if (workListItemSchema == null)
			{
				return false;
			}

			foreach (ISourceClass sourceClass in SourceClasses)
			{
				if (workListItemSchema.ContainsSourceClass(sourceClass))
				{
					return true;
				}
			}

			return false;
		}

		protected override void AdaptSourceFilter(QueryFilter filter, ISourceClass sourceClass)
		{
			// Consider doing this using definition expressions in the source classes
		}

		protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass sourceClass)
		{
			DatabaseSourceClass dbSourceClass = (DatabaseSourceClass) sourceClass;

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
				Geodatabase geodatabase = OpenGeodatabase(_path);
				table = geodatabase.OpenDataset<Table>(sourceClass.Name);
			}
			catch (Exception e)
			{
				_msg.Warn($"Error opening source table {sourceClass.Name}: {e.Message}.", e);
			}

			return table;
		}

		private Geodatabase OpenGeodatabase(Uri path)
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
				var fgdbPath = new FileGeodatabaseConnectionPath(new Uri(filePath));
				return new Geodatabase(fgdbPath);
			}

			throw new ArgumentOutOfRangeException($"Unknown PATH from {path}");
		}

		#endregion

		private static string GetOperationDescription(IWorkItem item)
		{
			string operationDescription;
			switch (item.Status)
			{
				case WorkItemStatus.Todo:
					operationDescription =
						$"Set status of work item OID={item.OID} to 'Not Corrected'";
					break;

				case WorkItemStatus.Done:
					operationDescription = $"Set status of work item OID={item.OID} to 'Corrected'";
					break;

				default:
					throw new ArgumentException($"Invalid status for operation: {item}");
			}

			return operationDescription;
		}

		public override void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo)
		{
			TableSchema = tableSchemaInfo;

			foreach (ISourceClass sourceClass in SourceClasses)
			{
				Table table = OpenTable(sourceClass);

				if (table != null)
				{
					sourceClass.AttributeReader = CreateAttributeReaderCore(
						table.GetDefinition(), tableSchemaInfo);
				}
				else
				{
					_msg.Warn(
						$"Cannot prepare table schema due to missing source table {sourceClass.Name}");
				}
			}
		}
	}
}
