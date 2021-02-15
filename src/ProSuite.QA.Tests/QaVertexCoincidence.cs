using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.PointEnumerators;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core;

namespace ProSuite.QA.Tests
{
	[GeometryTest]
	[UsedImplicitly]
	public class QaVertexCoincidence : ContainerTest
	{
		private const double _defaultUseXyTolerance = -1;
		private const bool _defaultRequireVertexOnNearbyEdge = true;

		[NotNull] private readonly VertexCoincidenceChecker _vertexCoincidenceChecker;
		private const bool _defaultIs3D = false;

		#region issue codes

		[CanBeNull] private static ITestIssueCodes _codes;

		// TODO: filter to "SameFeature" codes
		[NotNull]
		[UsedImplicitly]
		public static ITestIssueCodes Codes =>
			_codes ?? (_codes = new AggregatedTestIssueCodes(
				           VertexCoincidenceChecker.Codes));

		#endregion

		[Doc(nameof(DocStrings.QaVertexCoincidence_0))]
		public QaVertexCoincidence(
			[Doc(nameof(DocStrings.QaVertexCoincidence_featureClass))] [NotNull]
			IFeatureClass featureClass)
			: base((ITable) featureClass)
		{
			_vertexCoincidenceChecker =
				new VertexCoincidenceChecker(
					this, FormatComparison,
					DatasetUtils.GetMaximumXyTolerance(new[] {featureClass}))
				{
					Is3D = _defaultIs3D,
					VerifyWithinFeature = true,
					PointTolerance = _defaultUseXyTolerance,
					EdgeTolerance = _defaultUseXyTolerance,
					RequireVertexOnNearbyEdge = _defaultRequireVertexOnNearbyEdge
				};

			UpdateSearchDistance();
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_PointTolerance))]
		[TestParameter(_defaultUseXyTolerance)]
		public double PointTolerance
		{
			get { return _vertexCoincidenceChecker.PointTolerance; }
			set
			{
				_vertexCoincidenceChecker.PointTolerance = value;
				UpdateSearchDistance();
			}
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_EdgeTolerance))]
		[TestParameter(_defaultUseXyTolerance)]
		public double EdgeTolerance
		{
			get { return _vertexCoincidenceChecker.EdgeTolerance; }
			set
			{
				_vertexCoincidenceChecker.EdgeTolerance = value;
				UpdateSearchDistance();
			}
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_RequireVertexOnNearbyEdge))]
		[TestParameter(_defaultRequireVertexOnNearbyEdge)]
		public bool RequireVertexOnNearbyEdge
		{
			get { return _vertexCoincidenceChecker.RequireVertexOnNearbyEdge; }
			set { _vertexCoincidenceChecker.RequireVertexOnNearbyEdge = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_CoincidenceTolerance))]
		[TestParameter(0)]
		public double CoincidenceTolerance
		{
			get { return _vertexCoincidenceChecker.CoincidenceTolerance; }
			set { _vertexCoincidenceChecker.CoincidenceTolerance = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_Is3D))]
		[TestParameter(_defaultIs3D)]
		public bool Is3D
		{
			get { return _vertexCoincidenceChecker.Is3D; }
			set { _vertexCoincidenceChecker.Is3D = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_ZTolerance))]
		[TestParameter(0)]
		public double ZTolerance
		{
			get { return _vertexCoincidenceChecker.ZTolerance; }
			set { _vertexCoincidenceChecker.ZTolerance = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_ZCoincidenceTolerance))]
		[TestParameter(0)]
		public double ZCoincidenceTolerance
		{
			get { return _vertexCoincidenceChecker.ZCoincidenceTolerance; }
			set { _vertexCoincidenceChecker.ZCoincidenceTolerance = value; }
		}

		[Doc(nameof(DocStrings.QaVertexCoincidence_ReportCoordinates))]
		[TestParameter]
		public bool ReportCoordinates
		{
			get { return _vertexCoincidenceChecker.ReportCoordinates; }
			set { _vertexCoincidenceChecker.ReportCoordinates = value; }
		}

		protected override int ExecuteCore(IRow row, int tableIndex)
		{
			var feature = row as IFeature;
			if (feature == null)
			{
				return NoError;
			}

			return _vertexCoincidenceChecker.CheckCoincidence(
				PointsEnumeratorFactory.Create(feature, null),
				feature);
		}

		public override bool IsQueriedTable(int tableIndex)
		{
			return false;
		}

		public override bool RetestRowsPerIntersectedTile(int tableIndex)
		{
			return false;
		}

		private void UpdateSearchDistance()
		{
			SearchDistance = _vertexCoincidenceChecker.SearchDistance;
		}
	}
}
