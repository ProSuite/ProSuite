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
	public class QaMpHorizontalPerpendicularDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef MultiPatchClass { get; }
		public double NearAngle { get; }
		public double AzimuthTolerance { get; }
		public double HorizontalTolerance { get; }
		public bool ConnectedOnly { get; }
		public double ConnectedTolerance { get; }

		[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_0))]
		public QaMpHorizontalPerpendicularDefinition(
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_multiPatchClass))] [NotNull]
			IFeatureClassSchemaDef multiPatchClass,
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_nearAngle))]
			double nearAngle,
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_azimuthTolerance))]
			double azimuthTolerance,
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_horizontalTolerance))]
			double horizontalTolerance,
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_connectedOnly))]
			bool connectedOnly,
			[Doc(nameof(DocStrings.QaMpHorizontalPerpendicular_connectedTolerance))]
			double connectedTolerance)
			: base(multiPatchClass)
		{
			MultiPatchClass = multiPatchClass;
			NearAngle = nearAngle;
			AzimuthTolerance = azimuthTolerance;
			HorizontalTolerance = horizontalTolerance;
			ConnectedOnly = connectedOnly;
			ConnectedTolerance = connectedTolerance;
		}
	}
}
