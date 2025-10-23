using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public static class QueryTableUtils
	{
		public static IReadOnlyTable OpenQueryTable([NotNull] string associationName,
		                                            [NotNull] DdxModel model,
		                                            [NotNull] IList<IReadOnlyTable> tables,
		                                            [NotNull] IQueryTableContext queryTableContext,
		                                            JoinType joinType,
		                                            [CanBeNull] string whereClause = null)
		{
			if (! queryTableContext.CanOpenQueryTables())
			{
				throw new InvalidOperationException("Query context cannot open query tables.");
			}

			string relationshipClassName =
				queryTableContext.GetRelationshipClassName(associationName, model);

			IReadOnlyTable queryTable = queryTableContext.OpenQueryTable(
				relationshipClassName, model,
				tables, joinType, whereClause);

			return queryTable;
		}

		public static IReadOnlyTable OpenAoQueryTable([NotNull] string associationName,
		                                              [NotNull] DdxModel model,
		                                              [NotNull] IList<IReadOnlyTable> tables,
		                                              [NotNull] IDatasetContext datasetContext,
		                                              JoinType joinType,
		                                              [CanBeNull] string whereClause = null)
		{
			// Original implementation, we know there are Geodatabase tables behind the IReadOnlyTable:
			List<ITable> baseTables = GetBaseTables(tables);

			IRelationshipClass relationshipClass =
				OpenRelationshipClass(associationName, model, datasetContext);

			return RelationshipClassUtils.GetReadOnlyQueryTable(
				relationshipClass, baseTables, joinType, whereClause);
		}

		[NotNull]
		public static IRelationshipClass OpenRelationshipClass(
			[NotNull] string associationName,
			[NotNull] DdxModel model,
			[NotNull] IDatasetContext datasetContext)
		{
			// TODO REFACTORMODEL: what if the association is not in the primary dataset context (e.g. work unit), but from another model?

			Association association = DdxModelElementUtils.GetAssociationFromStoredName(
				associationName, model, ignoreUnknownAssociation: true);

			if (association == null && ! model.UseDefaultDatabaseOnlyForSchema)
			{
				IWorkspace masterWorkspace = model.GetMasterDatabaseWorkspace();
				if (masterWorkspace != null)
				{
					return OpenRelationshipClassFromMasterWorkspace(
						masterWorkspace, associationName,
						model);
				}
			}

			Assert.NotNull(association, "Association not found in current context: {0}",
			               associationName);

			IRelationshipClass result = datasetContext.OpenRelationshipClass(association);
			Assert.NotNull(result,
			               "Unable to open relationship class for association {0} in current context",
			               association.Name);

			return result;
		}

		[NotNull]
		public static string GetRelationshipClassName([NotNull] IWorkspace masterWorkspace,
		                                              [NotNull] string associationName,
		                                              [NotNull] DdxModel model)
		{
			if (masterWorkspace.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				// the workspace uses unqualified names

				return ModelElementNameUtils.IsQualifiedName(associationName)
					       ? ModelElementNameUtils.GetUnqualifiedName(associationName)
					       : associationName;
			}

			// the workspace uses qualified names

			if (! ModelElementNameUtils.IsQualifiedName(associationName))
			{
				Assert.NotNullOrEmpty(
					model.DefaultDatabaseSchemaOwner,
					"The master database schema owner is not defined, cannot qualify unqualified association name ({0})",
					associationName);

				return ModelElementNameUtils.GetQualifiedName(
					model.DefaultDatabaseName,
					model.DefaultDatabaseSchemaOwner,
					ModelElementNameUtils.GetUnqualifiedName(associationName));
			}

			// the association name is already qualified

			if (StringUtils.IsNotEmpty(model.DefaultDatabaseSchemaOwner))
			{
				return ModelElementNameUtils.GetQualifiedName(
					model.DefaultDatabaseName,
					model.DefaultDatabaseSchemaOwner,
					ModelElementNameUtils.GetUnqualifiedName(associationName));
			}

			return associationName;
		}

		[NotNull]
		private static IRelationshipClass OpenRelationshipClassFromMasterWorkspace(
			[NotNull] IWorkspace masterWorkspace,
			[NotNull] string associationName,
			[NotNull] DdxModel model)
		{
			string relClassName = GetRelationshipClassName(masterWorkspace, associationName,
			                                               model);

			return DatasetUtils.OpenRelationshipClass(
				(IFeatureWorkspace) masterWorkspace,
				relClassName);
		}

		private static List<ITable> GetBaseTables([NotNull] IEnumerable<IReadOnlyTable> tables)
		{
			List<ITable> baseTables = new List<ITable>();
			foreach (IReadOnlyTable readOnlyTable in tables)
			{
				if (readOnlyTable is ReadOnlyTable roTable)
				{
					baseTables.Add(roTable.BaseTable);
				}
				else
				{
					Assert.Fail($"{readOnlyTable.Name} is not {nameof(ReadOnlyTable)} cannot " +
					            $"be used in Relation");
				}
			}

			return baseTables;
		}
	}
}
