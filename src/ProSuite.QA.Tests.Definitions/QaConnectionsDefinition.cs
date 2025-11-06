using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.ParameterTypes;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[InternallyUsedTest]
	public class QaConnectionsDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }
		public IList<QaConnectionRule> Rules { get; }
		public double Tolerance { get; }

		#region Constructors

		[Doc(nameof(DocStrings.QaConnections_0))]
		public QaConnectionsDefinition(
			[Doc(nameof(DocStrings.QaConnections_featureClasses))]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaConnections_rules_0))]
			IList<string[]> rules)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			Rules = ToQaConnectionRuleList(rules);
		}

		[Doc(nameof(DocStrings.QaConnections_1))]
		public QaConnectionsDefinition(
			[Doc(nameof(DocStrings.QaConnections_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaConnections_rules_1))]
			IEnumerable<string> rules)
			: this(new List<IFeatureClassSchemaDef> { featureClass },
			       (IList<QaConnectionRule>) rules)
		{
			List<string[]> ruleArrays = rules.Select(rule => new[] { rule }).ToList();

			Rules = ToQaConnectionRuleList(ruleArrays);
		}

		[Doc(nameof(DocStrings.QaConnections_2))]
		public QaConnectionsDefinition(
				[Doc(nameof(DocStrings.QaConnections_featureClasses))]
				IList<IFeatureClassSchemaDef> featureClasses,
				[Doc(nameof(DocStrings.QaConnections_rules_1))]
				IList<QaConnectionRule> rules)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, rules, 0) { }

		[Doc(nameof(DocStrings.QaConnections_3))]
		public QaConnectionsDefinition(
			[Doc(nameof(DocStrings.QaConnections_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaConnections_rules_1))]
			IList<QaConnectionRule> rules)
			: this(new[] { featureClass }, rules) { }

		// TODO document/private?
		public QaConnectionsDefinition(
			IList<IFeatureClassSchemaDef> featureClasses,
			IList<QaConnectionRule> rules,
			double tolerance)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			Rules = rules;
			Tolerance = tolerance;
		}

		#endregion

		private List<QaConnectionRule> ToQaConnectionRuleList([NotNull] ICollection<string[]> rules)
		{
			List<QaConnectionRule> ruleList = new List<QaConnectionRule>(rules.Count);

			foreach (string[] rule in rules)
			{
				ruleList.Add(new QaConnectionRule(InvolvedTables, rule));
			}

			return ruleList;
		}
	}
}
