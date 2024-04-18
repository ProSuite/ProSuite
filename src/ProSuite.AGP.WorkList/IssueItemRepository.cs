using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.AGP.WorkList
{
	public class IssueItemRepository : GdbItemRepository
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _statusFieldName = "STATUS";

		public IssueItemRepository(IEnumerable<Table> tables, IRepository stateRepository,
		                           [CanBeNull] IWorkListItemDatastore tableSchema = null,
		                           string definitionQuery = null) : base(
			tables, stateRepository, tableSchema, definitionQuery) { }

		public IssueItemRepository(IEnumerable<Tuple<Table, string>> tableWithDefinitionQuery,
		                           IRepository workItemStateRepository) : base(
			tableWithDefinitionQuery, workItemStateRepository) { }

		protected override WorkListStatusSchema CreateStatusSchemaCore(TableDefinition definition)
		{
			int fieldIndex;

			try
			{
				fieldIndex = definition.FindField(_statusFieldName);

				if (fieldIndex < 0)
				{
					throw new ArgumentException($"No field {_statusFieldName}");
				}
			}
			catch (Exception e)
			{
				_msg.Error($"Error find field {_statusFieldName} in {definition.GetName()}", e);
				throw;
			}

			return new WorkListStatusSchema(_statusFieldName, fieldIndex,
			                                (int) IssueCorrectionStatus.NotCorrected,
			                                (int) IssueCorrectionStatus.Corrected);
		}

		protected override IAttributeReader CreateAttributeReaderCore(
			TableDefinition definition,
			IWorkListItemDatastore tableSchema)
		{
			Attributes[] attributes = new[]
			                          {
				                          Attributes.QualityConditionName,
				                          Attributes.IssueCodeDescription,
				                          Attributes.InvolvedObjects,
				                          Attributes.IssueSeverity,
				                          Attributes.IssueCode,
				                          Attributes.IssueDescription
			                          };

			return tableSchema != null
				       ? tableSchema.CreateAttributeReader(definition, attributes)
				       : new StatusOnlyAttributeReader(definition);
		}

		protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass source)
		{
			long id = GetNextOid(row);

			IAttributeReader reader = source.AttributeReader;

			IIssueItem item = new IssueItem(id, row);

			reader?.ReadAttributes(row, item, source);

			return RefreshState(item);
		}

		protected override ISourceClass CreateSourceClassCore(GdbTableIdentity identity,
		                                                      IAttributeReader attributeReader,
		                                                      WorkListStatusSchema statusSchema,
		                                                      string definitionQuery = null)
		{
			Assert.ArgumentNotNull(attributeReader, nameof(attributeReader));
			Assert.ArgumentNotNull(statusSchema, nameof(statusSchema));

			return new DatabaseSourceClass(identity, statusSchema, attributeReader,
			                               definitionQuery);
		}

		protected override void AdaptSourceFilter(QueryFilter filter, ISourceClass sourceClass)
		{
			// So far no manipulation of the filter is needed. The where clause is set in the caller by using
			// sourceClass.CreateWhereClause(statusFilter).
		}

		protected override void RefreshCore(IWorkItem item,
		                                    ISourceClass sourceClass,
		                                    Row row)
		{
			// todo daro: use AttributeReader?
			// todo daro: really needed here? Only geometry is updated but
			//			  the work item's state remains the same.
			item.Status = ((DatabaseSourceClass) sourceClass).GetStatus(row);
		}

		protected override async Task SetStatusCoreAsync(IWorkItem item, ISourceClass source)
		{
			Table table = OpenTable(source);
			Assert.NotNull(table);

			try
			{
				var databaseSourceClass = (DatabaseSourceClass) source;

				string description = GetOperationDescription(item);

				_msg.Info($"{description}, {item.Proxy}");

				var operation = new EditOperation { Name = description };
				operation.Callback(context =>
				{
					// ReSharper disable once AccessToDisposedClosure
					Row row = GdbQueryUtils.GetRow(table, item.ObjectID);
					context.Invalidate(row);
				}, table);

				// todo daro CancelMessage, AbortMessage
				string fieldName = databaseSourceClass.StatusFieldName;
				object value = databaseSourceClass.GetValue(item.Status);

				operation.Modify(table, item.ObjectID, fieldName, value);

				await operation.ExecuteAsync();
			}
			catch (Exception e)
			{
				_msg.Error($"Error set status of work item {item.OID}, {item.Proxy}", e);
				throw;
			}
			finally
			{
				table.Dispose();
			}
		}

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

		private class StatusOnlyAttributeReader : IAttributeReader
		{
			private readonly TableDefinition _tableDefinition;

			public StatusOnlyAttributeReader(TableDefinition tableDefinition)
			{
				_tableDefinition = tableDefinition;
			}

			#region Implementation of IAttributeReader

			public T GetValue<T>(Row row, Attributes attribute)
			{
				return default;
			}

			public void ReadAttributes(Row fromRow, IIssueItem forItem, ISourceClass source)
			{
				forItem.Status = ((DatabaseSourceClass) source).GetStatus(fromRow);
			}

			#endregion
		}
	}
}
