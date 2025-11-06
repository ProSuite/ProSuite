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
					new TestParameter("relationTables", typeof(IList<ITableSchemaDef>)),
					new TestParameter("relation", typeof(string)),
					new TestParameter("join", typeof(JoinType)),
					new TestParameter("groupByExpression", typeof(string)),
					new TestParameter("distinctExpression", typeof(string)),
					new TestParameter("maxDistinctCount", typeof(int)),
					new TestParameter("limitToTestedRows", typeof(bool)),
					new TestParameter(ExistsRowGroupFilterName, typeof(string),
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
