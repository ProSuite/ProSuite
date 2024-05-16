using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Reports horizontal with almost near azimuth
	/// </summary>
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpHorizontalHeightsDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef MultiPatchClass { get; }
		public double NearHeight { get; set; }
		public double HeightTolerance { get; set; }

		private readonly double _nearHeight;
		private readonly double _heightTolerance;

		[Doc(nameof(DocStrings.QaMpHorizontalHeights_0))]
		public QaMpHorizontalHeightsDefinition(
			[Doc(nameof(DocStrings.QaMpHorizontalHeights_multiPatchClass))] [NotNull]
			IFeatureClassSchemaDef
				multiPatchClass,
			[Doc(nameof(DocStrings.QaMpHorizontalHeights_nearHeight))]
			double nearHeight,
			[Doc(nameof(DocStrings.QaMpHorizontalHeights_heightTolerance))]
			double heightTolerance)
			: base(multiPatchClass)
		{
			_nearHeight = nearHeight;
			_heightTolerance = heightTolerance;

			MultiPatchClass = multiPatchClass;
			NearHeight = nearHeight;
			HeightTolerance = heightTolerance;
		}
	}
}
