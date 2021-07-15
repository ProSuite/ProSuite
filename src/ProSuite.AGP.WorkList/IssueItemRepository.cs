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
		                           IRepository stateRepository) : base(
			tablesByGeodatabase, stateRepository) { }

		protected override WorkListStatusSchema CreateStatusSchemaCore(
			FeatureClassDefinition definition)
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
			FeatureClassDefinition definition)
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

			IAttributeReader reader = ((DatabaseSourceClass) source).AttributeReader;

			var item = new IssueItem(id, row)
			           {
				           Status = ((DatabaseSourceClass) source).GetStatus(row),

				           IssueCode = reader.GetValue<string>(row, Attributes.IssueCode),
				           IssueCodeDescription =
					           reader.GetValue<string>(row, Attributes.IssueCodeDescription),
				           InvolvedObjects =
					           reader.GetValue<string>(row, Attributes.InvolvedObjects),
				           QualityCondition =
					           reader.GetValue<string>(row, Attributes.QualityConditionName),
				           TestName = reader.GetValue<string>(row, Attributes.TestName),
				           TestDescription =
					           reader.GetValue<string>(row, Attributes.TestDescription),
				           TestType = reader.GetValue<string>(row, Attributes.TestType),
				           IssueSeverity = reader.GetValue<string>(row, Attributes.IssueSeverity),
				           StopCondition = reader.GetValue<string>(row, Attributes.IsStopCondition),
				           Category = reader.GetValue<string>(row, Attributes.Category),
				           AffectedComponent =
					           reader.GetValue<string>(row, Attributes.AffectedComponent),
				           Url = reader.GetValue<string>(row, Attributes.Url),
				           DoubleValue1 = reader.GetValue<double?>(row, Attributes.DoubleValue1),
				           DoubleValue2 = reader.GetValue<double?>(row, Attributes.DoubleValue2),
				           TextValue = reader.GetValue<string>(row, Attributes.TextValue),
				           IssueAssignment =
					           reader.GetValue<string>(row, Attributes.IssueAssignment),
				           QualityConditionUuid =
					           reader.GetValue<string>(row, Attributes.QualityConditionUuid),
				           QualityConditionVersionUuid =
					           reader.GetValue<string>(row, Attributes.QualityConditionVersionUuid),
				           ExceptionStatus =
					           reader.GetValue<string>(row, Attributes.ExceptionStatus),
				           ExceptionNotes = reader.GetValue<string>(row, Attributes.ExceptionNotes),
				           ExceptionCategory =
					           reader.GetValue<string>(row, Attributes.ExceptionCategory),
				           ExceptionOrigin =
					           reader.GetValue<string>(row, Attributes.ExceptionOrigin),
				           ExceptionDefinedDate =
					           reader.GetValue<string>(row, Attributes.ExceptionDefinedDate),
				           ExceptionLastRevisionDate =
					           reader.GetValue<string>(row, Attributes.ExceptionLastRevisionDate),
				           ExceptionRetirementDate =
					           reader.GetValue<string>(row, Attributes.ExceptionRetirementDate),
				           ExceptionShapeMatchCriterion =
					           reader.GetValue<string>(row, Attributes.ExceptionShapeMatchCriterion)
			           };

			item.InIssueInvolvedTables = IssueUtils.ParseInvolvedTables(item.InvolvedObjects);

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
			//			  the work itmes's state remains the same.
			item.Status = ((DatabaseSourceClass) sourceClass).GetStatus(row);
		}

		protected override async Task SetStatusCoreAsync(IWorkItem item, ISourceClass source)
		{
			Table table = OpenFeatureClass(source);

			try
			{
				var databaseSourceClass = (DatabaseSourceClass) source;

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
					operationDescription = "Set status of work item to 'Not Corrected'";
					break;

				case WorkItemStatus.Done:
					operationDescription = "Set status of work item to 'Corrected'";
					break;

				default:
					throw new ArgumentException($"Invalid status for operation: {status}");
			}

			return operationDescription;
		}
	}
}
