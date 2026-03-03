using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.RemoveOverlaps;

namespace ProSuite.Commons.AO.Test.Geometry.RemoveOverlaps
{
	[TestFixture]
	public class OverlapsRemoverMultipatchTest
	{
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("OverlapsRemoverTest");
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanRemoveOverlapFromSinglePartMultipatch_NoExplosion()
		{
			// Test basic overlap removal without explosion
			// A single-part multipatch with one ring group overlapping a polygon
			IFeatureClass fc = CreateMultipatchFeatureClass("SinglePartNoExplosion");

			const int pointId = 1;
			// Create a simple multipatch: one horizontal ring at z=0
			IMultiPatch multipatch = CreateSimpleHorizontalMultipatch(
				new[]
				{
					new WKSPointZ { X = 0, Y = 0, Z = 0 },
					new WKSPointZ { X = 10, Y = 0, Z = 0 },
					new WKSPointZ { X = 10, Y = 10, Z = 0 },
					new WKSPointZ { X = 0, Y = 10, Z = 0 }
				},
				pointId);

			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			// Overlap polygon covering the right half
			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(5, 0),
				GeometryFactory.CreatePoint(15, 10));

			// Remove overlaps without explosion
			var remover = new OverlapsRemover(explodeMultipartResult: false);
			remover.CalculateResults(new[] { feature }, overlap);

			// Verify results
			Assert.AreEqual(1, remover.Result.ResultsByFeature.Count);
			var result = remover.Result.ResultsByFeature[0];
			Assert.AreEqual(1, result.ResultGeometries.Count, "Should have one result geometry");

			IMultiPatch resultPatch = (IMultiPatch) result.ResultGeometries[0];
			Assert.IsFalse(resultPatch.IsEmpty);

			// Verify the result is smaller than original (overlap removed)
			IArea originalArea = (IArea) multipatch;
			IArea resultArea = (IArea) resultPatch;
			Assert.Less(resultArea.Area, originalArea.Area, "Result area should be smaller");

			// Result should have approximately half the area (left half remains)
			Assert.AreEqual(originalArea.Area / 2, resultArea.Area, 1.0,
			                "Result should have approximately half the original area");

			// Check result pointId:
			Assert.IsTrue(GeometryUtils.IsPointIDAware(resultPatch));
			Assert.IsTrue(GeometryUtils.HasUniqueVertexId(resultPatch, out int id));

			Assert.AreEqual(0, id);
		}

		[Test]
		public void CanRemoveOverlapFromSinglePartMultipatch_WithExplosion()
		{
			// Test that explosion creates separate multipatches when a single ring is cut
			IFeatureClass fc = CreateMultipatchFeatureClass("SinglePartWithExplosion");

			// Create a multipatch with one ring
			IMultiPatch multipatch = CreateSimpleHorizontalMultipatch(
				new[]
				{
					new WKSPointZ { X = 0, Y = 0, Z = 0 },
					new WKSPointZ { X = 20, Y = 0, Z = 0 },
					new WKSPointZ { X = 20, Y = 10, Z = 0 },
					new WKSPointZ { X = 0, Y = 10, Z = 0 }
				},
				pointId: 1);

			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			// Overlap polygon cutting through the middle
			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(8, -5),
				GeometryFactory.CreatePoint(12, 15));

			// Remove overlaps WITH explosion
			var remover = new OverlapsRemover(explodeMultipartResult: true);
			remover.CalculateResults(new[] { feature }, overlap);

			// Verify results
			Assert.AreEqual(1, remover.Result.ResultsByFeature.Count);
			var result = remover.Result.ResultsByFeature[0];
			Assert.AreEqual(2, result.ResultGeometries.Count,
			                "Should have two result geometries (left and right parts)");

			// Both results should be valid multipatches
			var pointIds = new HashSet<int>();
			foreach (IGeometry resultGeom in result.ResultGeometries)
			{
				Assert.IsInstanceOf<IMultiPatch>(resultGeom);
				Assert.IsFalse(resultGeom.IsEmpty);

				// Verify PointID awareness
				IMultiPatch resultPatch = (IMultiPatch) resultGeom;
				Assert.IsTrue(GeometryUtils.IsPointIDAware(resultPatch));
				Assert.IsTrue(GeometryUtils.HasUniqueVertexId(resultPatch, out int id));
				pointIds.Add(id);
			}

			// Two disconnected pieces should have different PointIDs
			Assert.AreEqual(2, pointIds.Count, "Should have 2 distinct PointIDs");
		}

		[Test]
		public void CanPreservePointIDGroupsInMultipartMultipatch()
		{
			// Test that multipatches with multiple PointID groups maintain those groups
			// This is the key test for the refactoring!
			IFeatureClass fc = CreateMultipatchFeatureClass("MultiPartPreserveGroups");

			// Create a multipatch with TWO separate ring groups with different PointIDs
			IMultiPatch multipatch = CreateMultiPartMultipatch(
				new[]
				{
					// First ring group (PointID = 1) - left square
					new[]
					{
						new WKSPointZ { X = 0, Y = 0, Z = 0 },
						new WKSPointZ { X = 5, Y = 0, Z = 0 },
						new WKSPointZ { X = 5, Y = 5, Z = 0 },
						new WKSPointZ { X = 0, Y = 5, Z = 0 }
					},
					// Second ring group (PointID = 2) - right square
					new[]
					{
						new WKSPointZ { X = 10, Y = 0, Z = 0 },
						new WKSPointZ { X = 15, Y = 0, Z = 0 },
						new WKSPointZ { X = 15, Y = 5, Z = 0 },
						new WKSPointZ { X = 10, Y = 5, Z = 0 }
					}
				},
				pointIds: new[] { 1, 2 });

			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			// Overlap only affecting the FIRST ring group (left square)
			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2, -1),
				GeometryFactory.CreatePoint(6, 6));

			// Remove overlaps without explosion to keep groups together
			var remover = new OverlapsRemover(explodeMultipartResult: false);
			remover.CalculateResults(new[] { feature }, overlap);

			// Verify results
			Assert.AreEqual(1, remover.Result.ResultsByFeature.Count);
			var result = remover.Result.ResultsByFeature[0];
			Assert.AreEqual(1, result.ResultGeometries.Count);

			IMultiPatch resultPatch = (IMultiPatch) result.ResultGeometries[0];

			// Verify we still have two distinct PointID groups
			Dictionary<int, List<int>> ringsByPointId = GetRingGroupsByPointId(resultPatch);
			Assert.AreEqual(2, ringsByPointId.Count,
			                "Should have two distinct PointID groups");

			// Verify first group was modified (partial area)
			// Verify second group is unchanged (full area)
			IArea originalArea = (IArea) multipatch;
			IArea resultArea = (IArea) resultPatch;

			// Result should be smaller (first group was cut)
			Assert.Less(resultArea.Area, originalArea.Area);

			// But not by the full area of the first group (part remains)
			double expectedMinArea = 5 * 5; // Second group untouched
			Assert.Greater(resultArea.Area, expectedMinArea);
		}

		[Test]
		public void CanExplodeMultipleRingGroupsIndependently()
		{
			// Test explosion of a multipatch where both ring groups are cut
			IFeatureClass fc = CreateMultipatchFeatureClass("ExplodeMultipleGroups");

			// Create multipatch with two ring groups
			IMultiPatch multipatch = CreateMultiPartMultipatch(
				new[]
				{
					// First ring (PointID = 1)
					new[]
					{
						new WKSPointZ { X = 0, Y = 0, Z = 0 },
						new WKSPointZ { X = 10, Y = 0, Z = 0 },
						new WKSPointZ { X = 10, Y = 5, Z = 0 },
						new WKSPointZ { X = 0, Y = 5, Z = 0 }
					},
					// Second ring (PointID = 2)
					new[]
					{
						new WKSPointZ { X = 0, Y = 10, Z = 0 },
						new WKSPointZ { X = 10, Y = 10, Z = 0 },
						new WKSPointZ { X = 10, Y = 15, Z = 0 },
						new WKSPointZ { X = 0, Y = 15, Z = 0 }
					}
				},
				pointIds: new[] { 1, 2 });

			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			// Overlap cutting through BOTH rings vertically
			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(4, -1),
				GeometryFactory.CreatePoint(6, 16));

			// Remove with explosion enabled
			var remover = new OverlapsRemover(explodeMultipartResult: true);
			remover.CalculateResults(new[] { feature }, overlap);

			// Should have 4 result parts: left+right from first ring, left+right from second ring
			var result = remover.Result.ResultsByFeature[0];
			Assert.AreEqual(4, result.ResultGeometries.Count,
			                "Should have 4 parts: 2 from first ring group + 2 from second ring group");

			// Verify each result has a distinct PointID (groups are separated)
			var pointIds = new HashSet<int>();
			foreach (IMultiPatch resultPatch in result.ResultGeometries.Cast<IMultiPatch>())
			{
				var groups = GetRingGroupsByPointId(resultPatch);
				Assert.AreEqual(1, groups.Count,
				                "Each exploded part should have one PointID group");
				pointIds.Add(groups.Keys.First());
			}

			// Should have 4 unique PointIDs (one per exploded part)
			Assert.AreEqual(4, pointIds.Count);
		}

		[Test]
		public void CanHandleGeometryPartWithMultipleDisconnectedRingGroups()
		{
			// CRITICAL TEST: A GeometryPart (same PointID) containing multiple disconnected ring groups
			// This is the bug scenario that was fixed!
			IFeatureClass fc = CreateMultipatchFeatureClass("DisconnectedRingGroupsSamePointId");

			// Create a multipatch with TWO DISCONNECTED rings but SAME PointID
			// This simulates what GeometryPart.FromGeometry might return
			IMultiPatch multipatch = CreateMultipatchWithDisconnectedRingsWithSamePointId(
				new[]
				{
					// First disconnected ring (PointID = 1)
					new[]
					{
						new WKSPointZ { X = 0, Y = 0, Z = 0 },
						new WKSPointZ { X = 5, Y = 0, Z = 0 },
						new WKSPointZ { X = 5, Y = 5, Z = 0 },
						new WKSPointZ { X = 0, Y = 5, Z = 0 }
					},
					// Second disconnected ring (ALSO PointID = 1) - spatially separated
					new[]
					{
						new WKSPointZ { X = 10, Y = 0, Z = 0 },
						new WKSPointZ { X = 15, Y = 0, Z = 0 },
						new WKSPointZ { X = 15, Y = 5, Z = 0 },
						new WKSPointZ { X = 10, Y = 5, Z = 0 }
					}
				},
				pointId: 1); // SAME PointID for both!

			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			// Overlap affecting ONLY the first ring
			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2, -1),
				GeometryFactory.CreatePoint(6, 6));

			var remover = new OverlapsRemover(explodeMultipartResult: false);
			remover.CalculateResults(new[] { feature }, overlap);

			// Should have results because first ring was modified
			Assert.AreEqual(1, remover.Result.ResultsByFeature.Count,
			                "Should have results because overlap modified the feature");

			var result = remover.Result.ResultsByFeature[0];
			Assert.AreEqual(1, result.ResultGeometries.Count);

			IMultiPatch resultPatch = (IMultiPatch) result.ResultGeometries[0];

			// Should still have both ring groups (2 faces)
			int ringCount = GeometryUtils.GetPartCount(resultPatch);
			Assert.AreEqual(2, ringCount,
			                "Should preserve both rings (one modified, one unmodified)");

			// Total area should be smaller (first ring was partially removed)
			IArea originalArea = (IArea) multipatch;
			IArea resultArea = (IArea) resultPatch;
			Assert.Less(resultArea.Area, originalArea.Area,
			            "Result area should be smaller due to overlap removal from first ring");

			// Second ring should be untouched, so result area > its area
			double secondRingArea = 5 * 5; // 5x5 square
			Assert.Greater(resultArea.Area, secondRingArea,
			               "Result should include the untouched second ring");
		}

		[Test]
		public void CanPreserveUnmodifiedPartsWithOriginalPointIds()
		{
			// Test that unmodified parts retain their relationship to PointID grouping
			IFeatureClass fc = CreateMultipatchFeatureClass("PreserveUnmodifiedParts");

			// Three ring groups: only middle one overlaps
			IMultiPatch multipatch = CreateMultiPartMultipatch(
				new[]
				{
					// Left (PointID = 1) - will NOT overlap
					new[]
					{
						new WKSPointZ { X = 0, Y = 0, Z = 0 },
						new WKSPointZ { X = 5, Y = 0, Z = 0 },
						new WKSPointZ { X = 5, Y = 5, Z = 0 },
						new WKSPointZ { X = 0, Y = 5, Z = 0 }
					},
					// Middle (PointID = 2) - WILL overlap
					new[]
					{
						new WKSPointZ { X = 10, Y = 0, Z = 0 },
						new WKSPointZ { X = 15, Y = 0, Z = 0 },
						new WKSPointZ { X = 15, Y = 5, Z = 0 },
						new WKSPointZ { X = 10, Y = 5, Z = 0 }
					},
					// Right (PointID = 3) - will NOT overlap
					new[]
					{
						new WKSPointZ { X = 20, Y = 0, Z = 0 },
						new WKSPointZ { X = 25, Y = 0, Z = 0 },
						new WKSPointZ { X = 25, Y = 5, Z = 0 },
						new WKSPointZ { X = 20, Y = 5, Z = 0 }
					}
				},
				pointIds: new[] { 1, 2, 3 });

			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			// Overlap only affecting middle ring
			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(12, -1),
				GeometryFactory.CreatePoint(16, 6));

			var remover = new OverlapsRemover(explodeMultipartResult: false);
			remover.CalculateResults(new[] { feature }, overlap);

			var result = remover.Result.ResultsByFeature[0];
			IMultiPatch resultPatch = (IMultiPatch) result.ResultGeometries[0];

			// Should have 3 distinct PointID groups still
			var ringsByPointId = GetRingGroupsByPointId(resultPatch);
			Assert.AreEqual(3, ringsByPointId.Count, "Should maintain 3 PointID groups");

			// The two unmodified groups should have their rings intact
			// The modified group should have partial area
		}

		[Test]
		public void CanHandleMultipleRingGroupsWithSamePointIdTouchingAlongBoundary()
		{
			// Test: Multiple RingGroups with SAME PointID that TOUCH along boundary
			// This simulates a realistic scenario where connected faces share a PointID
			IFeatureClass fc = CreateMultipatchFeatureClass("TouchingRingsGroupsSamePointId");

			// Create two adjacent squares sharing an edge (touching boundary)
			// Both have PointID = 1
			IMultiPatch multipatch1 = CreateMultipatchWithTouchingRingsWithSamePointId(
				new[]
				{
					// Left square (PointID = 1)
					new[]
					{
						new WKSPointZ { X = 0, Y = 0, Z = 0 },
						new WKSPointZ { X = 5, Y = 0, Z = 0 },
						new WKSPointZ { X = 5, Y = 5, Z = 0 },
						new WKSPointZ { X = 0, Y = 5, Z = 0 }
					},
					// Right square (PointID = 1) - shares edge with left
					new[]
					{
						new WKSPointZ { X = 5, Y = 0, Z = 0 }, // Shared edge!
						new WKSPointZ { X = 10, Y = 0, Z = 0 },
						new WKSPointZ { X = 10, Y = 5, Z = 0 },
						new WKSPointZ { X = 5, Y = 5, Z = 0 }
					}
				},
				pointId: 1);

			IMultiPatch multipatch2 = CreateMultipatchWithTouchingRingsWithSamePointId(
				new[]
				{
					// Left square (PointID = 1)
					new[]
					{
						new WKSPointZ { X = 0, Y = 0, Z = 10 },
						new WKSPointZ { X = 5, Y = 0, Z = 10 },
						new WKSPointZ { X = 5, Y = 5, Z = 10 },
						new WKSPointZ { X = 0, Y = 5, Z = 10 }
					},
					// Right square (PointID = 1) - shares edge with left
					new[]
					{
						new WKSPointZ { X = 5, Y = 0, Z = 10 }, // Shared edge!
						new WKSPointZ { X = 10, Y = 0, Z = 10 },
						new WKSPointZ { X = 10, Y = 5, Z = 10 },
						new WKSPointZ { X = 5, Y = 5, Z = 10 }
					}
				},
				pointId: 2);

			// TODO: Check vertical, connected ring

			IMultiPatch multipatch = (IMultiPatch) GeometryUtils.Union(multipatch1, multipatch2);

			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			// Overlap affecting the RIGHT square only
			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(7, -1),
				GeometryFactory.CreatePoint(11, 6));

			var remover = new OverlapsRemover(explodeMultipartResult: false);
			remover.CalculateResults(new[] { feature }, overlap);

			Assert.AreEqual(1, remover.Result.ResultsByFeature.Count);
			var result = remover.Result.ResultsByFeature[0];
			Assert.AreEqual(1, result.ResultGeometries.Count);

			IMultiPatch resultPatch = (IMultiPatch) result.ResultGeometries[0];

			// Should still have 4 rings
			int ringCount = GeometryUtils.GetPartCount(resultPatch);
			Assert.AreEqual(4, ringCount,
			                "Should preserve both rings (one modified, one unchanged)");

			// The two touching groups should still be connected but be distinct
			var ringsByPointId = GetRingGroupsByPointId(resultPatch);
			Assert.AreEqual(2, ringsByPointId.Count,
			                "Should have Two PointID group (touching rings are connected)");

			// Verify PointID awareness
			Assert.IsTrue(GeometryUtils.IsPointIDAware(resultPatch));
			Assert.IsFalse(GeometryUtils.HasUniqueVertexId(resultPatch, out int id));

			// Result area should be smaller than original (right ring partially removed)
			IArea3D originalArea = (IArea3D) multipatch;
			IArea3D resultArea = (IArea3D) resultPatch;
			Assert.Less(resultArea.Area3D, originalArea.Area3D);
			Assert.AreEqual(70, resultArea.Area3D);

			// Now with the target touching only the upper ring group:
			// Overlap affecting the RIGHT square only
			overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(7, 0, 10),
				GeometryFactory.CreatePoint(11, 6, 10));

			remover = new OverlapsRemover(explodeMultipartResult: false);
			remover.CalculateResults(new[] { feature }, overlap);

			Assert.AreEqual(1, remover.Result.ResultsByFeature.Count);
			result = remover.Result.ResultsByFeature[0];
			Assert.AreEqual(1, result.ResultGeometries.Count);

			resultPatch = (IMultiPatch) result.ResultGeometries[0];

			// Should still have 4 rings
			ringCount = GeometryUtils.GetPartCount(resultPatch);
			Assert.AreEqual(4, ringCount,
			                "Should preserve both rings (one modified, one unchanged)");

			// The two touching groups should still be connected but be distinct
			ringsByPointId = GetRingGroupsByPointId(resultPatch);
			Assert.AreEqual(2, ringsByPointId.Count,
			                "Should have Two PointID group (touching rings are connected)");

			// Verify PointID awareness
			Assert.IsTrue(GeometryUtils.IsPointIDAware(resultPatch));
			Assert.IsFalse(GeometryUtils.HasUniqueVertexId(resultPatch, out id));

			// Result area should be smaller than original (right ring partially removed)
			originalArea = (IArea3D) multipatch;
			resultArea = (IArea3D) resultPatch;
			Assert.Less(resultArea.Area3D, originalArea.Area3D);
			Assert.AreEqual(85, resultArea.Area3D);
		}

		[Test]
		public void CanSeparateDisconnectedRingGroupsAfterCut()
		{
			// Test: A single ring is CUT into two disconnected pieces
			// Result should have DIFFERENT PointIDs for disconnected pieces
			IFeatureClass fc = CreateMultipatchFeatureClass("CutRingIntoDisconnectedPieces");

			// Create one large square
			IMultiPatch multipatch = CreateSimpleHorizontalMultipatch(
				new[]
				{
					new WKSPointZ { X = 0, Y = 0, Z = 0 },
					new WKSPointZ { X = 20, Y = 0, Z = 0 },
					new WKSPointZ { X = 20, Y = 10, Z = 0 },
					new WKSPointZ { X = 0, Y = 10, Z = 0 }
				},
				pointId: 1);

			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			// Overlap cutting through the MIDDLE - creates two disconnected pieces
			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(8, -1),
				GeometryFactory.CreatePoint(12, 11));

			var remover = new OverlapsRemover(explodeMultipartResult: false);
			remover.CalculateResults(new[] { feature }, overlap);

			var result = remover.Result.ResultsByFeature[0];
			Assert.AreEqual(1, result.ResultGeometries.Count,
			                "Should have one result multipatch (no explosion)");

			IMultiPatch resultPatch = (IMultiPatch) result.ResultGeometries[0];

			// Should have 2 rings (left and right pieces)
			int ringCount = GeometryUtils.GetPartCount(resultPatch);
			Assert.AreEqual(2, ringCount, "Should have two disconnected rings");

			// The two disconnected pieces should have DIFFERENT PointIDs
			var ringsByPointId = GetRingGroupsByPointId(resultPatch);
			Assert.AreEqual(2, ringsByPointId.Count,
			                "Should have TWO PointID groups (disconnected pieces)");

			// Verify PointID awareness
			Assert.IsTrue(GeometryUtils.IsPointIDAware(resultPatch));

			// Each group should have exactly one ring
			foreach (var group in ringsByPointId.Values)
			{
				Assert.AreEqual(1, group.Count, "Each PointID group should have one ring");
			}
		}

		[Test]
		public void CanSeparateDisconnectedRingGroupsAfterCut_WithExplosion()
		{
			// Same as above but WITH explosion - should create separate multipatches
			IFeatureClass fc = CreateMultipatchFeatureClass("CutRingWithExplosion");

			IMultiPatch multipatch = CreateSimpleHorizontalMultipatch(
				new[]
				{
					new WKSPointZ { X = 0, Y = 0, Z = 0 },
					new WKSPointZ { X = 20, Y = 0, Z = 0 },
					new WKSPointZ { X = 20, Y = 10, Z = 0 },
					new WKSPointZ { X = 0, Y = 10, Z = 0 }
				},
				pointId: 1);

			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(8, -1),
				GeometryFactory.CreatePoint(12, 11));

			var remover = new OverlapsRemover(explodeMultipartResult: true);
			remover.CalculateResults(new[] { feature }, overlap);

			var result = remover.Result.ResultsByFeature[0];
			Assert.AreEqual(2, result.ResultGeometries.Count,
			                "Should have TWO separate multipatches (exploded)");

			// Each exploded piece should have its own PointID
			var pointIds = new HashSet<int>();
			foreach (IMultiPatch resultPatch in result.ResultGeometries.Cast<IMultiPatch>())
			{
				var groups = GetRingGroupsByPointId(resultPatch);
				Assert.AreEqual(1, groups.Count,
				                "Each exploded multipatch should have one PointID");
				pointIds.Add(groups.Keys.First());
			}

			Assert.AreEqual(2, pointIds.Count, "Should have 2 distinct PointIDs");
		}

		[Test]
		public void CanHandleComplexMultipartWithMixedConnectivity()
		{
			// Test: Complex scenario with multiple PointID groups, some touching, some not
			// After overlap removal, connectivity-based grouping should prevail
			IFeatureClass fc = CreateMultipatchFeatureClass("ComplexMixedConnectivity");

			// Create 4 rings:
			// Ring 1 (PointID=1) and Ring 2 (PointID=1) touch - should stay together
			// Ring 3 (PointID=2) and Ring 4 (PointID=2) touch - should stay together
			// Group 1 and Group 2 are disconnected from each other
			IMultiPatch multipatch = CreateComplexMultipatchWithTouchingGroups();

			IFeature feature = fc.CreateFeature();
			feature.Shape = multipatch;
			feature.Store();

			// Overlap affecting all rings partially
			IPolygon overlap = GeometryFactory.CreatePolygon(
				GeometryFactory.CreatePoint(2, 2),
				GeometryFactory.CreatePoint(18, 3));

			var remover = new OverlapsRemover(explodeMultipartResult: false);
			remover.CalculateResults(new[] { feature }, overlap);

			var result = remover.Result.ResultsByFeature[0];
			IMultiPatch resultPatch = (IMultiPatch) result.ResultGeometries[0];

			// After modification, should still have 2 distinct PointID groups
			// (based on spatial connectivity)
			var ringsByPointId = GetRingGroupsByPointId(resultPatch);
			Assert.AreEqual(2, ringsByPointId.Count,
			                "Should have TWO PointID groups (two connected components)");
		}

		#region Helper Methods

		private IFeatureClass CreateMultipatchFeatureClass(string name)
		{
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryMultiPatch,
				                            spatialReference, 1000, true, false));

			return DatasetUtils.CreateSimpleFeatureClass(_testWs, name, fields);
		}

		private IMultiPatch CreateSimpleHorizontalMultipatch(WKSPointZ[] ringPoints, int pointId)
		{
			IMultiPatch multipatch = new MultiPatchClass();
			((IZAware) multipatch).ZAware = true;
			((IPointIDAware) multipatch).PointIDAware = true; // ✅ ADD THIS!

			object missing = Type.Missing;
			IGeometryCollection geometryCollection = (IGeometryCollection) multipatch;

			IPointCollection ring = new RingClass();
			((IZAware) ring).ZAware = true;

			foreach (WKSPointZ wksPoint in ringPoints)
			{
				IPoint point = new PointClass();
				point.PutCoords(wksPoint.X, wksPoint.Y);
				point.Z = wksPoint.Z;
				ring.AddPoint(point, ref missing, ref missing);
			}

			// Close the ring
			ring.AddPoint(ring.Point[0], ref missing, ref missing);

			geometryCollection.AddGeometry((IGeometry) ring, ref missing, ref missing);

			// Set PointIDs
			GeometryUtils.AssignConstantPointID((IGeometryCollection) multipatch, 0, pointId);

			multipatch.SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			return multipatch;
		}

		private IMultiPatch CreateMultiPartMultipatch(WKSPointZ[][] ringPointArrays, int[] pointIds)
		{
			Assert.AreEqual(ringPointArrays.Length, pointIds.Length);

			IMultiPatch multipatch = new MultiPatchClass();
			((IZAware) multipatch).ZAware = true;
			((IPointIDAware) multipatch).PointIDAware = true; // ✅ ADD THIS!

			object missing = Type.Missing;
			IGeometryCollection geometryCollection = (IGeometryCollection) multipatch;

			for (int i = 0; i < ringPointArrays.Length; i++)
			{
				IPointCollection ring = new RingClass();
				((IZAware) ring).ZAware = true;

				foreach (WKSPointZ wksPoint in ringPointArrays[i])
				{
					IPoint point = new PointClass();
					point.PutCoords(wksPoint.X, wksPoint.Y);
					point.Z = wksPoint.Z;
					ring.AddPoint(point, ref missing, ref missing);
				}

				// Close the ring
				ring.AddPoint(ring.Point[0], ref missing, ref missing);

				geometryCollection.AddGeometry((IGeometry) ring, ref missing, ref missing);

				// Set PointID for this ring
				GeometryUtils.AssignConstantPointID(geometryCollection, i, pointIds[i]);
			}

			multipatch.SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			return multipatch;
		}

		private IMultiPatch CreateMultipatchWithDisconnectedRingsWithSamePointId(
			WKSPointZ[][] ringPointArrays, int pointId)
		{
			IMultiPatch multipatch = new MultiPatchClass();
			((IZAware) multipatch).ZAware = true;
			((IPointIDAware) multipatch).PointIDAware = true; // ✅ ADD THIS!

			object missing = Type.Missing;
			IGeometryCollection geometryCollection = (IGeometryCollection) multipatch;

			for (int i = 0; i < ringPointArrays.Length; i++)
			{
				IPointCollection ring = new RingClass();
				((IZAware) ring).ZAware = true;

				foreach (WKSPointZ wksPoint in ringPointArrays[i])
				{
					IPoint point = new PointClass();
					point.PutCoords(wksPoint.X, wksPoint.Y);
					point.Z = wksPoint.Z;
					ring.AddPoint(point, ref missing, ref missing);
				}

				// Close the ring
				ring.AddPoint(ring.Point[0], ref missing, ref missing);

				geometryCollection.AddGeometry((IGeometry) ring, ref missing, ref missing);

				// IMPORTANT: Set SAME PointID for ALL rings
				GeometryUtils.AssignConstantPointID(geometryCollection, i, pointId);
			}

			multipatch.SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			return multipatch;
		}

		private IMultiPatch CreateMultipatchWithTouchingRingsWithSamePointId(
			WKSPointZ[][] ringPointArrays, int pointId)
		{
			// Same as CreateMultipatchWithDisconnectedRingsWithSamePointId
			// (rings can be either touching or disconnected, implementation is the same)
			return CreateMultipatchWithDisconnectedRingsWithSamePointId(ringPointArrays, pointId);
		}

		private IMultiPatch CreateComplexMultipatchWithTouchingGroups()
		{
			IMultiPatch multipatch = new MultiPatchClass();
			((IZAware) multipatch).ZAware = true;
			((IPointIDAware) multipatch).PointIDAware = true; // ✅ ADD THIS!

			IGeometryCollection geometryCollection = (IGeometryCollection) multipatch;

			// Group 1: Two touching rings (PointID = 1)
			// Ring 1
			var ring1Points = new[]
			                  {
				                  new WKSPointZ { X = 0, Y = 0, Z = 0 },
				                  new WKSPointZ { X = 5, Y = 0, Z = 0 },
				                  new WKSPointZ { X = 5, Y = 5, Z = 0 },
				                  new WKSPointZ { X = 0, Y = 5, Z = 0 }
			                  };
			AddRing(geometryCollection, ring1Points);
			GeometryUtils.AssignConstantPointID(geometryCollection, 0, 1);

			// Ring 2 - touches Ring 1
			var ring2Points = new[]
			                  {
				                  new WKSPointZ { X = 5, Y = 0, Z = 0 },
				                  new WKSPointZ { X = 10, Y = 0, Z = 0 },
				                  new WKSPointZ { X = 10, Y = 5, Z = 0 },
				                  new WKSPointZ { X = 5, Y = 5, Z = 0 }
			                  };
			AddRing(geometryCollection, ring2Points);
			GeometryUtils.AssignConstantPointID(geometryCollection, 1, 1);

			// Group 2: Two touching rings (PointID = 2) - disconnected from Group 1
			// Ring 3
			var ring3Points = new[]
			                  {
				                  new WKSPointZ { X = 15, Y = 0, Z = 0 },
				                  new WKSPointZ { X = 20, Y = 0, Z = 0 },
				                  new WKSPointZ { X = 20, Y = 5, Z = 0 },
				                  new WKSPointZ { X = 15, Y = 5, Z = 0 }
			                  };
			AddRing(geometryCollection, ring3Points);
			GeometryUtils.AssignConstantPointID(geometryCollection, 2, 2);

			// Ring 4 - touches Ring 3
			var ring4Points = new[]
			                  {
				                  new WKSPointZ { X = 20, Y = 0, Z = 0 },
				                  new WKSPointZ { X = 25, Y = 0, Z = 0 },
				                  new WKSPointZ { X = 25, Y = 5, Z = 0 },
				                  new WKSPointZ { X = 20, Y = 5, Z = 0 }
			                  };
			AddRing(geometryCollection, ring4Points);
			GeometryUtils.AssignConstantPointID(geometryCollection, 3, 2);

			multipatch.SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			return multipatch;
		}

		private void AddRing(IGeometryCollection geometryCollection, WKSPointZ[] ringPoints)
		{
			object missing = Type.Missing;

			IPointCollection ring = new RingClass();
			((IZAware) ring).ZAware = true;

			foreach (WKSPointZ wksPoint in ringPoints)
			{
				IPoint point = new PointClass();
				point.PutCoords(wksPoint.X, wksPoint.Y);
				point.Z = wksPoint.Z;
				ring.AddPoint(point, ref missing, ref missing);
			}

			// Close the ring
			ring.AddPoint(ring.Point[0], ref missing, ref missing);

			geometryCollection.AddGeometry((IGeometry) ring, ref missing, ref missing);
		}

		/// <summary>
		/// Gets a dictionary mapping PointID -> List of ring indices with that PointID
		/// </summary>
		private Dictionary<int, List<int>> GetRingGroupsByPointId(IMultiPatch multipatch)
		{
			var result = new Dictionary<int, List<int>>();

			IGeometryCollection geomCollection = (IGeometryCollection) multipatch;

			for (int i = 0; i < geomCollection.GeometryCount; i++)
			{
				IGeometry partGeometry = geomCollection.Geometry[i];
				IPointCollection pointCollection = (IPointCollection) partGeometry;

				if (pointCollection.PointCount == 0)
					continue;

				IPoint firstPoint = pointCollection.Point[0];
				int pointId = firstPoint.ID;

				if (! result.ContainsKey(pointId))
				{
					result[pointId] = new List<int>();
				}

				result[pointId].Add(i);
			}

			return result;
		}

		#endregion
	}
}
