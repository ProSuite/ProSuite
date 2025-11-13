using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaRelUniqueDefinition : TestFactoryDefinition
	{
		public override string TestDescription => DocStrings.QaRelUnique;

		protected override IList<TestParameter> CreateParameters()
		{
			// relationTables is redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list =
				new List<TestParameter>
				{
					new TestParameter("relationTables", typeof(IList<ITableSchemaDef>),
					                  DocStrings.QaRelUnique_relationTables),
					new TestParameter("relation", typeof(string),
					                  DocStrings.QaRelUnique_relation),
					new TestParameter("join", typeof(JoinType),
					                  DocStrings.QaRelUnique_join),
					new TestParameter("unique", typeof(string),
					                  DocStrings.QaRelUnique_unique),
					new TestParameter("maxRows", typeof(int),
					                  DocStrings.QaRelUnique_maxRows)
				};

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaRelUniqueDefinition)));
		}
	}
}
