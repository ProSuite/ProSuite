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
	public class QaDatasetConstraintFactoryDefinition : TestFactoryDefinition
	{
		public static readonly string TableAttribute = "table";
		public static readonly string ConstraintAttribute = "constraint";

		public override string TestDescription => DocStrings.QaDatasetConstraintFactory;

		protected override IList<TestParameter> CreateParameters()
		{
			var list =
				new List<TestParameter>
				{
					new TestParameter(TableAttribute, typeof(ITableSchemaDef),
					                  DocStrings.QaDatasetConstraintFactory_table),
					new TestParameter(ConstraintAttribute, typeof(IList<string>),
					                  DocStrings.QaDatasetConstraintFactory_constraint)
				};

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaDatasetConstraintFactoryDefinition)));
		}
	}
}
