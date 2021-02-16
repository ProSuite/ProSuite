using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Tests;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Core;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaRelLineGroupConstraints : QaRelationTestFactory
	{
		private const string _groupConditionName = "GroupCondition";

		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes => QaLineGroupConstraints.Codes;

		public override string GetTestTypeDescription()
		{
			return typeof(QaRelLineGroupConstraints).Name;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			// redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list =
				new List<TestParameter>
				{
					new TestParameter("relationTables", typeof(IList<ITable>),
					                  DocStrings.QaRelGroupConnected_relationTables),
					new TestParameter("relation", typeof(string),
					                  DocStrings.QaRelGroupConnected_relation),
					new TestParameter("join", typeof(JoinType),
					                  DocStrings.QaRelGroupConnected_join),
					new TestParameter(
						"minGap", typeof(double),
						Tests.Documentation.DocStrings.QaLineGroupConstraints_minGap),
					new TestParameter(
						"minGroupLength", typeof(double),
						Tests.Documentation.DocStrings
						     .QaLineGroupConstraints_minGroupLength),
					new TestParameter(
						"minDangleLength", typeof(double),
						Tests.Documentation.DocStrings
						     .QaLineGroupConstraints_minDangleLength),
					new TestParameter(
						"groupBy", typeof(string),
						Tests.Documentation.DocStrings.QaLineGroupConstraints_groupBy),
					new TestParameter(
						_groupConditionName, typeof(string),
						DocStrings.QaRelLineGroupConstraints_GroupCondition,
						isConstructorParameter: false)
				};

			AddOptionalTestParameters(
				list, typeof(QaLineGroupConstraints),
				ignoredTestParameters: new[]
				                       {
					                       nameof(QaLineGroupConstraints
						                              .GroupConditions)
				                       });

			return list.AsReadOnly();
		}

		public override string GetTestDescription()
		{
			return DocStrings.QaRelLineGroupConstraints;
		}

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

			var tables = ValidateType<IList<ITable>>(objParams[0], "IList<ITable>");
			var associationName =
				ValidateType<string>(objParams[1], "string (for relation)");
			var join = ValidateType<JoinType>(objParams[2]);

			objects[1] = ValidateType<double>(objParams[3]); // minEndDistance
			objects[2] = ValidateType<double>(objParams[4]); // minGroupLength
			objects[3] = ValidateType<double>(objParams[5]); // minLeafLength
			objects[4] = ValidateType<string>(objParams[6]); // groupBy
			objects[5] = tables;

			ITable queryTable = CreateQueryTable(datasetContext, associationName, tables, join);

			if (queryTable is IFeatureClass == false)
			{
				throw new InvalidOperationException(
					"Relation table is not a FeatureClass, try change join type");
			}

			objects[0] = queryTable;

			bool useCaseSensitiveQaSql;
			string tableConstraint = CombineTableParameters(tableParameters,
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
				new[] {(IFeatureClass) args[0]}, (double) args[1], (double) args[2],
				(double) args[3], new[] {(string) args[4]});

			test.AddRelatedTables((ITable) args[0], (IList<ITable>) args[5]);
			return test;
		}

		protected override void SetPropertyValue(ITest test, TestParameter testParameter,
		                                         object value)
		{
			if (testParameter.Name == _groupConditionName)
			{
				((QaLineGroupConstraints) test).GroupConditions = new[] {(string) value};
			}
			else
			{
				base.SetPropertyValue(test, testParameter, value);
			}
		}
	}
}
