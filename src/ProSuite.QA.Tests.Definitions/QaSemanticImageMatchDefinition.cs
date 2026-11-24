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
	public class QaSemanticImageMatchDefinition : AlgorithmDefinition
	{
		/// <summary>
		/// The length of the column that stores text parameter values in the data
		/// dictionary.
		/// </summary>
		public const int MaximumRuleLength = 4000;

		private const int _defaultMinimumErrorProbability = 50;

		public const string DefaultCacheDirectory =
			@"%LOCALAPPDATA%\Dira GeoSystems\ProSuite\SemanticImageMatch";

		[Doc(nameof(DocStrings.QaSemanticImageMatch_0))]
		public QaSemanticImageMatchDefinition(
			[Doc(nameof(DocStrings.QaSemanticImageMatch_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_imageRaster))] [NotNull]
			IRasterDatasetDef imageRaster,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_rule))] [NotNull]
			string rule)
			: base(featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));
			Assert.ArgumentNotNull(imageRaster, nameof(imageRaster));
			Assert.ArgumentNotNullOrEmpty(apiUrl, nameof(apiUrl));
			AssertValidRule(rule);

			FeatureClass = featureClass;
			ImageRaster = imageRaster;
			InvolvedRasters = new List<IRasterDatasetDef> { imageRaster };
			ApiUrl = apiUrl;
			Rule = rule;
		}

		[Doc(nameof(DocStrings.QaSemanticImageMatch_0))]
		public QaSemanticImageMatchDefinition(
			[Doc(nameof(DocStrings.QaSemanticImageMatch_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_imageMosaic))] [NotNull]
			IMosaicRasterDatasetDef imageMosaic,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_apiUrl))] [NotNull]
			string apiUrl,
			[Doc(nameof(DocStrings.QaSemanticImageMatch_rule))] [NotNull]
			string rule)
			: this(featureClass, (IRasterDatasetDef) imageMosaic, apiUrl, rule) { }

		public static void AssertValidRule([CanBeNull] string rule)
		{
			Assert.ArgumentCondition(! string.IsNullOrWhiteSpace(rule), "The rule is empty");
			Assert.ArgumentCondition(
				rule.Length <= MaximumRuleLength,
				"The rule has {0} characters, at most {1} can be stored",
				rule.Length, MaximumRuleLength);
		}

		[NotNull]
		public IFeatureClassSchemaDef FeatureClass { get; }

		[NotNull]
		public IRasterDatasetDef ImageRaster { get; }

		[NotNull]
		public List<IRasterDatasetDef> InvolvedRasters { get; set; }


		[NotNull]
		public string ApiUrl { get; }

		[NotNull]
		[MultilineText]
		public string Rule { get; }

		[Doc(nameof(DocStrings.QaSemanticImageMatch_MinimumErrorProbability))]
		[TestParameter(_defaultMinimumErrorProbability)]
		public int MinimumErrorProbability { get; set; } = _defaultMinimumErrorProbability;

		[Doc(nameof(DocStrings.QaSemanticImageMatch_contextFeatureClasses))]
		[TestParameter]
		[CanBeNull]
		public IList<IFeatureClassSchemaDef> ContextFeatureClasses { get; set; }

		[Doc(nameof(DocStrings.QaSemanticImageMatch_CacheDirectory))]
		[TestParameter(DefaultCacheDirectory)]
		[CanBeNull]
		public string CacheDirectory { get; set; } = DefaultCacheDirectory;
	}
}
