using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports non-linear polycurve segments as errors
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaCurveDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }

		[Doc(nameof(DocStrings.QaCurve_0))]
		public QaCurveDefinition(
			[Doc(nameof(DocStrings.QaCurve_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass)
			: base(featureClass)
		{
			FeatureClass = featureClass;
		}

		[TestParameter]
		[Doc(nameof(DocStrings.QaCurve_AllowedNonLinearSegmentTypes))]
		public IList<NonLinearSegmentType> AllowedNonLinearSegmentTypes { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaCurve_GroupIssuesBySegmentType))]
		public bool GroupIssuesBySegmentType { get; set; }
	}
}
