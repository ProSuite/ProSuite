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
	public class QaRelConstraintDefinition : TestFactoryDefinition
	{
		public override string TestDescription => DocStrings.QaRelConstraint;

		public string ParameterNameApplyFilterInDatabase { get; } =
			"ApplyFilterExpressionsInDatabase";

		protected override IList<TestParameter> CreateParameters()
		{
			// relationTables is partly redundant with relation, but needed for following reasons: 
			// - used to derive dataset constraints
			// - needed to be displayed in Tests displayed by dataset !!

			var list = new List<TestParameter>
			           {
				           new TestParameter("relationTables", typeof(IList<ITableSchemaDef>),
				                             DocStrings.QaRelConstraint_relationTables),
				           new TestParameter("relation", typeof(string),
				                             DocStrings.QaRelConstraint_relation),
				           new TestParameter("join", typeof(JoinType),
				                             DocStrings.QaRelConstraint_join),
				           new TestParameter("constraint", typeof(IList<string>),
				                             DocStrings.QaRelConstraint_constraint),
				           new TestParameter(
					           ParameterNameApplyFilterInDatabase, typeof(bool),
					           DocStrings.QaRelConstraint_ApplyFilterExpressionsInDatabase,
					           isConstructorParameter: false) { DefaultValue = false }
			           };

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaRelConstraintDefinition)));
		}
	}
}
