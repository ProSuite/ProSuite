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
	public class QaWithinBoxDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }
		public double XMin { get; }
		public double YMin { get; }
		public double XMax { get; }
		public double YMax { get; }
		public bool ReportOnlyOutsideParts { get; }

		[Doc(nameof(DocStrings.QaWithinBox_0))]
		public QaWithinBoxDefinition(
				[Doc(nameof(DocStrings.QaWithinBox_featureClass))] [NotNull]
				IFeatureClassSchemaDef featureClass,
				[Doc(nameof(DocStrings.QaWithinBox_xMin))]
				double xMin,
				[Doc(nameof(DocStrings.QaWithinBox_yMin))]
				double yMin,
				[Doc(nameof(DocStrings.QaWithinBox_xMax))]
				double xMax,
				[Doc(nameof(DocStrings.QaWithinBox_yMax))]
				double yMax)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClass, xMin, yMin, xMax, yMax, false) { }

		[Doc(nameof(DocStrings.QaWithinBox_0))]
		public QaWithinBoxDefinition(
			[Doc(nameof(DocStrings.QaWithinBox_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaWithinBox_xMin))]
			double xMin,
			[Doc(nameof(DocStrings.QaWithinBox_yMin))]
			double yMin,
			[Doc(nameof(DocStrings.QaWithinBox_xMax))]
			double xMax,
			[Doc(nameof(DocStrings.QaWithinBox_yMax))]
			double yMax,
			[Doc(nameof(DocStrings.QaWithinBox_reportOnlyOutsideParts))]
			bool reportOnlyOutsideParts)
			: base(featureClass)
		{
			Assert.ArgumentNotNaN(xMin, nameof(xMin));
			Assert.ArgumentNotNaN(yMin, nameof(yMin));
			Assert.ArgumentNotNaN(xMax, nameof(xMax));
			Assert.ArgumentNotNaN(yMax, nameof(yMax));
			Assert.ArgumentCondition(xMin < xMax, "xMin must be smaller than xMax");
			Assert.ArgumentCondition(yMin < yMax, "yMin must be smaller than yMax");

			FeatureClass = featureClass;
			XMin = xMin;
			YMin = yMin;
			XMax = xMax;
			YMax = yMax;
			ReportOnlyOutsideParts = reportOnlyOutsideParts;
		}
	}
}
