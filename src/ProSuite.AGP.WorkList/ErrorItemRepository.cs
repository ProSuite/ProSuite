using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	public class ErrorItemRepository : GdbItemRepository
	{
		public ErrorItemRepository(IEnumerable<IWorkspaceContext> workspaces) : base(workspaces) { }

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

		protected override IWorkItem CreateWorkItemCore(Row row, ISourceClass source)
		{
			int id = CreateItemIDCore(row, source);

			return new ErrorItem(id, row, source.AttributeReader);
		}

		protected override ISourceClass CreateSourceClassCore(GdbTableReference identity,
		                                                      IAttributeReader attributeReader,
		                                                      DatabaseStatusSchema statusSchema = null)
		{
			return new DatabaseSourceClass(identity, statusSchema, attributeReader);
		}
	}
}
