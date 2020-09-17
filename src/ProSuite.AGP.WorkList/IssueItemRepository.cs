using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList
{
	public class IssueItemRepository : GdbItemRepository
	{
		static readonly string _statusFieldName = "DblValue1";

		public IssueItemRepository(Dictionary<Geodatabase, List<Table>> tablesByGeodatabase,
		                           IRepository stateRepository) : base(tablesByGeodatabase, stateRepository) { }

		protected override DatabaseStatusSchema CreateStatusSchemaCore(FeatureClassDefinition definition)
		{
			int fieldIndex = definition.FindField(_statusFieldName);
			return new DatabaseStatusSchema(_statusFieldName, fieldIndex, 100, 200);
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

			return RefreshState(new IssueItem(id, row, source.AttributeReader));
		}

		protected override ISourceClass CreateSourceClassCore(GdbTableIdentity identity,
		                                                      IAttributeReader attributeReader,
		                                                      DatabaseStatusSchema statusSchema = null)
		{
			return new DatabaseSourceClass(identity, statusSchema, attributeReader);
		}

		//protected override void UpdateCore(ISourceClass source, IWorkItem item)
		//{
		//	using (Table table = OpenFeatureClass(source))
		//	{
		//		long oid = item.Proxy.ObjectId;

		//		var editOperation = new EditOperation();
		//		editOperation.Name = GetOperationDescription(item.Status);
				
		//		editOperation.Modify(table, oid, source.StatusFieldName, source.GetValue(item.Status));
		//		editOperation.Execute();
		//	}
		//}

		private static string GetOperationDescription(WorkItemStatus status)
		{
			string operationDescription;
			switch (status)
			{
				case WorkItemStatus.Todo:
					operationDescription = "Set status of work items to 'Todo'";
					break;

				case WorkItemStatus.Done:
					operationDescription = "Set status of work items to 'Done'";
					break;

				default:
					throw new ArgumentException($"Invalid status for operation: {status}");
			}

			return operationDescription;
		}
	}
}
