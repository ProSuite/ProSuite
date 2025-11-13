using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaRelLineGroupConstraintsDefinition : TestFactoryDefinition
	{
		public string GroupConditionName { get; } = "GroupCondition";

		public override string TestDescription => DocStrings.QaRelLineGroupConstraints;

		protected override IList<TestParameter> CreateParameters()
		{
			// redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list =
				new List<TestParameter>
				{
					new TestParameter("relationTables", typeof(IList<ITableSchemaDef>),
					                  DocStrings.QaRelGroupConnected_relationTables),
					new TestParameter("relation", typeof(string),
					                  DocStrings.QaRelGroupConnected_relation),
					new TestParameter("join", typeof(JoinType),
					                  DocStrings.QaRelGroupConnected_join),
					new TestParameter("minGap", typeof(double),
					                  Tests.Documentation.DocStrings.QaLineGroupConstraints_minGap),
					new TestParameter("minGroupLength", typeof(double),
					                  Tests.Documentation.DocStrings
					                       .QaLineGroupConstraints_minGroupLength),
					new TestParameter("minDangleLength", typeof(double),
					                  Tests.Documentation.DocStrings
					                       .QaLineGroupConstraints_minDangleLength),
					new TestParameter("groupBy", typeof(string),
					                  Tests.Documentation.DocStrings
					                       .QaLineGroupConstraints_groupBy),
					new TestParameter(GroupConditionName, typeof(string),
					                  DocStrings.QaRelLineGroupConstraints_GroupCondition,
					                  isConstructorParameter: false)
				};

			AddOptionalTestParameters(
				list, typeof(QaLineGroupConstraintsDefinition),
				ignoredTestParameters: new[]
				                       {
					                       nameof(QaLineGroupConstraintsDefinition.GroupConditions)
				                       });

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaRelLineGroupConstraintsDefinition)));
		}
	}
}
