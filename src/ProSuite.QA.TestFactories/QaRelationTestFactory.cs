using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.QA.TestFactories
{
	public abstract class QaRelationTestFactory : TestFactory
	{
		[NotNull]
		protected static string CombineTableParameters(
			[NotNull] IEnumerable<TableConstraint> tableConstraints,
			out bool useCaseSensitiveQaSql)
		{
			Assert.ArgumentNotNull(tableConstraints, nameof(tableConstraints));

			var nonEmptyExpressions = new List<string>();

			useCaseSensitiveQaSql = false;

			foreach (TableConstraint tableConstraint in tableConstraints)
			{
				if (tableConstraint.QaSqlIsCaseSensitive)
				{
					useCaseSensitiveQaSql = true;
				}

				if (StringUtils.IsNullOrEmptyOrBlank(tableConstraint.FilterExpression))
				{
					continue;
				}

				nonEmptyExpressions.Add(tableConstraint.FilterExpression);
			}

			if (nonEmptyExpressions.Count == 0)
			{
				return string.Empty;
			}

			if (nonEmptyExpressions.Count == 1)
			{
				return nonEmptyExpressions[0];
			}

			// enclose expressions in parentheses to maintain expected precedence
			// (e.g. when OR is used in individual expressions)
			return StringUtils.Concatenate(nonEmptyExpressions,
			                               expression => $"({expression})",
			                               " AND ");
		}

		protected ITable CreateQueryTable([NotNull] IOpenDataset datasetContext,
		                                  [NotNull] string associationName,
		                                  [NotNull] IList<ITable> tables,
		                                  JoinType joinType)
		{
			return CreateQueryTable(datasetContext, associationName, tables, joinType, null, out _);
		}

		protected ITable CreateQueryTable([NotNull] IOpenDataset datasetContext,
		                                  [NotNull] string associationName,
		                                  [NotNull] IList<ITable> tables,
		                                  JoinType joinType,
		                                  [CanBeNull] string whereClause,
		                                  out string relationshipClassName)
		{
			ITable queryTable;
			if (datasetContext is IQueryTableContext queryContext &&
			    queryContext.CanOpenQueryTables())
			{
				Model uniqueModel =
					GetUniqueModel(Assert.NotNull(Condition, "No quality condition assigned"));

				relationshipClassName =
					queryContext.GetRelationshipClassName(associationName, uniqueModel);

				queryTable = queryContext.OpenQueryTable(relationshipClassName, uniqueModel,
				                                         tables, joinType, whereClause);
			}
			else
			{
				IRelationshipClass relationshipClass = OpenRelationshipClass(associationName,
				                                                             datasetContext);
				queryTable = RelationshipClassUtils.GetQueryTable(
					relationshipClass, tables, joinType, whereClause);

				relationshipClassName = DatasetUtils.GetName(relationshipClass);
			}

			return queryTable;
		}

		[NotNull]
		protected IRelationshipClass OpenRelationshipClass(
			[NotNull] string associationName,
			[NotNull] IOpenDataset datasetContext)
		{
			Assert.NotNull(Condition, "No quality condition assigned");

			Model model = GetUniqueModel(Condition);

			// TODO REFACTORMODEL: what if the association is not in the primary dataset context (e.g. work unit), but from another model?

			Association association = ModelElementUtils.GetAssociationFromStoredName(
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
		private static IRelationshipClass OpenRelationshipClassFromMasterWorkspace(
			[NotNull] IWorkspace masterWorkspace,
			[NotNull] string associationName,
			[NotNull] Model model)
		{
			string relClassName = GetRelationshipClassName(masterWorkspace, associationName,
			                                               model);

			return DatasetUtils.OpenRelationshipClass(
				(IFeatureWorkspace) masterWorkspace,
				relClassName);
		}

		[NotNull]
		private static string GetRelationshipClassName([NotNull] IWorkspace masterWorkspace,
		                                               [NotNull] string associationName,
		                                               [NotNull] Model model)
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
		private static Model GetUniqueModel([NotNull] InstanceConfiguration qualityCondition)
		{
			Model uniqueModel = null;
			foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues())
			{
				if (uniqueModel == null)
				{
					uniqueModel = dataset.Model as Model;
				}
				else
				{
					Assert.AreEqual(
						uniqueModel, dataset.Model,
						"Datasets involved in relation tests must be from same model (quality condition: {0})",
						qualityCondition.Name);
				}
			}

			Assert.NotNull(uniqueModel,
			               "Unable to determine model from quality condition {0}",
			               qualityCondition.Name);
			return uniqueModel;
		}
	}
}
