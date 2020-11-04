using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	public class IssueItemRepository : GdbItemRepository
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _statusFieldName = "STATUS";

		public IssueItemRepository(Dictionary<Geodatabase, List<Table>> tablesByGeodatabase,
		                           IRepository stateRepository) : base(tablesByGeodatabase, stateRepository) { }

		protected override WorkListStatusSchema CreateStatusSchemaCore(FeatureClassDefinition definition)
		{
			int fieldIndex;

			try
			{
				fieldIndex = definition.FindField(_statusFieldName);
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

		protected override IAttributeReader CreateAttributeReaderCore(FeatureClassDefinition definition)
		{
			return new AttributeReader(definition,
			                           Attributes.QualityConditionName,
			                           Attributes.IssueCodeDescription,
									   Attributes.InvolvedObjects,
			                           Attributes.IssueSeverity,
			                           Attributes.IssueCode);
		}

		protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass source)
		{
			int id = CreateItemIDCore(row, source);

			return RefreshState(new IssueItem(id, row, ((DatabaseSourceClass) source).AttributeReader));
		}

		protected override ISourceClass CreateSourceClassCore(
			GdbTableIdentity identity,
			IAttributeReader attributeReader,
			WorkListStatusSchema statusSchema)
		{
			Assert.ArgumentNotNull(attributeReader, nameof(attributeReader));
			Assert.ArgumentNotNull(statusSchema, nameof(statusSchema));

			return new DatabaseSourceClass(identity, statusSchema, attributeReader);
		}

		protected override void RefreshCore(IWorkItem item,
		                                    ISourceClass sourceClass,
		                                    Row row)
		{
			// todo daro: really needed here? Only geometry is updated but
			//			  the work itmes's state remains the same.
			item.Status = ((DatabaseSourceClass)sourceClass).GetStatus(row);
		}

		protected override async Task UpdateCoreAsync(IWorkItem item, ISourceClass source, Row row)
		{
			Table table = OpenFeatureClass(source);

			try
			{
				var databaseSourceClass = (DatabaseSourceClass) source;

				WorkItemStatus priorStatus = ((DatabaseSourceClass)source).GetStatus(row);

				string description = GetOperationDescription(item.Status);

				_msg.Info($"{description}, {item.Proxy}");

				var operation = new EditOperation {Name = description};

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
				table?.Dispose();
			}
		}

		private static string GetOperationDescription(WorkItemStatus status)
		{
			string operationDescription;
			switch (status)
			{
				case WorkItemStatus.Todo:
					operationDescription = "Set status of work item to 'Todo'";
					break;

				case WorkItemStatus.Done:
					operationDescription = "Set status of work item to 'Done'";
					break;

				default:
					throw new ArgumentException($"Invalid status for operation: {status}");
			}

			return operationDescription;
		}
	}
}
