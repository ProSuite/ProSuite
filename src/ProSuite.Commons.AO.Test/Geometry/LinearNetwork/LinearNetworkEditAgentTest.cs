using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.LinearNetwork;
using ProSuite.Commons.AO.Geometry.LinearNetwork.Editing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.Geometry.LinearNetwork
{
	[TestFixture]
	public class LinearNetworkEditAgentTest
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
		public void CanCreateJunctionsForNewEdgeJunctions()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			FeatureClassMock edgeClass;
			LinearNetworkDef networkDef = CreateSimpleNetworkDef(out edgeClass, out _);

			var networkFeatureFinder = new NetworkFeatureFinderMock();

			LinearNetworkEditAgent observer = new LinearNetworkEditAgent(
				networkDef, networkFeatureFinder);

			observer.NoCaching = true;

			IPolyline edge1Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600000, 1200000, 450, double.NaN, sr),
				GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr));

			CreateInOperation(() => CreateFeature(edgeClass, edge1Polyline),
			                  observer);

			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			// New features get added to the feature finder's cache: 1 edge + 2 junctions
			Assert.AreEqual(3, networkFeatureFinder.TargetFeatureCandidates.Count);

			IFeature fromJunction = networkFeatureFinder.TargetFeatureCandidates.First(
				f => f.Shape.GeometryType ==
				     esriGeometryType
					     .esriGeometryPoint);

			IFeature toJunction = networkFeatureFinder.TargetFeatureCandidates.Last(
				f => f.Shape.GeometryType ==
				     esriGeometryType.esriGeometryPoint);

			Assert.IsTrue(GeometryUtils.AreEqual(edge1Polyline.FromPoint, fromJunction.Shape));
			Assert.IsTrue(GeometryUtils.AreEqual(edge1Polyline.ToPoint, toJunction.Shape));

			//// Make sure the fake features are known to the feature finder:
			//networkFeatureFinder.TargetFeatureCandidates.Add(fromJunction);
			//networkFeatureFinder.TargetFeatureCandidates.Add(toJunction);

			// Add another adjacent feature connected to the first:
			IPolyline edge2Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr),
				GeometryFactory.CreatePoint(2600030, 1200000, 451, double.NaN, sr));

			CreateInOperation(() => CreateFeature(edgeClass, edge2Polyline),
			                  observer);

			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			// original 3 + 1 edge + 1 junction
			Assert.AreEqual(5, networkFeatureFinder.TargetFeatureCandidates.Count);

			IFeature newJunction = networkFeatureFinder.TargetFeatureCandidates.Last(
				f => f.Shape.GeometryType ==
				     esriGeometryType
					     .esriGeometryPoint);

			Assert.IsTrue(GeometryUtils.AreEqual(edge2Polyline.ToPoint, newJunction.Shape));

			// TODO: Adajcent feature at different Z
		}

		[Test]
		public void CanSplitEdgeOnNodeInsertion()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			FeatureClassMock edgeClass, junctionClass;
			LinearNetworkDef networkDef = CreateSimpleNetworkDef(out edgeClass, out junctionClass);

			var networkFeatureFinder = new NetworkFeatureFinderMock();

			LinearNetworkEditAgent observer = new LinearNetworkEditAgent(
				networkDef, networkFeatureFinder);

			observer.NoCaching = true;

			// Existing feature:
			IPolyline edge1Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600000, 1200000, 450, double.NaN, sr),
				GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr));

			IFeature existingFeature = CreateInOperation(
				() => CreateFeature(edgeClass, edge1Polyline), observer);

			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());
			Assert.IsTrue(existingFeature == observer.GetCreatedInLastOperation().First());

			// New features get added to the feature finder's cache: 1 edge + 2 junctions
			Assert.AreEqual(3, networkFeatureFinder.TargetFeatureCandidates.Count);

			// Create edge snapped onto interior (Z actually does not matter):
			IPolyline edge2Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600010, 1200010, 450, double.NaN, sr),
				GeometryFactory.CreatePoint(2600010, 1200005, 451, double.NaN, sr));

			IFeature snappedFeature = CreateInOperation(
				() => CreateFeature(edgeClass, edge2Polyline), observer);

			Assert.NotNull(snappedFeature);

			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			// 3 original features + 1 new edge + 2 junctions + 1 new split-edge
			Assert.AreEqual(7, networkFeatureFinder.TargetFeatureCandidates.Count);

			// Now just add a junction feature
			IPoint junctionPoint =
				GeometryFactory.CreatePoint(2600002, 1200001, 450, double.NaN, sr);

			IFeature insertedJunction = CreateInOperation(
				() => CreateFeature(junctionClass, junctionPoint), observer);
			Assert.NotNull(insertedJunction);

			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			// 7 original features + 1 new junction + 1 new split-edge
			Assert.AreEqual(9, networkFeatureFinder.TargetFeatureCandidates.Count);
		}

		[Test]
		public void CanSplitEdgeOnNodeUpdate()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			FeatureClassMock edgeClass, junctionClass;
			LinearNetworkDef networkDef = CreateSimpleNetworkDef(out edgeClass, out junctionClass);

			var networkFeatureFinder = new NetworkFeatureFinderMock();

			LinearNetworkEditAgent observer = new LinearNetworkEditAgent(
				                                  networkDef, networkFeatureFinder)
			                                  {
				                                  NoCaching = true
			                                  };

			// Existing feature:
			IPolyline edge1Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600000, 1200000, 450, double.NaN, sr),
				GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr));

			CreateInOperation(() => CreateFeature(edgeClass, edge1Polyline), observer);

			// Add a junction feature that does not intersect the edge:
			IPoint junctionPoint =
				GeometryFactory.CreatePoint(2600002, 1200002, 450, double.NaN, sr);

			IFeature insertedJunction = CreateInOperation(
				() => CreateFeature(junctionClass, junctionPoint), observer);
			Assert.NotNull(insertedJunction);
			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			// Now move it onto the edge:
			IPoint movedPoint = GeometryFactory.CreatePoint(2600002, 1200001, 450, double.NaN, sr);

			IFeature updated = UpdateInOperation(() =>
			{
				insertedJunction.Shape = movedPoint;
				insertedJunction.Store();
				return insertedJunction;
			}, observer);

			Assert.AreEqual(1, observer.GetUpdatedInLastOperation().Count());
			Assert.IsTrue(updated == observer.GetUpdatedInLastOperation().First());

			// 1 original edge + its 2 junctions + 1 junction + 1 split-insert edge
			Assert.AreEqual(5, networkFeatureFinder.TargetFeatureCandidates.Count);
		}

		[Test]
		public void DoesNotSplitEdgeOnNodeUpdateForNonSplittingNode()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			FeatureClassMock edgeClass, junctionClass;
			LinearNetworkDef networkDef = CreateSimpleNetworkDef(out edgeClass, out junctionClass);

			// Set network classes to not split:
			foreach (LinearNetworkClassDef classDefinition in networkDef.NetworkClassDefinitions)
			{
				classDefinition.Splitting = false;
			}

			var networkFeatureFinder = new NetworkFeatureFinderMock();

			LinearNetworkEditAgent observer = new LinearNetworkEditAgent(
				                                  networkDef, networkFeatureFinder)
			                                  {
				                                  NoCaching = true
			                                  };

			// Existing feature:
			IPolyline edge1Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600000, 1200000, 450, double.NaN, sr),
				GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr));

			CreateInOperation(() => CreateFeature(edgeClass, edge1Polyline), observer);

			// Add a junction feature that does not intersect the edge:
			IPoint junctionPoint =
				GeometryFactory.CreatePoint(2600002, 1200002, 450, double.NaN, sr);

			IFeature insertedJunction = CreateInOperation(
				() => CreateFeature(junctionClass, junctionPoint), observer);
			Assert.NotNull(insertedJunction);
			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			// Now move it onto the edge:
			IPoint movedPoint = GeometryFactory.CreatePoint(2600002, 1200001, 450, double.NaN, sr);

			IFeature updated = UpdateInOperation(() =>
			{
				insertedJunction.Shape = movedPoint;
				insertedJunction.Store();
				return insertedJunction;
			}, observer);

			Assert.AreEqual(1, observer.GetUpdatedInLastOperation().Count());
			Assert.IsTrue(updated == observer.GetUpdatedInLastOperation().First());

			// 1 original edge + its 2 junctions + 1 junction + 0 split-insert edge
			Assert.AreEqual(4, networkFeatureFinder.TargetFeatureCandidates.Count);
		}

		[Test]
		public void CanSplitEdgeOnEdgeUpdate()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			FeatureClassMock edgeClass;
			LinearNetworkDef networkDef = CreateSimpleNetworkDef(out edgeClass, out _);

			var networkFeatureFinder = new NetworkFeatureFinderMock();

			LinearNetworkEditAgent observer = new LinearNetworkEditAgent(
				                                  networkDef, networkFeatureFinder)
			                                  {
				                                  NoCaching = true
			                                  };

			// Existing feature:
			IPolyline edge1Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600000, 1200000, 450, double.NaN, sr),
				GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr));

			IFeature existingFeature = CreateInOperation(
				() => CreateFeature(edgeClass, edge1Polyline), observer);

			// Add an edge feature that does not yet intersect the edge:
			IPolyline edge2Polyline =
				GeometryFactory.CreatePolyline(
					GeometryFactory.CreatePoint(2600000, 1200005, 450, double.NaN, sr),
					GeometryFactory.CreatePoint(2600008, 1200005, 450, double.NaN, sr));

			IFeature secondEdge = CreateInOperation(
				() => CreateFeature(edgeClass, edge2Polyline), observer);
			Assert.NotNull(secondEdge);
			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			// Now move the end of the second edge onto the edge1:
			IPoint movedPoint = GeometryFactory.CreatePoint(2600010, 1200005, 450, double.NaN, sr);
			IFeature updated =
				UpdateInOperation(() => MoveEndPoint(secondEdge, movedPoint), observer);

			Assert.AreEqual(1, observer.GetUpdatedInLastOperation().Count());
			Assert.IsTrue(updated == observer.GetUpdatedInLastOperation().First());

			// 1 original edge + its 2 junctions + 1 second edge + its 2 juctions
			// + 1 split-insert edge
			Assert.AreEqual(7, networkFeatureFinder.TargetFeatureCandidates.Count);
		}

		[Test]
		public void CanSplitEdgeAndKeepConnectedStable()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			FeatureClassMock edgeClass;
			LinearNetworkDef networkDef = CreateSimpleNetworkDef(out edgeClass, out _);

			var networkFeatureFinder = new NetworkFeatureFinderMock();

			LinearNetworkEditAgent observer = new LinearNetworkEditAgent(
				                                  networkDef, networkFeatureFinder)
			                                  {
				                                  NoCaching = true
			                                  };

			// Existing feature:
			IPolyline edge1Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600000, 1200000, 450, double.NaN, sr),
				GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr));

			IFeature existingFeature = CreateInOperation(
				() => CreateFeature(edgeClass, edge1Polyline), observer);

			// Add a connected edge feature:
			IPolyline edge2Polyline =
				GeometryFactory.CreatePolyline(
					GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr),
					GeometryFactory.CreatePoint(2600030, 1200005, 450, double.NaN, sr));

			IPolyline edge2PolylineOrig = GeometryFactory.Clone(edge2Polyline);

			IFeature secondEdge = CreateInOperation(
				() => CreateFeature(edgeClass, edge2Polyline), observer);
			Assert.NotNull(secondEdge);
			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			IPolyline edge3Polyline =
				GeometryFactory.CreatePolyline(
					GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr),
					GeometryFactory.CreatePoint(2600030, 1200015, 450, double.NaN, sr));

			IFeature thirdEdge = CreateInOperation(
				() => CreateFeature(edgeClass, edge3Polyline), observer);

			// Now split by an existing edge (chopper style):
			IPolyline cuttingPolyline =
				GeometryFactory.CreatePolyline(
					new[]
					{
						WKSPointZUtils.CreatePoint(2600020, 1200010, 452),
						WKSPointZUtils.CreatePoint(2600020, 1200000, 452),
						WKSPointZUtils.CreatePoint(2600015, 1200000, 452),
						WKSPointZUtils.CreatePoint(2600015, 1200020, 450)
					}, sr);

			IFeature cuttingEdge = CreateInOperation(
				() => CreateFeature(edgeClass, cuttingPolyline), observer);

			IPoint splitPoint =
				GeometryFactory.CreatePoint(2600015, 1200007.5, 450, double.NaN, sr);

			SplitInOperation(existingFeature, splitPoint, observer);

			Assert.AreEqual(1, observer.GetUpdatedInLastOperation().Count());

			// Only the actual update - no dragging along of the connected edge!
			// 1 original edge + its 2 junctions + 1 second edge + its 1 extra juction
			// + 1 split-insert edge + 1 split junction
			//Assert.AreEqual(7, networkFeatureFinder.TargetFeatureCandidates.Count);

			Assert.IsTrue(GeometryUtils.AreEqual(edge2PolylineOrig, secondEdge.Shape));
		}

		[Test]
		public void CanKeepConnectedStableOnMerge()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			FeatureClassMock edgeClass;
			LinearNetworkDef networkDef = CreateSimpleNetworkDef(out edgeClass, out _);

			var networkFeatureFinder = new NetworkFeatureFinderMock();

			LinearNetworkEditAgent observer = new LinearNetworkEditAgent(
				                                  networkDef, networkFeatureFinder)
			                                  {
				                                  NoCaching = true
			                                  };

			// Existing feature:
			IPolyline edge1Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600000, 1200000, 450, double.NaN, sr),
				GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr));

			IFeature existingFeature = CreateInOperation(
				() => CreateFeature(edgeClass, edge1Polyline), observer);

			// Add a connected edge feature:
			IPolyline edge2Polyline =
				GeometryFactory.CreatePolyline(
					GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr),
					GeometryFactory.CreatePoint(2600030, 1200005, 450, double.NaN, sr));

			IPolyline edge2PolylineOrig = GeometryFactory.Clone(edge2Polyline);

			IFeature secondEdge = CreateInOperation(
				() => CreateFeature(edgeClass, edge2Polyline), observer);
			Assert.NotNull(secondEdge);
			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			IPolyline edge3Polyline =
				GeometryFactory.CreatePolyline(
					GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr),
					GeometryFactory.CreatePoint(2600030, 1200015, 450, double.NaN, sr));

			IFeature thirdEdge = CreateInOperation(
				() => CreateFeature(edgeClass, edge3Polyline), observer);

			// Now merge features 1 and 2:
			MergeInOperation(existingFeature, thirdEdge, observer);

			Assert.AreEqual(1, observer.GetUpdatedInLastOperation().Count());

			// Make sure the second edge has not changed:
			Assert.IsTrue(GeometryUtils.AreEqual(edge2PolylineOrig, secondEdge.Shape));
		}

		[Test]
		public void CanMoveOrCreateJunctionsForMovedEdgeEndpoint()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			FeatureClassMock edgeClass;
			LinearNetworkDef networkDef = CreateSimpleNetworkDef(out edgeClass, out _);

			var networkFeatureFinder = new NetworkFeatureFinderMock();

			LinearNetworkEditAgent observer = new LinearNetworkEditAgent(
				                                  networkDef, networkFeatureFinder)
			                                  {
				                                  NoCaching = true
			                                  };

			// Existing feature:
			IPolyline edge1Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600000, 1200000, 450, double.NaN, sr),
				GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr));

			IFeature existingFeature = CreateInOperation(
				() => CreateFeature(edgeClass, edge1Polyline), observer);

			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			// New features get added to the feature finder's cache: 1 edge + 2 junctions
			Assert.AreEqual(3, networkFeatureFinder.TargetFeatureCandidates.Count);

			IFeature fromJunction = networkFeatureFinder.TargetFeatureCandidates.First(
				f => f.Shape.GeometryType ==
				     esriGeometryType
					     .esriGeometryPoint);

			IFeature toJunction = networkFeatureFinder.TargetFeatureCandidates.Last(
				f => f.Shape.GeometryType ==
				     esriGeometryType.esriGeometryPoint);

			Assert.IsTrue(GeometryUtils.AreEqual(edge1Polyline.FromPoint, fromJunction.Shape));
			Assert.IsTrue(GeometryUtils.AreEqual(edge1Polyline.ToPoint, toJunction.Shape));

			// Move end point --> the orphan junction is moved
			IPoint newEnd = edge1Polyline.ToPoint;
			newEnd.X += 4;
			UpdateInOperation(() => MoveEndPoint(existingFeature, newEnd), observer);

			Assert.AreEqual(0, observer.GetCreatedInLastOperation().Count());
			Assert.AreEqual(1, observer.GetUpdatedInLastOperation().Count());

			// The 3 original features + 0 new junctions (the original was moved to the right place)
			Assert.AreEqual(3, networkFeatureFinder.TargetFeatureCandidates.Count);

			IList<IFeature> movedJunction = networkFeatureFinder.FindJunctionFeaturesAt(newEnd);
			Assert.AreEqual(1, movedJunction.Count);
			Assert.IsTrue(GeometryUtils.AreEqual(newEnd, movedJunction[0].Shape));
			Assert.AreEqual(toJunction, movedJunction[0]);

			// 'Protect' the junction with a new polyline feature

			// Add another adjacent feature connected to the first:
			IPolyline edge2Polyline = GeometryFactory.CreatePolyline(
				newEnd,
				GeometryFactory.CreatePoint(2600030, 1200000, 451, double.NaN, sr));

			CreateInOperation(() => CreateFeature(edgeClass, edge2Polyline),
			                  observer);

			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			// The 3 previous features + the new edge + 1 new junction
			Assert.AreEqual(5, networkFeatureFinder.TargetFeatureCandidates.Count);

			// Now move the end at the 'connected' edge end / junction -> they should be moved along
			IPoint newEnd2 = GeometryFactory.Clone(newEnd);
			newEnd2.Y += 4;
			UpdateInOperation(() => MoveEndPoint(existingFeature, newEnd2), observer);

			// At the original location, no junction and no edge should be left:
			IList<IFeature> junctionsAtOrig = networkFeatureFinder.FindJunctionFeaturesAt(newEnd);
			Assert.AreEqual(0, junctionsAtOrig.Count);

			IList<IFeature> edgesAtOrig = networkFeatureFinder.FindEdgeFeaturesAt(newEnd);
			Assert.AreEqual(0, edgesAtOrig.Count);

			// But at the newEnd2 of the original polyline they should be now:
			IList<IFeature> newJunction2 = networkFeatureFinder.FindJunctionFeaturesAt(newEnd2);
			Assert.AreEqual(1, newJunction2.Count);
			Assert.IsTrue(GeometryUtils.AreEqual(newEnd2, newJunction2[0].Shape));

			// in fact, it should be the original junction
			Assert.AreEqual(toJunction, newJunction2[0]);

			// And both the explicitly and the implicitly updated edges:
			IList<IFeature> newEdge2 = networkFeatureFinder.FindEdgeFeaturesAt(newEnd2);
			Assert.AreEqual(2, newEdge2.Count);

			// TOP-5262: End points moved in Z only should still result in "vertical drag-along"
			// Now move the end at the 'connected' edge end / junction -> they should be moved along
			IPoint newEnd2z = GeometryFactory.Clone(newEnd2);
			newEnd2z.Z += 2.5;
			UpdateInOperation(() => MoveEndPoint(existingFeature, newEnd2z), observer);

			// At the original location, the updated junction and edge should be found
			junctionsAtOrig = networkFeatureFinder.FindJunctionFeaturesAt(newEnd2);
			Assert.AreEqual(1, junctionsAtOrig.Count);
			Assert.AreEqual(newEnd2z.Z, ((Point) junctionsAtOrig[0].Shape).Z);

			edgesAtOrig = networkFeatureFinder.FindEdgeFeaturesAt(newEnd2);
			Assert.AreEqual(2, edgesAtOrig.Count);

			foreach (IFeature edge in edgesAtOrig)
			{
				IPoint endPoint = GetLineEndPointAt(newEnd2, (IPolyline) edge.Shape);
				Assert.AreEqual(newEnd2z.Z, endPoint.Z);
			}
		}

		[Test]
		public void CanDeleteOrphanedJunctionsForDeletedEdge()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			FeatureClassMock edgeClass;
			LinearNetworkDef networkDef = CreateSimpleNetworkDef(out edgeClass, out _);

			var networkFeatureFinder = new NetworkFeatureFinderMock();

			LinearNetworkEditAgent observer =
				new LinearNetworkEditAgent(networkDef, networkFeatureFinder)
				{
					NoCaching = true,
					DeleteOrphanedJunctionsOnEdgeDelete = true
				};

			IPolyline edge1Polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePoint(2600000, 1200000, 450, double.NaN, sr),
				GeometryFactory.CreatePoint(2600020, 1200010, 452, double.NaN, sr));

			IFeature edgeFeature = CreateInOperation(() => CreateFeature(edgeClass, edge1Polyline),
			                                         observer);

			Assert.AreEqual(1, observer.GetCreatedInLastOperation().Count());

			// New features get added to the feature finder's cache: 1 edge + 2 junctions
			Assert.AreEqual(3, networkFeatureFinder.TargetFeatureCandidates.Count);

			DeleteInOperation(edgeFeature, observer);

			Assert.AreEqual(0, networkFeatureFinder.TargetFeatureCandidates.Count);

			//
			// Recreate and delete without the Delete option:
			edgeFeature = CreateInOperation(() => CreateFeature(edgeClass, edge1Polyline),
			                                observer);

			Assert.AreEqual(3, networkFeatureFinder.TargetFeatureCandidates.Count);

			observer.DeleteOrphanedJunctionsOnEdgeDelete = false;

			DeleteInOperation(edgeFeature, observer);

			// Only the 2 junctions are left
			Assert.AreEqual(2, networkFeatureFinder.TargetFeatureCandidates.Count);
		}

		private static void SplitInOperation(IFeature polylineFeature,
		                                     IPoint splitPoint,
		                                     IEditOperationObserver observer)
		{
			observer.StartedOperation();

			var originalLine = (IPolyline) polylineFeature.ShapeCopy;

			IPolyline shorterGeometry;
			IPolyline longerGeometry;

			Assert.True(GeometryUtils.TrySplitPolyline(
				            originalLine, splitPoint, true, out shorterGeometry,
				            out longerGeometry));

			const bool exceptShape = true;
			IFeature newFeature = GdbObjectUtils.DuplicateFeature(polylineFeature, exceptShape);

			GdbObjectUtils.SetFeatureShape(newFeature, shorterGeometry);
			GdbObjectUtils.SetFeatureShape(polylineFeature, longerGeometry);

			newFeature.Store();
			polylineFeature.Store();

			observer.Updating(polylineFeature);
			observer.Creating(newFeature);

			observer.IsCompletingOperation = true;
			observer.CompletingOperation();
			observer.IsCompletingOperation = false;
		}

		private static void MergeInOperation(IFeature update,
		                                     IFeature delete,
		                                     IEditOperationObserver observer)
		{
			observer.StartedOperation();

			IGeometry resultGeometry = GeometryUtils.Union(update.Shape, delete.Shape);

			GdbObjectUtils.SetFeatureShape(update, resultGeometry);
			observer.Updating(update);

			delete.Delete();
			observer.Deleting(delete);

			observer.IsCompletingOperation = true;
			observer.CompletingOperation();
			observer.IsCompletingOperation = false;
		}

		private IPoint GetLineEndPointAt(IPoint expectedLocation, IPolyline edgeShape)
		{
			IPoint from = edgeShape.FromPoint;

			if (GeometryUtils.AreEqualInXY(expectedLocation, from))
			{
				return from;
			}

			IPoint to = edgeShape.ToPoint;

			Assert.IsTrue(GeometryUtils.AreEqualInXY(expectedLocation, to));

			return to;
		}

		private static LinearNetworkDef CreateSimpleNetworkDef(out FeatureClassMock edgeClass,
		                                                       out FeatureClassMock junctionClass)
		{
			edgeClass = new FeatureClassMock("STRASSE", esriGeometryType.esriGeometryPolyline, 1);
			junctionClass = new FeatureClassMock("KNOTEN", esriGeometryType.esriGeometryPoint, 2);

			LinearNetworkClassDef edgeClassDef = new LinearNetworkClassDef(edgeClass);
			LinearNetworkClassDef junctionClassDef = new LinearNetworkClassDef(junctionClass);

			LinearNetworkDef networkDef = new LinearNetworkDef(
				new List<LinearNetworkClassDef>
				{
					edgeClassDef,
					junctionClassDef
				}, junctionClass);

			return networkDef;
		}

		private static IFeature CreateInOperation(Func<IFeature> createFeatureProc,
		                                          IEditOperationObserver observer)
		{
			observer.StartedOperation();

			IFeature createdFeature = createFeatureProc();

			observer.Creating(createdFeature);

			observer.IsCompletingOperation = true;
			observer.CompletingOperation();
			observer.IsCompletingOperation = false;

			return createdFeature;
		}

		private static IFeature UpdateInOperation(Func<IFeature> updateFeatureProc,
		                                          IEditOperationObserver observer)
		{
			observer.StartedOperation();

			IFeature updatedFeature = updateFeatureProc();

			observer.Updating(updatedFeature);

			observer.IsCompletingOperation = true;
			observer.CompletingOperation();
			observer.IsCompletingOperation = false;

			return updatedFeature;
		}

		private void DeleteInOperation(IFeature featureToDelete, IEditOperationObserver observer)
		{
			observer.StartedOperation();

			featureToDelete.Delete();

			observer.Deleting(featureToDelete);

			observer.IsCompletingOperation = true;
			observer.CompletingOperation();
			observer.IsCompletingOperation = false;
		}

		private static IFeature CreateFeature([NotNull] FeatureClassMock networkClass,
		                                      [NotNull] IGeometry shape)
		{
			IFeature result = networkClass.CreateFeature(shape);

			result.Store();

			return result;
		}

		private IFeature MoveEndPoint(IFeature polylineFeature, IPoint newEnd)
		{
			IPolyline polyline = (IPolyline) polylineFeature.ShapeCopy;

			polyline.ToPoint = newEnd;

			polylineFeature.Shape = polyline;

			polylineFeature.Store();

			return polylineFeature;
		}
	}
}
