using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[UsedImplicitly]
	[AttributeTest]
	public class QaValidCoordinateFieldsDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; set; }
		public string XCoordinateFieldName { get; }
		public string YCoordinateFieldName { get; }
		public string ZCoordinateFieldName { get; }
		public double XyTolerance { get; }
		public double ZTolerance { get; }
		public string Culture { get; }

		[Doc(nameof(DocStrings.QaValidCoordinateFields_0))]
		public QaValidCoordinateFieldsDefinition(
			[Doc(nameof(DocStrings.QaValidCoordinateFields_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_xCoordinateFieldName))] [CanBeNull]
			string xCoordinateFieldName,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_yCoordinateFieldName))] [CanBeNull]
			string yCoordinateFieldName,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_zCoordinateFieldName))] [CanBeNull]
			string zCoordinateFieldName,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_xyTolerance))]
			double xyTolerance,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_zTolerance))]
			double zTolerance,
			[Doc(nameof(DocStrings.QaValidCoordinateFields_culture))] [CanBeNull]
			string culture)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentCondition(
				featureClass.ShapeType == ProSuiteGeometryType.Point,
				$"{featureClass.ShapeType}, only point feature classes are supported");

			FeatureClass = featureClass;
			XCoordinateFieldName = xCoordinateFieldName;
			YCoordinateFieldName = yCoordinateFieldName;
			ZCoordinateFieldName = zCoordinateFieldName;
			XyTolerance = xyTolerance;
			ZTolerance = zTolerance;
			Culture = culture;
		}

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaValidCoordinateFields_AllowXYFieldValuesForUndefinedShape))]
		public bool AllowXYFieldValuesForUndefinedShape { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaValidCoordinateFields_AllowZFieldValueForUndefinedShape))]
		public bool AllowZFieldValueForUndefinedShape { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaValidCoordinateFields_AllowMissingZFieldValueForDefinedShape))]
		public bool AllowMissingZFieldValueForDefinedShape { get; set; }

		[TestParameter(false)]
		[Doc(nameof(DocStrings.QaValidCoordinateFields_AllowMissingXYFieldValueForDefinedShape))]
		public bool AllowMissingXYFieldValueForDefinedShape { get; set; }
	}
}
