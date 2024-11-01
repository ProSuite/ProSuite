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
	[ProximityTest]
	public class QaRelMustBeNearOtherDefinition : TestFactoryDefinition
	{
		public override string TestDescription => DocStrings.QaRelMustBeNearOther;

		protected override IList<TestParameter> CreateParameters()
		{
			// redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list =
				new List<TestParameter>
				{
					new TestParameter("relationTables", typeof(IList<ITableSchemaDef>),
					                  DocStrings.QaRelMustBeNearOther_relationTables),
					new TestParameter("relation", typeof(string),
					                  DocStrings.QaRelMustBeNearOther_relation),
					new TestParameter("join", typeof(JoinType),
					                  DocStrings.QaRelMustBeNearOther_join),
				};
			AddConstructorParameters(list, typeof(QaMustBeNearOtherDefinition),
			                         constructorIndex: 0,
			                         ignoreParameters: new[] { 0 });

			AddOptionalTestParameters(
				list, typeof(QaMustBeNearOtherDefinition));

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaRelMustBeNearOtherDefinition)));
		}
	}
}
