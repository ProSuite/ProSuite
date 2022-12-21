using System.Linq;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.LinearNetwork.ShortestPath;
using ProSuite.Commons.AO.Test.TestSupport;

namespace ProSuite.Commons.AO.Test.Geometry.LinearNetwork
{
	[TestFixture]
	public class PolylineGraphConnectivityTest
	{
		#region Setup/Teardown

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		#endregion

		[Test]
		public void CanDetermineNodeDegreeAtAreaOfInterestBoundaryOneEdgePartiallyOutside()
		{
			// General idea: The southernmost edge can be traversed from the inside to the
			// outside, but not vice versa. Degree 3 should be determined correctly
			//  _______________________
			//  |                     |
			//  |    \     /          |
			//  |     \   /           |
			//  |      \ /            |
			//  |_______|_____________|
			//          |
			//          |
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IEnvelope aoi = GeometryFactory.CreateEnvelope(2600000, 1200000, 2600100, 1200100, sr);

			FeatureClassMock edgeClass =
				new FeatureClassMock("ROADS", esriGeometryType.esriGeometryPolyline, 1);

			PolylineGraphConnectivity connectivity = new PolylineGraphConnectivity(sr, aoi);

			// Create node with degree 3 where one feature is partially outside the AOI

			IPoint junction = GeometryFactory.CreatePoint(2600050, 1200020, sr);
			IPoint outsidePoint = GeometryFactory.CreatePoint(2600050, 1199900, sr);

			FeatureMock feature1 =
				new FeatureMock(11, edgeClass)
				{
					Shape = GeometryFactory.CreatePolyline(outsidePoint, junction)
				};

			FeatureMock feature2 =
				new FeatureMock(12, edgeClass)
				{
					Shape = GeometryFactory.CreatePolyline(
						junction,
						GeometryFactory.CreatePoint(2600030, 1200050, sr))
				};

			FeatureMock feature3 =
				new FeatureMock(13, edgeClass)
				{
					Shape = GeometryFactory.CreatePolyline(
						junction,
						GeometryFactory.CreatePoint(2600070, 1200050, sr))
				};

			connectivity.AddConnectivity(feature1, false);
			connectivity.AddConnectivity(feature2, false);
			connectivity.AddConnectivity(feature3, false);

			var edges = connectivity.GetIncidentEdges(junction).ToList();

			Assert.AreEqual(3, edges.Count);
			Assert.IsFalse(connectivity.TryGetNodeIndex(outsidePoint, out _));

			connectivity.RemoveConnection(new GdbObjectReference(feature1),
			                              new[] {junction, outsidePoint});

			Assert.AreEqual(2, connectivity.GetIncidentEdges(junction).Count());

			// Now add it again while respecting the orientation and therefore not adding to
			// from-node connections because the from node is outside (index -1)
			// an neither to to-node connections because out-flow is against line orientation.
			connectivity.AddConnectivity(feature1, true);
			Assert.AreEqual(2, connectivity.GetIncidentEdges(junction).Count());

			// Reverse the partially outside feature:
			var feature1Shape = (IPolyline) feature1.ShapeCopy;
			feature1Shape.ReverseOrientation();
			feature1.Shape = feature1Shape;

			connectivity = new PolylineGraphConnectivity(sr, aoi);
			connectivity.AddConnectivity(new[] {feature1, feature2, feature3}, false);

			edges = connectivity.GetIncidentEdges(junction).ToList();

			Assert.AreEqual(3, edges.Count);
			Assert.IsFalse(connectivity.TryGetNodeIndex(outsidePoint, out _));

			// Now respect the orientation and this time it should not matter because out-flow
			// is the natural line orientation
			connectivity = new PolylineGraphConnectivity(sr, aoi);
			connectivity.AddConnectivity(new[] {feature1, feature2, feature3}, true);

			edges = connectivity.GetIncidentEdges(junction).ToList();

			Assert.AreEqual(3, edges.Count);
			Assert.IsFalse(connectivity.TryGetNodeIndex(outsidePoint, out _));
		}

		[Test]
		public void CanDetermineNodeDegreeAtAreaOfInterestBoundaryTwoEdgesPartiallyOutside()
		{
			// General idea: The partially inside edges can be traversed from the inside to the
			// outside, but not vice versa. Degree 3 should be determined correctly.
			//    \
			//  ___\___________________
			//  |   \                 |
			//  |    \     /          |
			//  |     \   /           |
			//  |      \ /            |
			//  |_______|_____________|
			//          |
			//          |
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IEnvelope aoi = GeometryFactory.CreateEnvelope(2600000, 1200000, 2600100, 1200100, sr);

			FeatureClassMock edgeClass =
				new FeatureClassMock("ROADS", esriGeometryType.esriGeometryPolyline, 1);

			PolylineGraphConnectivity connectivity = new PolylineGraphConnectivity(sr, aoi);

			// Create node with degree 3 where two features are partially outside the AOI

			IPoint junction = GeometryFactory.CreatePoint(2600050, 1200020, sr);

			FeatureMock feature1 =
				new FeatureMock(11, edgeClass)
				{
					Shape = GeometryFactory.CreatePolyline(
						GeometryFactory.CreatePoint(2600050, 1199900, sr),
						junction)
				};

			FeatureMock feature2 =
				new FeatureMock(12, edgeClass)
				{
					Shape = GeometryFactory.CreatePolyline(
						junction,
						GeometryFactory.CreatePoint(2600010, 1200250, sr))
				};

			FeatureMock feature3 =
				new FeatureMock(13, edgeClass)
				{
					Shape = GeometryFactory.CreatePolyline(
						junction,
						GeometryFactory.CreatePoint(2600070, 1200050, sr))
				};

			connectivity.AddConnectivity(feature1, false);
			connectivity.AddConnectivity(feature2, false);
			connectivity.AddConnectivity(feature3, false);

			Assert.AreEqual(3, connectivity.GetIncidentEdges(junction).Count());

			connectivity = new PolylineGraphConnectivity(sr, aoi);
			connectivity.AddConnectivity(new[] {feature1, feature2, feature3}, true);

			Assert.AreEqual(2, connectivity.GetIncidentEdges(junction).Count());
		}

		[Test]
		public void CanDetermineNodeDegreeAtAreaOfInterestBoundaryOneEdgeFullyOutside()
		{
			// General idea: The southernmost edge can be traversed from the inside to the
			// outside, but not vice versa. Degree 3 should be determined correctly
			//  _______________________
			//  |                     |
			//  |    \     /          |
			//  |_____\___/___________|
			//         \ /
			//          |
			//          |
			//          |
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IEnvelope aoi = GeometryFactory.CreateEnvelope(2600000, 1200000, 2600100, 1200100, sr);

			FeatureClassMock edgeClass =
				new FeatureClassMock("ROADS", esriGeometryType.esriGeometryPolyline, 1);

			PolylineGraphConnectivity connectivity = new PolylineGraphConnectivity(sr, aoi);

			// Create node with degree 3 where one feature is partially outside the AOI

			IPoint junction = GeometryFactory.CreatePoint(2600050, 1199900, sr);

			FeatureMock feature1 =
				new FeatureMock(11, edgeClass)
				{
					Shape = GeometryFactory.CreatePolyline(
						GeometryFactory.CreatePoint(2600050, 1199800, sr),
						junction)
				};

			FeatureMock feature2 =
				new FeatureMock(12, edgeClass)
				{
					Shape = GeometryFactory.CreatePolyline(
						junction,
						GeometryFactory.CreatePoint(2600030, 1200050, sr))
				};

			FeatureMock feature3 =
				new FeatureMock(13, edgeClass)
				{
					Shape = GeometryFactory.CreatePolyline(
						junction,
						GeometryFactory.CreatePoint(2600070, 1200050, sr))
				};

			connectivity.AddConnectivity(feature1, false);
			connectivity.AddConnectivity(feature2, false);
			connectivity.AddConnectivity(feature3, false);

			var edges = connectivity.GetIncidentEdges(junction).ToList();

			Assert.AreEqual(0, edges.Count);
		}
	}
}
