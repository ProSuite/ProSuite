using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[TopologyTest]
	[UsedImplicitly]
	public class QaVertexCoincidenceSelfDefinition : AlgorithmDefinition
	{
		public IList<IFeatureClassSchemaDef> FeatureClasses { get; }

		public string AllowedNonCoincidenceCondition { get; }

		private const double _defaultUseXyTolerance = -1;
		private const bool _defaultRequireVertexOnNearbyEdge = true;
		private const bool _defaultIs3D = false;
		private const bool _defaultVerifyWithinFeature = false;

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_0))]
		public QaVertexCoincidenceSelfDefinition(
			[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass)
			: this(new[] { featureClass }) { }

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_1))]
		public QaVertexCoincidenceSelfDefinition(
				[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_featureClasses))] [NotNull]
				IList<IFeatureClassSchemaDef>
					featureClasses)
			// ReSharper disable once IntroduceOptionalParameters.Global
			: this(featureClasses, null) { }

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_2))]
		public QaVertexCoincidenceSelfDefinition(
			[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_featureClasses))] [NotNull]
			IList<IFeatureClassSchemaDef>
				featureClasses,
			[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_allowedNonCoincidenceCondition))]
			[CanBeNull]
			string
				allowedNonCoincidenceCondition)
			: base(featureClasses)
		{
			FeatureClasses = featureClasses;
			AllowedNonCoincidenceCondition = allowedNonCoincidenceCondition;
		}

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_PointTolerance))]
		[TestParameter(_defaultUseXyTolerance)]

		public double PointTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_EdgeTolerance))]
		[TestParameter(_defaultUseXyTolerance)]
		public double EdgeTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_RequireVertexOnNearbyEdge))]
		[TestParameter(_defaultRequireVertexOnNearbyEdge)]

		public bool RequireVertexOnNearbyEdge { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_CoincidenceTolerance))]
		[TestParameter(0)]
		public double CoincidenceTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_Is3D))]
		[TestParameter(_defaultIs3D)]
		public bool Is3D { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidenceSelf_VerifyWithinFeature))]
		[TestParameter(_defaultVerifyWithinFeature)]
		public bool VerifyWithinFeature { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidence_ZTolerance))]
		[TestParameter(0)]

		public double ZTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidence_ZCoincidenceTolerance))]
		[TestParameter(0)]
		public double ZCoincidenceTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidence_ReportCoordinates))]
		[TestParameter]
		public bool ReportCoordinates { get; set; }
	}
}
