using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.LinearNetwork;
using ProSuite.Commons.AO.Geometry.LinearNetwork.Editing;
using ProSuite.Commons.AO.Test.TestSupport;

namespace ProSuite.Commons.AO.Test.Geometry.LinearNetwork
{
	[TestFixture]
	public class LinearNetworkNodeUpdaterTest
	{
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

		[Test]
		public void CanUpdateWithEndPointSnapped()
		{
			//                /line1
			//               /
			//              * new end point
			//             /
			//            /\old end point
			//           /  \
			//    line 2/    \update-geometry

			// line 1 and2 are connected

			var line1Geometry =
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("line1.xml"));
			IFeature line1Feature = TestUtils.CreateMockFeature(line1Geometry);
			var lineClass = (FeatureClassMock) line1Feature.Class;
			ISpatialReference sr = DatasetUtils.GetSpatialReference(lineClass);
			Assert.NotNull(sr);
			var pointClass = new FeatureClassMock(2345, "Junctions",
			                                      esriGeometryType.esriGeometryPoint,
			                                      esriFeatureType.esriFTSimple, sr);

			var line2Geometry =
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("line2.xml"));
			IFeature line2Feature = lineClass.CreateFeature(line2Geometry);

			// line 3 joins the others in their connection point (building a junction)
			var updatedGeometry =
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("line3.xml"));
			IFeature featureToReshape = lineClass.CreateFeature(updatedGeometry);

			IPoint junction = ((IPolyline) updatedGeometry).FromPoint;
			IFeature junctionFeature = pointClass.CreateFeature(junction);

			ILinearNetworkFeatureFinder featureFinder =
				new NetworkFeatureFinderMock(line1Feature, line2Feature, featureToReshape,
				                             junctionFeature);

			IPolyline newPolyline = GeometryFactory.Clone((IPolyline) updatedGeometry);
			IPoint oldEndPoint = newPolyline.FromPoint;
			IPoint newEndPoint = GeometryFactory.CreatePoint(2709889.588, 1264759.757, 538.400);
			newPolyline.FromPoint = newEndPoint;

			double origLengthLine1 = GetLineFeatureLength(line1Feature);
			double origLengthLine2 = GetLineFeatureLength(line2Feature);

			var networkUpdater = new LinearNetworkNodeUpdater(featureFinder);

			networkUpdater.BarrierGeometryOriginal = (IPolycurve) featureToReshape.Shape;
			networkUpdater.BarrierGeometryChanged = newPolyline;

			networkUpdater.UpdateFeatureEndpoint(featureToReshape, newPolyline, null);

			IEnvelope refreshEnvelope = networkUpdater.RefreshEnvelope;

			Assert.NotNull(refreshEnvelope);

			Assert.True(GeometryUtils.Contains(refreshEnvelope, oldEndPoint));
			Assert.True(GeometryUtils.Contains(refreshEnvelope, newEndPoint));

			double lengthLine1 = GetLineFeatureLength(line1Feature);
			double lengthLine2 = GetLineFeatureLength(line2Feature);

			Assert.AreNotEqual(origLengthLine1, lengthLine1);
			Assert.AreNotEqual(origLengthLine2, lengthLine2);

			Assert.AreEqual(Math.Round(origLengthLine1 + origLengthLine2, 3),
			                Math.Round(lengthLine1 + lengthLine2, 3));

			Assert.IsTrue(GeometryUtils.Touches(line1Feature.Shape, line2Feature.Shape));
			Assert.IsTrue(GeometryUtils.Touches(line1Feature.Shape, featureToReshape.Shape));
			Assert.IsTrue(GeometryUtils.Touches(line2Feature.Shape, featureToReshape.Shape));

			Assert.IsTrue(GeometryUtils.AreEqual(newEndPoint, junctionFeature.Shape));
		}

		[Test]
		public void CanUpdateWithEndPointSnappedToExistingJunction()
		{
			SnapToExistingJunction(0);
		}

		[Test]
		public void CanUpdateWithEndPointAlmostSnappedToExistingJunction()
		{
			// Within tolerance but more than the resolution:
			SnapToExistingJunction(0.005);
		}

		[Test]
		public void CanUpdateWithEndPointNotSnapped()
		{
			//                /line1
			//               /
			//              /   * new end point
			//             /
			//            /\old end point
			//           /  \
			//    line 2/    \update-geometry

			// line 1 and2 are connected

			var line1Geometry = TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("line1.xml"));
			IFeature line1Feature = TestUtils.CreateMockFeature(line1Geometry);
			var lineClass = (FeatureClassMock) line1Feature.Class;
			ISpatialReference sr = DatasetUtils.GetSpatialReference(lineClass);
			Assert.NotNull(sr);
			var pointClass = new FeatureClassMock(2345, "Junctions",
			                                      esriGeometryType.esriGeometryPoint,
			                                      esriFeatureType.esriFTSimple, sr);

			var line2Geometry = TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("line2.xml"));
			IFeature line2Feature = lineClass.CreateFeature(line2Geometry);

			// line 3 joins the others in their connection point (building a junction)
			var updatedGeometry = TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("line3.xml"));
			IFeature featureToReshape = lineClass.CreateFeature(updatedGeometry);

			IPoint junction = ((IPolyline) updatedGeometry).FromPoint;
			IFeature junctionFeature = pointClass.CreateFeature(junction);

			ILinearNetworkFeatureFinder featureFinder =
				new NetworkFeatureFinderMock(line1Feature, line2Feature, featureToReshape,
				                             junctionFeature);

			IPolyline newPolyline = GeometryFactory.Clone((IPolyline) updatedGeometry);
			IPoint oldEndPoint = newPolyline.FromPoint;
			IPoint newEndPoint = GeometryFactory.CreatePoint(2709894.123, 1264759.757, 538.400);
			newPolyline.FromPoint = newEndPoint;

			double origLengthLine1 = GetLineFeatureLength(line1Feature);
			double origLengthLine2 = GetLineFeatureLength(line2Feature);

			var networkUpdater = new LinearNetworkNodeUpdater(featureFinder);

			networkUpdater.BarrierGeometryOriginal = (IPolycurve) featureToReshape.Shape;
			networkUpdater.BarrierGeometryChanged = newPolyline;

			networkUpdater.UpdateFeatureEndpoint(featureToReshape, newPolyline, null);

			IEnvelope refreshEnvelope = networkUpdater.RefreshEnvelope;
			Assert.NotNull(refreshEnvelope);

			Assert.True(GeometryUtils.Contains(refreshEnvelope, oldEndPoint));
			Assert.True(GeometryUtils.Contains(refreshEnvelope, newEndPoint));

			double lengthLine1 = GetLineFeatureLength(line1Feature);
			double lengthLine2 = GetLineFeatureLength(line2Feature);

			Assert.AreNotEqual(origLengthLine1, lengthLine1);
			Assert.AreNotEqual(origLengthLine2, lengthLine2);

			Assert.AreNotEqual(Math.Round(origLengthLine1 + origLengthLine2, 3),
			                   Math.Round(lengthLine1 + lengthLine2, 3));

			Assert.IsTrue(GeometryUtils.Touches(line1Feature.Shape, line2Feature.Shape));
			Assert.IsTrue(GeometryUtils.Touches(line1Feature.Shape, featureToReshape.Shape));
			Assert.IsTrue(GeometryUtils.Touches(line2Feature.Shape, featureToReshape.Shape));

			Assert.IsTrue(GeometryUtils.AreEqual(newEndPoint, junctionFeature.Shape));
		}

		private static void SnapToExistingJunction(double notQuiteSnapDistance)
		{
			//                 * new end point == line1.FromPoint
			//                /
			//               / line1
			//              /
			//             /
			//            /\old end point
			//           /  \
			//    line 2/    \update-geometry

			// line 1 and2 are connected

			var line1Geometry =
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("line1.xml"));
			IFeature line1Feature = TestUtils.CreateMockFeature(line1Geometry);
			var lineClass = (FeatureClassMock) line1Feature.Class;
			ISpatialReference sr = DatasetUtils.GetSpatialReference(lineClass);
			Assert.NotNull(sr);
			var pointClass = new FeatureClassMock(2345, "Junctions",
			                                      esriGeometryType.esriGeometryPoint,
			                                      esriFeatureType.esriFTSimple, sr);

			var line2Geometry =
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("line2.xml"));
			IFeature line2Feature = lineClass.CreateFeature(line2Geometry);

			// line 3 joins the others in their connection point (building a junction)
			var updatedGeometry =
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("line3.xml"));
			IFeature featureToReshape = lineClass.CreateFeature(updatedGeometry);

			IPoint junction = ((IPolyline) updatedGeometry).FromPoint;
			IFeature junctionFeature = pointClass.CreateFeature(junction);

			IPoint line1FromJunction = ((IPolyline) line1Geometry).FromPoint;
			IFeature line1FromJunctionFeature = pointClass.CreateFeature(line1FromJunction);

			ILinearNetworkFeatureFinder featureFinder =
				new NetworkFeatureFinderMock(line1Feature, line2Feature, featureToReshape,
				                             junctionFeature, line1FromJunctionFeature)
				{
					SearchTolerance = 0.01
				};

			// Single junction feature at line1.FromJunction:
			Assert.AreEqual(1, featureFinder.FindJunctionFeaturesAt(line1FromJunction).Count);

			IPolyline newPolyline = GeometryFactory.Clone((IPolyline) updatedGeometry);
			IPoint oldEndPoint = newPolyline.FromPoint;
			IPoint newEndPoint = GeometryFactory.Clone(line1FromJunction);

			newEndPoint.X += notQuiteSnapDistance;
			newPolyline.FromPoint = newEndPoint;

			double origLengthLine1 = GetLineFeatureLength(line1Feature);
			double origLengthLine2 = GetLineFeatureLength(line2Feature);

			var networkUpdater = new LinearNetworkNodeUpdater(featureFinder);

			networkUpdater.BarrierGeometryOriginal = (IPolycurve) featureToReshape.Shape;
			networkUpdater.BarrierGeometryChanged = newPolyline;

			networkUpdater.UpdateFeatureEndpoint(featureToReshape, newPolyline, null);

			IEnvelope refreshEnvelope = networkUpdater.RefreshEnvelope;
			Assert.NotNull(refreshEnvelope);

			// Still just 1 junction, the original junction is not dragged along
			Assert.AreEqual(1, featureFinder.FindJunctionFeaturesAt(line1FromJunction).Count);

			Assert.True(GeometryUtils.Intersects(refreshEnvelope, oldEndPoint));
			Assert.True(GeometryUtils.Intersects(refreshEnvelope, newEndPoint));

			double lengthLine1 = GetLineFeatureLength(line1Feature);
			double lengthLine2 = GetLineFeatureLength(line2Feature);

			Assert.AreEqual(origLengthLine1, lengthLine1);
			Assert.AreEqual(origLengthLine2, lengthLine2);

			Assert.IsTrue(GeometryUtils.Touches(line1Feature.Shape, line2Feature.Shape));
			Assert.IsTrue(GeometryUtils.Touches(line1Feature.Shape, featureToReshape.Shape));
			Assert.IsFalse(GeometryUtils.Intersects(line2Feature.Shape, featureToReshape.Shape));

			Assert.IsTrue(GeometryUtils.AreEqual(newEndPoint, line1FromJunctionFeature.Shape));

			IPolyline savedPolyline = (IPolyline) featureToReshape.Shape;

			// Must be exactly snapped, even if the junction was not quite hit:
			Assert.IsTrue(GeometryUtils.IsSamePoint(savedPolyline.FromPoint,
			                                        (IPoint) line1FromJunctionFeature.Shape, 0.001,
			                                        0.001));
		}

		private static double GetLineFeatureLength(IFeature feature)
		{
			var polyline = (IPolyline) feature.Shape;

			return GeometryUtils.GetLength(polyline, true);
		}
	}
}
