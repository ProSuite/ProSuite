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
	public class QaMpHorizontalAzimuthsDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef MultiPatchClass { get; }
		public double NearAngle { get; }
		public double AzimuthTolerance { get; }
		public double HorizontalTolerance { get; }
		public bool PerRing { get; }


		[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_0))]
		public QaMpHorizontalAzimuthsDefinition(
			[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_multiPatchClass))] [NotNull]
			IFeatureClassSchemaDef multiPatchClass,
			[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_nearAngle))]
			double nearAngle,
			[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_azimuthTolerance))]
			double azimuthTolerance,
			[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_horizontalTolerance))]
			double horizontalTolerance,
			[Doc(nameof(DocStrings.QaMpHorizontalAzimuths_perRing))]
			bool perRing)
			: base(multiPatchClass)
		{
			MultiPatchClass = multiPatchClass;
			NearAngle = nearAngle;
			AzimuthTolerance = azimuthTolerance;
			HorizontalTolerance = horizontalTolerance;
			PerRing = perRing;
		}
	}
}
