using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList
{
	public class IssueItemRepository : GdbItemRepository
	{
		public IssueItemRepository(Dictionary<Geodatabase, List<Table>> tablesByGeodatabase) : base(tablesByGeodatabase) { }

		protected override DatabaseStatusSchema CreateStatusSchemaCore()
		{
			return new DatabaseStatusSchema("Code", 100, 200);
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

			return new IssueItem(id, row, source.AttributeReader);
		}

		protected override ISourceClass CreateSourceClassCore(GdbTableIdentity identity,
		                                                      IAttributeReader attributeReader,
		                                                      DatabaseStatusSchema statusSchema = null)
		{
			return new DatabaseSourceClass(identity, statusSchema, attributeReader);
		}
	}
}
