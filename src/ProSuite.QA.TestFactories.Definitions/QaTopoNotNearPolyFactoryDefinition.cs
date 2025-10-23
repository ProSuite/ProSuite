using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.TestFactories
{
	[UsedImplicitly]
	[ProximityTest]
	public class QaTopoNotNearPolyFactoryDefinition : TestFactoryDefinition
	{
		public override string TestDescription => DocStrings.QaTopoNotNearPolyFactory;

		public const string FeatureClassParamName = "featureClass";
		public const string ReferenceParamName = "reference";
		public const string ReferenceSubtypesParamName = "referenceSubtypes";
		public const string FeaturesubtypeRulesParamName = "featuresubtypeRules";

		protected override IList<TestParameter> CreateParameters()
		{
			List<TestParameter> list = CreateParameterList();

			return list.AsReadOnly();
		}

		public static List<TestParameter> CreateParameterList()
		{
			var list =
				new List<TestParameter>
				{
					new TestParameter(FeatureClassParamName, typeof(IFeatureClassSchemaDef),
									  DocStrings.QaTopoNotNearPolyFactory_featureClass),
					new TestParameter(ReferenceParamName, typeof(IFeatureClassSchemaDef),
									  DocStrings.QaTopoNotNearPolyFactory_reference),
					new TestParameter(ReferenceSubtypesParamName, typeof(int[]),
									  DocStrings.QaTopoNotNearPolyFactory_referenceSubtypes),
					new TestParameter(FeaturesubtypeRulesParamName, typeof(string[]),
									  DocStrings.QaTopoNotNearPolyFactory_featuresubtypeRules)
				};

			return list;
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaTopoNotNearPolyFactoryDefinition)));
		}
	}
}
