using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.IssueFilters
{
	[UsedImplicitly]
	public class IfIntersectingDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }

		[DocIf(nameof(DocIfStrings.IfIntersecting_0))]
		public IfIntersectingDefinition(
			[DocIf(nameof(DocIfStrings.IfIntersecting_featureClass))]
			IFeatureClassSchemaDef featureClass)
			: base(new[] { featureClass })
		{
			FeatureClass = featureClass;
		}
	}
}
