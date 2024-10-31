using System;
using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.QA.TestFactories
{
	public abstract class QaRelationTestFactory : QaFactoryBase
	{
		[NotNull]
		protected static string CombineTableParameters(
			[NotNull] IEnumerable<TableConstraint> tableConstraints,
			[NotNull] IDictionary<string, string> tableNameReplacements,
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

			string untranslatedExpression;
			if (nonEmptyExpressions.Count == 1)
			{
				untranslatedExpression = nonEmptyExpressions[0];
			}
			else
			{
				// enclose expressions in parentheses to maintain expected precedence
				// (e.g. when OR is used in individual expressions)
				untranslatedExpression = StringUtils.Concatenate(nonEmptyExpressions,
				                               expression => $"({expression})",
				                               " AND ");
			}

			string translatedExpression =
				tableNameReplacements.Count == 0
					? untranslatedExpression
					: ExpressionUtils.ReplaceTableNames(untranslatedExpression,
					                                    tableNameReplacements);
			return translatedExpression;
		}

		[NotNull]
		protected static IDictionary<string, string> GetTableNameReplacements(
			[NotNull] IEnumerable<DatasetTestParameterValue> datasetParameterValues,
			[NotNull] IOpenDataset datasetContext)
		{
			var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach (DatasetTestParameterValue datasetParameterValue in datasetParameterValues)
			{
				Dataset dataset = datasetParameterValue.DatasetValue;
				if (dataset == null)
				{
					continue;
				}

				IReadOnlyTable table =
					datasetContext.OpenDataset(
						dataset, Assert.NotNull(datasetParameterValue.DataType)) as IReadOnlyTable;

				if (table == null)
				{
					continue;
				}

				string tableName = table.Name;

				if (!string.Equals(dataset.Name, tableName))
				{
					replacements.Add(dataset.Name, tableName);
				}
			}

			return replacements;
		}


		protected IReadOnlyTable CreateQueryTable([NotNull] IOpenDataset datasetOpener,
		                                          [NotNull] string associationName,
		                                          [NotNull] IList<IReadOnlyTable> tables,
		                                          JoinType joinType)
		{
			return CreateQueryTable(datasetOpener, associationName, tables, joinType, null, out _);
		}

		protected IReadOnlyTable CreateQueryTable([NotNull] IOpenDataset datasetOpener,
		                                          [NotNull] string associationName,
		                                          [NotNull] IList<IReadOnlyTable> tables,
		                                          JoinType joinType,
		                                          [CanBeNull] string whereClause,
		                                          out string relationshipClassName)
		{
			if (! (datasetOpener is IOpenAssociation associationOpener))
			{
				throw new NotSupportedException(
					"Query tables are not supported by the current context.");
			}

			IReadOnlyTable result = null;

			Model model =
				GetUniqueModel(Assert.NotNull(Condition, "No quality condition assigned"));

			Association association = DdxModelElementUtils.GetAssociationFromStoredName(
				associationName, model, ignoreUnknownAssociation: true);

			if (association == null)
			{
				if (! model.UseDefaultDatabaseOnlyForSchema)
				{
					result = associationOpener.OpenQueryTable(
						associationName, model, tables, joinType, whereClause);
				}
				else
				{
					Assert.NotNull(association, "Association not found in current context: {0}",
					               associationName);
				}
			}
			else
			{
				result = associationOpener.OpenQueryTable(
					association, tables, joinType, whereClause);
			}

			// Used in case it's the m:n bridge table name
			relationshipClassName =
				associationOpener.GetRelationshipClassName(associationName, model);

			return result;
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
