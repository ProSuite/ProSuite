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
	public class QaRelRegularExpressionDefinition : TestFactoryDefinition
	{
		public string MatchIsErrorName { get; } = "MatchIsError";
		public string PatternDescriptionName { get; } = "PatternDescription";

		public override string TestDescription => DocStrings.QaRelConstraint;

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
				           new TestParameter("pattern", typeof(string),
				                             Tests.Documentation.DocStrings
				                                  .QaRegularExpression_pattern),
				           new TestParameter("fieldNames", typeof(IList<string>),
				                             Tests.Documentation.DocStrings
				                                  .QaRegularExpression_fieldNames),
				           new TestParameter(MatchIsErrorName, typeof(bool),
				                             Tests.Documentation.DocStrings
				                                  .QaRegularExpression_matchIsError,
				                             isConstructorParameter: false),
				           new TestParameter(PatternDescriptionName, typeof(string),
				                             Tests.Documentation.DocStrings
				                                  .QaRegularExpression_patternDescription,
				                             isConstructorParameter: false)
			           };

			return list.AsReadOnly();
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(
				InstanceUtils.TryGetAlgorithmName(nameof(QaRelRegularExpressionDefinition)));
		}
	}
}
