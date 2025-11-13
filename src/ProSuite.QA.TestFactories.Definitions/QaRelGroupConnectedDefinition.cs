using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaRelGroupConnectedDefinition : TestFactoryDefinition
	{
		public override string TestDescription => DocStrings.QaRelGroupConnected;

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
					new TestParameter("groupBy", typeof(IList<string>),
					                  DocStrings.QaRelGroupConnected_groupBy),
					new TestParameter("allowedShape",
					                  typeof(ShapeAllowed),
					                  DocStrings.QaRelGroupConnected_allowedShape)
				};

			// TODO: Test thoroughly
			AddOptionalTestParameters(
				list, typeof(QaGroupConnectedDefinition),
				additionalProperties: new[]
				                      {
					                      nameof(QaGroupConnectedDefinition.ErrorReporting)
				                      });

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaRelGroupConnectedDefinition)));
		}
	}
}
