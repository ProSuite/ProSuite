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

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaRelRegularExpression : QaRelationTestFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaRegularExpression.Codes;

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters, out tableParameters);
			if (objParams.Length != 5)
			{
				throw new ArgumentException(string.Format("expected 5 parameter, got {0}",
				                                          objParams.Length));
			}

			if (! (objParams[0] is IList<ITableSchemaDef>))
			{
				throw new ArgumentException(string.Format(
					                            "expected IList<ITableSchemaDef>, got {0}",
					                            objParams[0].GetType()));
			}

			if (objParams[1] is string == false)
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

			if (objParams[3] is string == false)
			{
				throw new ArgumentException(string.Format("expected string, got {0}",
				                                          objParams[3].GetType()));
			}

			if (objParams[4] as IList<string> == null)
			{
				throw new ArgumentException(string.Format("expected IList<string>, got {0}",
				                                          objParams[4].GetType()));
			}

			var tables = ToReadOnlyTableList<IReadOnlyTable>(objParams[0]);
			var associationName = (string) objParams[1];
			var join = (JoinType) objParams[2];
			var pattern = (string) objParams[3];
			var fields = (IList<string>) objParams[4];

			var matchIsError = false;
			string patternDescription = null;

			foreach (TestParameter parameter in testParameters)
			{
				if (parameter.IsConstructorParameter)
				{
					continue;
				}

				object value;
				if (! TryGetArgumentValue(parameter, datasetContext, out value))
				{
					continue;
				}

				var factoryDef = (QaRelRegularExpressionDefinition) FactoryDefinition;
				if (parameter.Name.Equals(factoryDef.MatchIsErrorName,
				                          StringComparison.CurrentCultureIgnoreCase))
				{
					matchIsError = (bool) value;
				}

				if (parameter.Name.Equals(factoryDef.PatternDescriptionName,
				                          StringComparison.CurrentCultureIgnoreCase))
				{
					patternDescription = (string) value;
				}
			}

			IReadOnlyTable queryTable =
				CreateQueryTable(datasetContext, associationName, tables, join);

			var objects = new object[6];

			objects[0] = queryTable;
			objects[1] = pattern;
			objects[2] = fields;
			objects[3] = matchIsError;
			objects[4] = patternDescription;
			objects[5] = tables;

			IDictionary<string, string> replacements = GetTableNameReplacements(
				Assert.NotNull(Condition).ParameterValues.OfType<DatasetTestParameterValue>(),
				datasetContext);

			bool useCaseSensitiveQaSql;
			string tableConstraint = CombineTableParameters(tableParameters, replacements,
			                                                out useCaseSensitiveQaSql);

			tableParameters = new List<TableConstraint>
			                  {
				                  new TableConstraint(queryTable, tableConstraint,
				                                      useCaseSensitiveQaSql)
			                  };

			return objects;
		}

		protected override void SetPropertyValue(object test, TestParameter parameter,
		                                         object value)
		{
			var factoryDef = (QaRelRegularExpressionDefinition) FactoryDefinition;
			if (parameter.Name.Equals(factoryDef.MatchIsErrorName,
			                          StringComparison.CurrentCultureIgnoreCase) ||
			    parameter.Name.Equals(factoryDef.PatternDescriptionName,
			                          StringComparison.CurrentCultureIgnoreCase))
			{
				return;
			}

			base.SetPropertyValue(test, parameter, value);
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			var test = new QaRegularExpression((IReadOnlyTable) args[0],
			                                   (string) args[1],
			                                   (IEnumerable<string>) args[2],
			                                   (bool) args[3],
			                                   (string) args[4]);
			return test;
		}
	}
}
