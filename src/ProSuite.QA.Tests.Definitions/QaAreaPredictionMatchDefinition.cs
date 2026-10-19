using System.Collections.Generic;
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
	public class QaAreaPredictionMatchDefinition : AlgorithmDefinition
	{
		private const double _defaultMinimumArea = 6.0;
		private const double _defaultMinimumOverlapRatio = 0.9;
		private const AreaPredictionMatchMode _defaultMatchMode =
			AreaPredictionMatchMode.NewPredictions;

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_0))]
		public QaAreaPredictionMatchDefinition(
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_imageRaster))] [NotNull]
			IRasterDatasetDef imageRaster,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_modelName))] [NotNull]
			string modelName)
			: this(featureClass, imageRaster, apiUrl, modelName,
			       _defaultMinimumArea,
			       _defaultMinimumOverlapRatio) { }

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_0))]
		public QaAreaPredictionMatchDefinition(
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_imageRaster))] [NotNull]
			IRasterDatasetDef imageRaster,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_modelName))] [NotNull]
			string modelName,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_minimumArea))]
			double minimumArea,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_minimumOverlapRatio))]
			double minimumOverlapRatio)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentNotNull(imageRaster, nameof(imageRaster));
			Assert.ArgumentNotNullOrEmpty(apiUrl, nameof(apiUrl));
			Assert.ArgumentNotNullOrEmpty(modelName, nameof(modelName));
			Assert.ArgumentCondition(minimumArea >= 0,
			                         "Minimum area must be greater than or equal to zero");
			Assert.ArgumentCondition(minimumOverlapRatio >= 0 &&
			                         minimumOverlapRatio <= 1,
			                         "Minimum overlap ratio must be between 0 and 1");

			FeatureClass = featureClass;
			ImageRaster = imageRaster;
			InvolvedRasters = new List<IRasterDatasetDef> { imageRaster };
			ApiUrl = apiUrl;
			ModelName = modelName;
			MinimumArea = minimumArea;
			MinimumOverlapRatio = minimumOverlapRatio;
		}

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_0))]
		public QaAreaPredictionMatchDefinition(
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_imageMosaic))] [NotNull]
			IMosaicRasterDatasetDef imageMosaic,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_modelName))] [NotNull]
			string modelName)
			: this(featureClass, imageMosaic, apiUrl, modelName,
			       _defaultMinimumArea,
			       _defaultMinimumOverlapRatio) { }

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_0))]
		public QaAreaPredictionMatchDefinition(
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_imageMosaic))] [NotNull]
			IMosaicRasterDatasetDef imageMosaic,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_modelName))] [NotNull]
			string modelName,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_minimumArea))]
			double minimumArea,
			[Doc(nameof(DocStrings.QaAreaPredictionMatch_minimumOverlapRatio))]
			double minimumOverlapRatio)
			: this(featureClass, (IRasterDatasetDef) imageMosaic, apiUrl, modelName,
			       minimumArea, minimumOverlapRatio) { }

		[NotNull]
		public IFeatureClassSchemaDef FeatureClass { get; }

		[NotNull]
		public IRasterDatasetDef ImageRaster { get; }

		[NotNull]
		public List<IRasterDatasetDef> InvolvedRasters { get; set; }

		[NotNull]
		public string ApiUrl { get; }

		[NotNull]
		public string ModelName { get; }

		public double MinimumArea { get; }

		public double MinimumOverlapRatio { get; }

		[Doc(nameof(DocStrings.QaAreaPredictionMatch_MatchMode))]
		[TestParameter(_defaultMatchMode)]
		public AreaPredictionMatchMode MatchMode { get; set; } =
			_defaultMatchMode;
	}
}
