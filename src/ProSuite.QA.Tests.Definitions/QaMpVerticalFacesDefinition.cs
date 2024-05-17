using System;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.ParameterTypes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[GeometryTest]
	public class QaMpVerticalFacesDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef MultiPatchClass { get; }
		public double NearAngle { get; }
		public double ToleranceAngle { get; }
		public AngleUnit AngleUnit { get; private set; }

		private readonly double _nearCosinus;
		private readonly double _toleranceSinus;
		private readonly double _xyTolerance;
		private readonly double _toleranceAngleRad;

		[Doc(nameof(DocStrings.QaMpVerticalFaces_0))]
		public QaMpVerticalFacesDefinition(
			[Doc(nameof(DocStrings.QaMpVerticalFaces_multiPatchClass))] [NotNull]
			IFeatureClassSchemaDef multiPatchClass,
			[Doc(nameof(DocStrings.QaMpVerticalFaces_nearAngle))]
			double nearAngle,
			[Doc(nameof(DocStrings.QaMpVerticalFaces_toleranceAngle))]
			double toleranceAngle)
			: base(multiPatchClass)
		{
			AngleUnit = AngleUnit.Degree;

			double nearAngleRad = MathUtils.ToRadians(nearAngle);
			_toleranceAngleRad = MathUtils.ToRadians(toleranceAngle);

			_nearCosinus = Math.Cos(nearAngleRad);
			_toleranceSinus = Math.Sin(_toleranceAngleRad);

			MultiPatchClass = multiPatchClass;
			NearAngle = nearAngle;
			ToleranceAngle = toleranceAngle;
		}
	}
}
