using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[LinearNetworkTest]
	public class QaLineConnectionDefinition : TestFactoryDefinition
	{
		public override string TestDescription => DocStrings.QaLineConnection;

		protected override IList<TestParameter> CreateParameters()
		{
			var list =
				new List<TestParameter>
				{
					new TestParameter("featureClasses", typeof(IFeatureClassSchemaDef[]),
					                  DocStrings.QaLineConnection_featureClasses),
					new TestParameter("rules", typeof(string[]),
					                  DocStrings.QaLineConnection_rules)
				};

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaLineConnectionDefinition)));
		}
	}
}
