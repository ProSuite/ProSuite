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
	[AttributeTest]
	public class QaRelGroupConstraintsDefinition : TestFactoryDefinition
	{
		public string ExistsRowGroupFilterName { get; } = "ExistsRowGroupFilter";

		public override string TestDescription => DocStrings.QaRelGroupConstraints;

		protected override IList<TestParameter> CreateParameters()
		{
			// redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list =
				new List<TestParameter>
				{
					new TestParameter("relationTables", typeof(IList<ITableSchemaDef>),
					                  DocStrings.QaRelConstraint_relationTables),
					new TestParameter("relation", typeof(string),
					                  DocStrings.QaRelConstraint_relation),
					new TestParameter("join", typeof(JoinType),
					                  DocStrings.QaRelConstraint_join),
					new TestParameter("groupByExpression", typeof(string),
					                  Tests.Documentation.DocStrings
					                       .QaGroupConstraints_groupByExpression),
					new TestParameter("distinctExpression", typeof(string),
					                  Tests.Documentation.DocStrings
					                       .QaGroupConstraints_distinctExpression),
					new TestParameter("maxDistinctCount", typeof(int),
					                  Tests.Documentation.DocStrings
					                       .QaGroupConstraints_maxDistinctCount),
					new TestParameter("limitToTestedRows", typeof(bool),
					                  Tests.Documentation.DocStrings
					                       .QaGroupConstraints_limitToTestedRows),
					new TestParameter(ExistsRowGroupFilterName, typeof(string),
					                  DocStrings.QaRelGroupConstraints_ExistsRowGroupFilter,
					                  isConstructorParameter: false)
				};

			AddOptionalTestParameters(
				list, typeof(QaGroupConstraintsDefinition),
				new[] { nameof(QaGroupConstraintsDefinition.ExistsRowGroupFilters) });

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaRelGroupConstraintsDefinition)));
		}
	}
}
