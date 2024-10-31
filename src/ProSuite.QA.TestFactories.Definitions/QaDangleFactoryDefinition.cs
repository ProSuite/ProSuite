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
	public class QaDangleFactoryDefinition : TestFactoryDefinition
	{
		public override string TestDescription => DocStrings.QaDangleFactory;

		protected override IList<TestParameter> CreateParameters()
		{
			var list = new List<TestParameter>
			           {
				           new TestParameter("polylineClasses", typeof(IFeatureClassSchemaDef[]),
				                             DocStrings.QaDangleFactory_polylineClasses)
			           };

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaDangleFactoryDefinition)));
		}
	}
}
