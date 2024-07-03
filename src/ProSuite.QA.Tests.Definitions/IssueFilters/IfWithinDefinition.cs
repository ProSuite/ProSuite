using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.IssueFilters
{
	[UsedImplicitly]
	public class IfWithinDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }

		[DocIf(nameof(DocIfStrings.IfWithin_0))]
		public IfWithinDefinition(
			[DocIf(nameof(DocIfStrings.IfWithin_featureClass))]
			IFeatureClassSchemaDef featureClass)
			: base(new[] { featureClass })
		{
			FeatureClass = featureClass;
		}
	}
}
