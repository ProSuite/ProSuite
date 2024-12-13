using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	public class DbStatusWorkItemRepository : GdbItemRepository
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Overrides of GdbItemRepository

		public DbStatusWorkItemRepository(
			[NotNull] IEnumerable<DbStatusSourceClassDefinition> sourceClassDefinitions,
			[NotNull] IWorkItemStateRepository workItemStateRepository)
			: base(sourceClassDefinitions, workItemStateRepository) { }

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
			long id = GetNextOid(row);

			DatabaseSourceClass dbSourceClass = (DatabaseSourceClass) sourceClass;

			WorkItemStatus status = dbSourceClass.GetStatus(row);

			return new DbStatusWorkItem(id, sourceClass.GetUniqueTableId(), row, status);
		}

		// TODO: Remove other two constructors who need this method
		protected override ISourceClass CreateSourceClassCore(
			GdbTableIdentity identity, IAttributeReader attributeReader,
			WorkListStatusSchema statusSchema,
			string definitionQuery = null)
		{
			throw new NotImplementedException();
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

				// todo daro CancelMessage, AbortMessage
				string fieldName = databaseSourceClass.StatusSchema.FieldName;
				object value = databaseSourceClass.GetValue(item.Status);

				operation.Modify(table, item.ObjectID, fieldName, value);

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
