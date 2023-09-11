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
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.AGP.WorkList
{
	public class IssueItemRepository : GdbItemRepository
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _statusFieldName = "STATUS";

		public IssueItemRepository(Dictionary<Geodatabase, List<Table>> tablesByGeodatabase,
		                           IRepository stateRepository) : base(
			tablesByGeodatabase, stateRepository) { }

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

		protected override IAttributeReader CreateAttributeReaderCore(TableDefinition definition)
		{
			return new AttributeReader(definition,
			                           Attributes.QualityConditionName,
			                           Attributes.IssueCodeDescription,
			                           Attributes.InvolvedObjects,
			                           Attributes.IssueSeverity,
			                           Attributes.IssueCode,
			                           Attributes.IssueDescription);
		}

		protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass source)
		{
			long id = GetNextOid(row);

			IAttributeReader reader = source.AttributeReader;

			var item = new IssueItem(id, row);

			if (reader != null)
			{
				item.IssueCode = reader.GetValue<string>(row, Attributes.IssueCode);
				item.IssueCodeDescription =
					reader.GetValue<string>(row, Attributes.IssueCodeDescription);
				item.IssueDescription = reader.GetValue<string>(row, Attributes.IssueDescription);
				item.InvolvedObjects = reader.GetValue<string>(row, Attributes.InvolvedObjects);
				item.QualityCondition =
					reader.GetValue<string>(row, Attributes.QualityConditionName);
				item.TestName = reader.GetValue<string>(row, Attributes.TestName);
				item.TestDescription = reader.GetValue<string>(row, Attributes.TestDescription);
				item.TestType = reader.GetValue<string>(row, Attributes.TestType);
				item.IssueSeverity = reader.GetValue<string>(row, Attributes.IssueSeverity);
				item.StopCondition = reader.GetValue<string>(row, Attributes.IsStopCondition);
				item.Category = reader.GetValue<string>(row, Attributes.Category);
				item.AffectedComponent = reader.GetValue<string>(row, Attributes.AffectedComponent);
				item.Url = reader.GetValue<string>(row, Attributes.Url);
				item.DoubleValue1 = reader.GetValue<double?>(row, Attributes.DoubleValue1);
				item.DoubleValue2 = reader.GetValue<double?>(row, Attributes.DoubleValue2);
				item.TextValue = reader.GetValue<string>(row, Attributes.TextValue);
				item.IssueAssignment = reader.GetValue<string>(row, Attributes.IssueAssignment);
				item.QualityConditionUuid =
					reader.GetValue<string>(row, Attributes.QualityConditionUuid);
				item.QualityConditionVersionUuid =
					reader.GetValue<string>(row, Attributes.QualityConditionVersionUuid);
				item.ExceptionStatus = reader.GetValue<string>(row, Attributes.ExceptionStatus);
				item.ExceptionNotes = reader.GetValue<string>(row, Attributes.ExceptionNotes);
				item.ExceptionCategory = reader.GetValue<string>(row, Attributes.ExceptionCategory);
				item.ExceptionOrigin = reader.GetValue<string>(row, Attributes.ExceptionOrigin);
				item.ExceptionDefinedDate =
					reader.GetValue<string>(row, Attributes.ExceptionDefinedDate);
				item.ExceptionLastRevisionDate =
					reader.GetValue<string>(row, Attributes.ExceptionLastRevisionDate);
				item.ExceptionRetirementDate =
					reader.GetValue<string>(row, Attributes.ExceptionRetirementDate);
				item.ExceptionShapeMatchCriterion =
					reader.GetValue<string>(row, Attributes.ExceptionShapeMatchCriterion);
				item.Status = ((DatabaseSourceClass) source).GetStatus(row);
			}

			// todo daro: use source class to determine whether involved tables have geoemtry?
			item.InIssueInvolvedTables =
				IssueUtils.ParseInvolvedTables(item.InvolvedObjects, source.HasGeometry);

			return RefreshState(item);
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
			// todo daro: use AttributeReader?
			// todo daro: really needed here? Only geometry is updated but
			//			  the work item's state remains the same.
			item.Status = ((DatabaseSourceClass) sourceClass).GetStatus(row);
		}

		protected override async Task SetStatusCoreAsync(IWorkItem item, ISourceClass source)
		{
			Table table = OpenFeatureClass(source);
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
	}
}
