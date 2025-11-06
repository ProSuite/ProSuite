using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[TopologyTest]
	[UsedImplicitly]
	public class QaVertexCoincidenceOtherDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }

		public IList<IFeatureClassSchemaDef> RelatedClasses { get; }

		private const double _defaultUseXyTolerance = -1;
		private const bool _defaultRequireVertexOnNearbyEdge = true;
		private const bool _defaultIs3D = false;
		private const bool _defaultBidirectional = true;

		public string AllowedNonCoincidenceCondition { get; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceOther_0))]
		public QaVertexCoincidenceOtherDefinition(
			[Doc(nameof(DocStrings.QaVertexCoincidenceOther_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass,
			[Doc(nameof(DocStrings.QaVertexCoincidenceOther_relatedClass))] [NotNull]
			IFeatureClassSchemaDef relatedClass)
			: this(new[] { featureClass }, new[] { relatedClass }) { }

		[Doc(nameof(DocStrings.QaVertexCoincidenceOther_1))]
		public QaVertexCoincidenceOtherDefinition(
				[Doc(nameof(DocStrings.QaVertexCoincidenceOther_featureClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> featureClasses,
				[Doc(nameof(DocStrings.QaVertexCoincidenceOther_relatedClasses))] [NotNull]
				IList<IFeatureClassSchemaDef> relatedClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, relatedClasses, null) { }

		[Doc(nameof(DocStrings.QaVertexCoincidenceOther_2))]
		public QaVertexCoincidenceOtherDefinition(
			[Doc(nameof(DocStrings.QaVertexCoincidenceOther_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> featureClasses,
			[Doc(nameof(DocStrings.QaVertexCoincidenceOther_relatedClasses))] [NotNull]
			IList<IFeatureClassSchemaDef> relatedClasses,
			[Doc(nameof(DocStrings.QaVertexCoincidenceOther_allowedNonCoincidenceCondition))]
			[CanBeNull]
			string allowedNonCoincidenceCondition)
			: base(featureClasses.Union(relatedClasses))
		{
			FeatureClasses = featureClasses;
			RelatedClasses = relatedClasses;
			AllowedNonCoincidenceCondition = allowedNonCoincidenceCondition;
		}

		[Doc(nameof(DocStrings.QaVertexCoincidenceOther_PointTolerance))]
		[TestParameter(_defaultUseXyTolerance)]
		public double PointTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceOther_EdgeTolerance))]
		[TestParameter(_defaultUseXyTolerance)]
		public double EdgeTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceOther_RequireVertexOnNearbyEdge))]
		[TestParameter(_defaultRequireVertexOnNearbyEdge)]
		public bool RequireVertexOnNearbyEdge { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceOther_CoincidenceTolerance))]
		[TestParameter(0)]
		public double CoincidenceTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidence_ZTolerance))]
		[TestParameter(0)]
		public double ZTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidence_ZCoincidenceTolerance))]
		[TestParameter(0)]
		public double ZCoincidenceTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceOther_Is3D))]
		[TestParameter(_defaultIs3D)]
		public bool Is3D { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceOther_Bidirectional))]
		[TestParameter(_defaultBidirectional)]
		public bool Bidirectional { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidence_ReportCoordinates))]
		[TestParameter]
		public bool ReportCoordinates { get; set; }
	}
}
