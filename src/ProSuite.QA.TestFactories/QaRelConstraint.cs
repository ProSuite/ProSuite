using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests;
using ProSuite.QA.Tests.Constraints;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaRelConstraint : QaRelationTestFactory
	{

		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaConstraint.Codes;

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters, out tableParameters);

			if (objParams.Length != 4)
			{
				throw new ArgumentException(string.Format("expected 4 parameters, got {0}",
				                                          objParams.Length));
			}

			if (! (objParams[0] is IList<IReadOnlyTable>))
			{
				throw new ArgumentException(string.Format("expected IList<ITable>, got {0}",
				                                          objParams[0].GetType()));
			}

			if (! (objParams[1] is string))
			{
				throw new ArgumentException(
					string.Format("expected string (for relation), got {0}",
					              objParams[1].GetType()));
			}

			if (objParams[2].GetType() != typeof(JoinType))
			{
				throw new ArgumentException(string.Format("expected JoinType, got {0}",
				                                          objParams[2].GetType()));
			}

			if (! (objParams[3] is IList<string>))
			{
				throw new ArgumentException(string.Format("expected IList<string>, got {0}",
				                                          objParams[3].GetType()));
			}

			var objects = new object[3];

			var tables = (IList<IReadOnlyTable>) objParams[0];
			var associationName = (string) objParams[1];
			var join = (JoinType) objParams[2];
			var constraints = (IList<string>) objParams[3];

			var factoryDef = (QaRelConstraintDefinition)FactoryDefinition;

			bool applyFilterInDatabase = GetParameterValue<bool>(testParameters,
			                                                     datasetContext,
			                                                     factoryDef.ParameterNameApplyFilterInDatabase);

			IDictionary<string, string> replacements = GetTableNameReplacements(
				Assert.NotNull(Condition).ParameterValues.OfType<DatasetTestParameterValue>(),
				datasetContext);

			string tableConstraint = CombineTableParameters(tableParameters, replacements,
			                                                out bool useCaseSensitiveQaSql);

			string whereClause = applyFilterInDatabase ? tableConstraint : null;

			IReadOnlyTable queryTable = CreateQueryTable(datasetContext, associationName, tables,
			                                             join,
			                                             whereClause,
			                                             out string relationshipClassName);


			if (!string.Equals(associationName, relationshipClassName,
			                   StringComparison.OrdinalIgnoreCase))
			{
				replacements.Add(associationName, relationshipClassName);
			}

			IList<string> translatedConstraints = TranslateConstraints(
				constraints, replacements);

			// assign objects[]
			IList<ConstraintNode> nodes =
				HierarchicalConstraintUtils.GetConstraintHierarchy(translatedConstraints);

			objects[0] = queryTable;
			objects[1] = nodes;
			objects[2] = tables;

			tableParameters = applyFilterInDatabase
				                  ? new List<TableConstraint>()
				                  : new List<TableConstraint>
				                    {
					                    new TableConstraint(
						                    queryTable,
						                    tableConstraint, useCaseSensitiveQaSql)
				                    };

			return objects;
		}

		private T GetParameterValue<T>(
			[NotNull] IEnumerable<TestParameter> testParameters,
			[NotNull] IOpenDataset datasetContext,
			[NotNull] string parameterName)
		{
			var parameter = testParameters.First(p => string.Equals(
				                                     p.Name, parameterName,
				                                     StringComparison.OrdinalIgnoreCase));

			object value;
			if (! TryGetArgumentValue(parameter,
			                          datasetContext,
			                          out value))
			{
				value = parameter.DefaultValue;
			}

			return (T) value;
		}

		[NotNull]
		private static IList<string> TranslateConstraints(
			[NotNull] IEnumerable<string> constraints, IDictionary<string, string> replacements)
		{
			if (replacements.Count == 0)
			{
				return constraints.ToList();
			}

			return constraints.Select(sql => ExpressionUtils.ReplaceTableNames(sql,
				                          replacements))
			                  .ToList();
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			var test = new QaRelationConstraint((IReadOnlyTable) args[0],
			                                    (IList<ConstraintNode>) args[1],
			                                    (IList<IReadOnlyTable>) args[2]);
			return test;
		}

		protected override void SetPropertyValue(object test, TestParameter testParameter,
		                                         object value)
		{
			var factoryDef = (QaRelConstraintDefinition)FactoryDefinition;
			if (string.Equals(testParameter.Name,
							   factoryDef.ParameterNameApplyFilterInDatabase,
			                  StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			base.SetPropertyValue(test, testParameter, value);
		}
	}
}
