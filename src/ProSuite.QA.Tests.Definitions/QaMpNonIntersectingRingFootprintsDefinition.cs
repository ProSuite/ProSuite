using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpNonIntersectingRingFootprintsDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef MultiPatchClass { get; }
		public bool AllowIntersectionsForDifferentPointIds { get; }

		private const double _defaultResolutionFactor = 1;
		private double _resolutionFactor = _defaultResolutionFactor;

		[Doc(nameof(DocStrings.QaMpNonIntersectingRingFootprints_0))]
		public QaMpNonIntersectingRingFootprintsDefinition(
			[Doc(nameof(DocStrings.QaMpNonIntersectingRingFootprints_multiPatchClass))] [NotNull]
			IFeatureClassSchemaDef multiPatchClass,
			[Doc(nameof(DocStrings
				            .QaMpNonIntersectingRingFootprints_allowIntersectionsForDifferentPointIds))]
			bool allowIntersectionsForDifferentPointIds)
			: base(multiPatchClass)
		{
			Assert.ArgumentNotNull(multiPatchClass, nameof(multiPatchClass));

			MultiPatchClass = multiPatchClass;
			AllowIntersectionsForDifferentPointIds = allowIntersectionsForDifferentPointIds;
		}

		[UsedImplicitly]
		[TestParameter(_defaultResolutionFactor)]
		[Doc(nameof(DocStrings.QaMpNonIntersectingRingFootprints_ResolutionFactor))]
		public double ResolutionFactor
		{
			get { return _resolutionFactor; }
			set
			{
				Assert.ArgumentCondition(value >= 1, "value must be >= 1");

				_resolutionFactor = value;
			}
		}
	}
}
