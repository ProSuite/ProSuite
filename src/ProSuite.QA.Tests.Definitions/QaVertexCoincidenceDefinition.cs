using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests
{
	[GeometryTest]
	[UsedImplicitly]
	public class QaVertexCoincidenceDefinition : AlgorithmDefinition
	{
		public IFeatureClassSchemaDef FeatureClass { get; }

		private const double _defaultUseXyTolerance = -1;
		private const bool _defaultRequireVertexOnNearbyEdge = true;
		private const bool _defaultIs3D = false;

		[Doc(nameof(DocStrings.QaVertexCoincidence_0))]
		public QaVertexCoincidenceDefinition(
			[Doc(nameof(DocStrings.QaVertexCoincidence_featureClass))] [NotNull]
			IFeatureClassSchemaDef featureClass)
			: base(featureClass)

		{
			FeatureClass = featureClass;
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_PointTolerance))]
		[TestParameter(_defaultUseXyTolerance)]
		public double PointTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidence_EdgeTolerance))]
		[TestParameter(_defaultUseXyTolerance)]
		public double EdgeTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidence_RequireVertexOnNearbyEdge))]
		[TestParameter(_defaultRequireVertexOnNearbyEdge)]
		public bool RequireVertexOnNearbyEdge { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidence_CoincidenceTolerance))]
		[TestParameter(0)]
		public double CoincidenceTolerance { get; set; }

		[Doc(nameof(DocStrings.QaVertexCoincidence_Is3D))]
		[TestParameter(_defaultIs3D)]
		public bool Is3D { get; set; }

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
