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
	public class QaRelGroupConstraints : QaRelationTestFactory
	{
		private const string _existsRowGroupFilterName = "ExistsRowGroupFilter";

		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaGroupConstraints.Codes;

		public override string GetTestTypeDescription()
		{
			return typeof(QaRelGroupConstraints).Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			// redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list =
				new List<TestParameter>
				{
					new TestParameter("relationTables", typeof(IList<IReadOnlyTable>)),
					new TestParameter("relation", typeof(string)),
					new TestParameter("join", typeof(JoinType)),
					new TestParameter("groupByExpression", typeof(string)),
					new TestParameter("distinctExpression", typeof(string)),
					new TestParameter("maxDistinctCount", typeof(int)),
					new TestParameter("limitToTestedRows", typeof(bool)),
					new TestParameter(_existsRowGroupFilterName, typeof(string),
					                  isConstructorParameter: false)
				};

			AddOptionalTestParameters(
				list, typeof(QaGroupConstraints),
				new[] { nameof(QaGroupConstraints.ExistsRowGroupFilters) });

			return list.AsReadOnly();
		}

		public override string TestDescription => DocStrings.QaRelGroupConstraints;

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

			var tables = ValidateType<IList<IReadOnlyTable>>(objParams[0]);
			var associationName = ValidateType<string>(objParams[1]);
			var join = ValidateType<JoinType>(objParams[2]);

			IReadOnlyTable queryTable =
				CreateQueryTable(datasetContext, associationName, tables, join);

			if (queryTable is IReadOnlyFeatureClass == false)
			{
				throw new InvalidOperationException(
					"Relation table is not a FeatureClass, try change join type");
			}

			objects[0] = queryTable;
			objects[1] = ValidateType<string>(objParams[3]);
			objects[2] = ValidateType<string>(objParams[4]);
			objects[3] = ValidateType<int>(objParams[5]);
			objects[4] = ValidateType<bool>(objParams[6]);
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

		protected override ITest CreateTestInstance(object[] args)
		{
			var test = new QaGroupConstraints((IReadOnlyTable) args[0],
			                                  (string) args[1],
			                                  (string) args[2],
			                                  (int) args[3],
			                                  (bool) args[4]);

			test.SetRelatedTables((IList<IReadOnlyTable>) args[5]);
			return test;
		}

		protected override void SetPropertyValue(object test, TestParameter testParameter,
		                                         object value)
		{
			if (testParameter.Name == _existsRowGroupFilterName)
			{
				((QaGroupConstraints) test).ExistsRowGroupFilters = new[] { (string) value };
			}
			else
			{
				base.SetPropertyValue(test, testParameter, value);
			}
		}
	}
}
