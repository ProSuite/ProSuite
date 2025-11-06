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
	public class QaConstraintsListFactoryDefinition : TestFactoryDefinition
	{
		private const string _tableAttribute = "table";
		private const string _constraintsTableAttribute = "constraintsTable";
		private const string _expressionField = "expressionField";
		private const string _expressionIsError = "expressionIsError";
		private const string _descriptionField = "descriptionField";

		public override string TestDescription => DocStrings.QaConstraintsListFactory;

		protected override IList<TestParameter> CreateParameters()
		{
			var list =
				new List<TestParameter>
				{
					new TestParameter(_tableAttribute, typeof(ITableSchemaDef),
					                  DocStrings.QaConstraintsListFactory_table),
					new TestParameter(_constraintsTableAttribute, typeof(ITableSchemaDef),
					                  DocStrings.QaConstraintsListFactory_constraintsTable),
					new TestParameter(_expressionField, typeof(string),
					                  DocStrings.QaConstraintsListFactory_expressionField),
					new TestParameter(_expressionIsError, typeof(bool),
					                  DocStrings.QaConstraintsListFactory_expressionIsError),
					new TestParameter(_descriptionField, typeof(string),
					                  DocStrings.QaConstraintsListFactory_descriptionField),
				};

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaConstraintDefinition)));
		}
	}
}
