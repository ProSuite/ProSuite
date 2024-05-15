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
	public class QaMinSegAngleDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double Limit { get; }
		public bool Is3D { get; }

		private const bool _defaultUseTangents = false;
		private const AngleUnit _defaultAngularUnit = DefaultAngleUnit;

		[Doc(nameof(DocStrings.QaMinSegAngle_0))]
		public QaMinSegAngleDefinition(
			[Doc(nameof(DocStrings.QaMinSegAngle_featureClass))]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaMinSegAngle_limit))]
			double limit,
			[Doc(nameof(DocStrings.QaMinSegAngle_is3D))]
			bool is3D)
			: base(featureClass)
		{
			UseTangents = _defaultUseTangents;
			AngularUnit = _defaultAngularUnit;

			FeatureClass = featureClass;
			Limit = limit;
			Is3D = is3D;
		}

		[Doc(nameof(DocStrings.QaMinSegAngle_0))]
		public QaMinSegAngleDefinition(
				[Doc(nameof(DocStrings.QaMinSegAngle_featureClass))]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaMinSegAngle_limit))]
				double limit)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, limit, false) { }

		[TestParameter(_defaultUseTangents)]
		[Doc(nameof(DocStrings.QaMinSegAngle_UseTangents))]
		public bool UseTangents { get; set; }

		[TestParameter(DefaultAngleUnit)]
		[Doc(nameof(DocStrings.QaLineIntersectAngle_AngularUnit))]
		public AngleUnit AngularUnit { get; set; } = DefaultAngleUnit;
	}
}
