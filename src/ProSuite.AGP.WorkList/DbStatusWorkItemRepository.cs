using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class DbStatusWorkItemRepository : GdbItemRepository
	{
		#region Overrides of GdbItemRepository

		public DbStatusWorkItemRepository(
			[NotNull] IEnumerable<DbStatusSourceClassDefinition> sourceClassDefinitions,
			[NotNull] IWorkItemStateRepository workItemStateRepository)
			: base(sourceClassDefinitions, workItemStateRepository) { }

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

		#endregion
	}
}
