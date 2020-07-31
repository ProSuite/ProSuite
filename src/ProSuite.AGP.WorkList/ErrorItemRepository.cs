using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	public class ErrorItemRepository : GdbItemRepository
	{
		public ErrorItemRepository(IWorkspaceContext workspaceContext) : base(workspaceContext) { }

		protected override DatabaseStatusSchema CreateStatusSchemaCore()
		{
			return new DatabaseStatusSchema("Code", 100, 200);
		}

		protected override IAttributeReader CreateAttributeReaderCore(
			FeatureClassDefinition definition)
		{
			return new AttributeReader(definition,
			                           Attributes.IssueCode,
			                           Attributes.IssueCodeDescription);
		}

		protected override IWorkItem CreateWorkItemCore(
			Row row, IAttributeReader reader)
		{
			return new ErrorItem(row, reader);
		}

		protected override DatabaseSourceClass CreateSourceClassCore(
			string name, DatabaseStatusSchema statusSchema, IAttributeReader attributeReader)
		{
			return new DatabaseSourceClass(name, statusSchema, attributeReader);
		}
	}
}
