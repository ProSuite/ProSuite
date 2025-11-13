using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.IssueFilters
{
	[UsedImplicitly]
	public class IfNearDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Near { get; }

		[DocIf(nameof(DocIfStrings.IfNear_0))]
		public IfNearDefinition(
			[DocIf(nameof(DocIfStrings.IfNear_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[DocIf(nameof(DocIfStrings.IfNear_near))]
			double near)
			: base(new[] { featureClass })
		{
			FeatureClass = featureClass;
			Near = near;
		}
	}
}
