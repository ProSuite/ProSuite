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
	[LinearNetworkTest]
	public class QaRelLineGroupConstraints : QaRelationTestFactory
	{
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaLineGroupConstraints.Codes;

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			object[] objParams =
				base.Args(datasetContext, testParameters, out tableParameters);
			if (objParams.Length != 7)
			{
				throw new ArgumentException(string.Format("expected 7 parameter, got {0}",
				                                          objParams.Length));
			}

			var objects = new object[6];

			if (! (objParams[0] is IList<ITableSchemaDef>))
			{
				throw new ArgumentException(string.Format(
					                            "expected IList<ITableSchemaDef>, got {0}",
					                            objParams[0].GetType()));
			}

			var tables = ToReadOnlyTableList<IReadOnlyTable>(objParams[0]);

			var associationName =
				ValidateType<string>(objParams[1], "string (for relation)");
			var join = ValidateType<JoinType>(objParams[2]);

			objects[1] = ValidateType<double>(objParams[3]); // minEndDistance
			objects[2] = ValidateType<double>(objParams[4]); // minGroupLength
			objects[3] = ValidateType<double>(objParams[5]); // minLeafLength
			objects[4] = ValidateType<string>(objParams[6]); // groupBy
			objects[5] = tables;

			IReadOnlyTable queryTable =
				CreateQueryTable(datasetContext, associationName, tables, join);

			if (queryTable is IReadOnlyFeatureClass == false)
			{
				throw new InvalidOperationException(
					"Relation table is not a FeatureClass, try change join type");
			}

			objects[0] = queryTable;

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

		protected override ITest CreateTestInstance(object[] args)
		{
			var test = new QaLineGroupConstraints(
				new[] { (IReadOnlyFeatureClass) args[0] }, (double) args[1], (double) args[2],
				(double) args[3], new[] { (string) args[4] });

			return test;
		}

		protected override void SetPropertyValue(object test, TestParameter testParameter,
		                                         object value)
		{
			var factoryDef = (QaRelLineGroupConstraintsDefinition) FactoryDefinition;
			if (testParameter.Name == factoryDef.GroupConditionName)
			{
				((QaLineGroupConstraints) test).GroupConditions = new[] { (string) value };
			}
			else
			{
				base.SetPropertyValue(test, testParameter, value);
			}
		}
	}
}
