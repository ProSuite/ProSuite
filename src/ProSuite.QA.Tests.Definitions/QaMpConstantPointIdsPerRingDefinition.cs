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
	public class QaMpConstantPointIdsPerRingDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef MultiPatchClass { get; }
		public bool IncludeInnerRings { get; }

		[Doc(nameof(DocStrings.QaMpConstantPointIdsPerRing_0))]
		public QaMpConstantPointIdsPerRingDefinition(
			[Doc(nameof(DocStrings.QaMpConstantPointIdsPerRing_multiPatchClass))] [NotNull]
			IFeatureClassSchemaDef
				multiPatchClass,
			[Doc(nameof(DocStrings.QaMpConstantPointIdsPerRing_includeInnerRings))]
			bool includeInnerRings)
			: base(multiPatchClass)
		{
			MultiPatchClass = multiPatchClass;
			IncludeInnerRings = includeInnerRings;
		}
	}
}
