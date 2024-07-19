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
	public class QaGdbConstraintFactoryDefinition : TestFactoryDefinition
	{
		public string FieldsParameterName { get; } = "Fields";

		public string AllowNullValuesForCodedValueDomains { get; } =
			"AllowNullValuesForCodedValueDomains";

		public string AllowNullValuesForRangeDomains { get; } =
			"AllowNullValuesForRangeDomains";

		public override string TestDescription => DocStrings.QaGdbConstraintFactory;

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaConstraintDefinition)));
		}

		protected override IList<TestParameter> CreateParameters()
		{
			return new List<TestParameter>
			       {
				       new TestParameter("table", typeof(ITableSchemaDef),
				                         DocStrings.QaGdbConstraintFactory_table),
				       new TestParameter(AllowNullValuesForCodedValueDomains,
				                         typeof(bool),
				                         description: DocStrings
					                         .QaGdbConstraintFactory_AllowNullValuesForCodedValueDomains,
				                         isConstructorParameter: false)
				       {
					       DefaultValue = true
				       },
				       new TestParameter(AllowNullValuesForRangeDomains,
				                         typeof(bool),
				                         description: DocStrings
					                         .QaGdbConstraintFactory_AllowNullValuesForRangeDomains,
				                         isConstructorParameter: false)
				       {
					       DefaultValue = true
				       },
				       new TestParameter(FieldsParameterName,
				                         typeof(IList<string>),
				                         description: DocStrings
					                         .QaGdbConstraintFactory_Fields,
				                         isConstructorParameter: false),
			       }.AsReadOnly();
		}
	}
}
