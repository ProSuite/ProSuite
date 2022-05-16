using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Tests;
using ProSuite.QA.Tests.Constraints;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaRelConstraint : QaRelationTestFactory
	{
		private const string _parameterNameApplyFilterInDatabase =
			"ApplyFilterExpressionsInDatabase";

		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaConstraint.Codes;

		public override string GetTestTypeDescription()
		{
			return typeof(QaRelationConstraint).Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			// relationTables is partly redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list = new List<TestParameter>
			           {
				           new TestParameter("relationTables", typeof(IList<IReadOnlyTable>),
				                             DocStrings.QaRelConstraint_relationTables),
				           new TestParameter("relation", typeof(string),
				                             DocStrings.QaRelConstraint_relation),
				           new TestParameter("join", typeof(JoinType),
				                             DocStrings.QaRelConstraint_join),
				           new TestParameter("constraint", typeof(IList<string>),
				                             DocStrings.QaRelConstraint_constraint),
				           new TestParameter(
					           _parameterNameApplyFilterInDatabase, typeof(bool),
					           DocStrings.QaRelConstraint_ApplyFilterExpressionsInDatabase,
					           isConstructorParameter: false) {DefaultValue = false}
			           };

			return list.AsReadOnly();
		}

		public override string TestDescription => DocStrings.QaRelConstraint;

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

			bool applyFilterInDatabase = GetParameterValue<bool>(testParameters,
			                                                     datasetContext,
			                                                     _parameterNameApplyFilterInDatabase);

			bool useCaseSensitiveQaSql;
			string tableConstraint = CombineTableParameters(tableParameters,
			                                                out useCaseSensitiveQaSql);

			string whereClause = applyFilterInDatabase ? tableConstraint : null;

			IReadOnlyTable queryTable = CreateQueryTable(datasetContext, associationName, tables, join,
			                                     whereClause, out string relationshipClassName);

			IList<string> translatedConstraints = TranslateConstraints(
				constraints,
				associationName, relationshipClassName,
				Assert.NotNull(Condition).ParameterValues.OfType<DatasetTestParameterValue>(),
				datasetContext);

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
			[NotNull] IEnumerable<string> constraints,
			[NotNull] string associationName,
			[NotNull] string relationshipClassName,
			[NotNull] IEnumerable<DatasetTestParameterValue> datasetParameterValues,
			[NotNull] IOpenDataset datasetContext)
		{
			IDictionary<string, string> replacements = GetTableNameReplacements(
				datasetParameterValues, datasetContext, associationName, relationshipClassName);

			if (replacements.Count == 0)
			{
				return constraints.ToList();
			}

			return constraints.Select(sql => ExpressionUtils.ReplaceTableNames(sql,
			                                                                   replacements))
			                  .ToList();
		}

		[NotNull]
		private static IDictionary<string, string> GetTableNameReplacements(
			[NotNull] IEnumerable<DatasetTestParameterValue> datasetParameterValues,
			[NotNull] IOpenDataset datasetContext,
			[NotNull] string associationName,
			[NotNull] string relationshipClassName)
		{
			var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			if (! string.Equals(associationName, relationshipClassName,
			                    StringComparison.OrdinalIgnoreCase))
			{
				replacements.Add(associationName, relationshipClassName);
			}

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

				if (! string.Equals(dataset.Name, tableName))
				{
					replacements.Add(dataset.Name, tableName);
				}
			}

			return replacements;
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
			if (string.Equals(testParameter.Name,
			                  _parameterNameApplyFilterInDatabase,
			                  StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			base.SetPropertyValue(test, testParameter, value);
		}
	}
}
