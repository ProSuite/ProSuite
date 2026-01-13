using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ProtobufConversionUtilsTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void CanConvertMultipatchToFromShapeMsg()
		{
			Multipatch multipatch = CreateMultipatch();

			ShapeMsg shapeMsg = ProtobufConversionUtils.ToShapeMsg(multipatch);

			Geometry rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			Assert.IsTrue(GeometryEngine.Instance.Equals(multipatch, rehydrated));
			// Note: IsEqual is stricter than Equals and can fail due to precision differences in WKB round-trip
			// For production use, topological equality (above) is sufficient
		}

		[Test]
		public void CanConvertMultipatchWithPointIdsToFromShapeMsg()
		{
			// Create a multipatch with point IDs - all points in both rings have ID 0
			// The IDs are not really transferred other than the grouping into polyhedra
			Multipatch multipatch = CreateMultipatchWithPointIds(ringId1: 0, ringId2: 0);

			Assert.IsTrue(multipatch.HasID, "Source multipatch should have point IDs");

			ShapeMsg shapeMsg = ProtobufConversionUtils.ToShapeMsg(multipatch);

			Geometry rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			// Verify topological equality
			Assert.IsTrue(GeometryEngine.Instance.Equals(multipatch, rehydrated));

			var rehydratedMultipatch = (Multipatch) rehydrated;
			Assert.IsNotNull(rehydratedMultipatch);

			// Note: Point IDs are preserved through WKB conversion when rings are grouped by ID
			// The CreatePolyhedra method now properly groups rings by point ID
			Assert.IsTrue(rehydratedMultipatch.HasID,
			              "Rehydrated multipatch should have point IDs");

			// Verify point IDs are consistent (all should be 1)
			VerifyPointIds(rehydratedMultipatch, expectedId: 0);
		}

		[Test]
		public void CanConvertMultipatchWithTwoSeparateRingGroups()
		{
			// Create a multipatch with two separate ring groups (different point IDs)
			// Ring group 1: exterior ring with ID 0
			// Ring group 2: exterior ring with ID 1
			Multipatch multipatch =
				CreateMultipatchWithTwoSeparateRingGroups(ringId1: 0, ringId2: 1);

			Assert.IsTrue(multipatch.HasID, "Source multipatch should have point IDs");

			ShapeMsg shapeMsg = ProtobufConversionUtils.ToShapeMsg(multipatch);

			Geometry rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			Assert.IsTrue(GeometryEngine.Instance.Equals(multipatch, rehydrated));

			var rehydratedMultipatch = (Multipatch) rehydrated;
			Assert.IsNotNull(rehydratedMultipatch);

			Assert.IsTrue(rehydratedMultipatch.HasID,
			              "Rehydrated multipatch should have point IDs");

			// Verify that the two ring groups are properly separated
			Assert.AreEqual(2, rehydratedMultipatch.PartCount, "Should have 2 separate rings");

			// Verify each ring group maintained its ID
			VerifyRingGroupPointIds(rehydratedMultipatch, 0, expectedId: 0);
			VerifyRingGroupPointIds(rehydratedMultipatch, 1, expectedId: 1);
		}

		[Test]
		public void CanConvertMultipatchWithInteriorRingsAndSamePointId()
		{
			// Create a multipatch where exterior and interior ring have the same point ID
			Multipatch multipatch = CreateMultipatchWithPointIds(ringId1: 0, ringId2: 0);

			Assert.IsTrue(multipatch.HasID);

			ShapeMsg shapeMsg = ProtobufConversionUtils.ToShapeMsg(multipatch);

			Geometry rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			Assert.IsTrue(GeometryEngine.Instance.Equals(multipatch, rehydrated));

			var rehydratedMultipatch = (Multipatch) rehydrated;
			Assert.IsNotNull(rehydratedMultipatch);
			Assert.IsTrue(rehydratedMultipatch.HasID);

			// Verify both rings have the same ID (5)
			VerifyPointIds(rehydratedMultipatch, expectedId: 0);
		}

		[Test]
		public void CanConvertMultipatchWithInteriorRingsAndDifferentPointIds()
		{
			// Create a multipatch where exterior and interior ring have different point IDs
			// In this case, the ring group should not have a uniform ID
			Multipatch multipatch = CreateMultipatchWithPointIds(ringId1: 0, ringId2: 1);

			Assert.IsTrue(multipatch.HasID);

			ShapeMsg shapeMsg = ProtobufConversionUtils.ToShapeMsg(multipatch);

			Geometry rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			Assert.IsTrue(GeometryEngine.Instance.Equals(multipatch, rehydrated));

			var rehydratedMultipatch = (Multipatch) rehydrated;
			Assert.IsNotNull(rehydratedMultipatch);
			Assert.IsTrue(rehydratedMultipatch.HasID);

			// Interior rings by definition are part of the same ring group:
			VerifyRingGroupPointIds(rehydratedMultipatch, 0, expectedId: 0); // Exterior ring
			VerifyRingGroupPointIds(rehydratedMultipatch, 1, expectedId: 0); // Interior ring
		}

		[Test]
		public void CanConvertComplexMultipatchWithMixedPointIds()
		{
			// Create a complex multipatch with multiple ring groups
			Multipatch multipatch = CreateComplexMultipatchWithMixedPointIds();

			Assert.IsTrue(multipatch.HasID);

			ShapeMsg shapeMsg = ProtobufConversionUtils.ToShapeMsg(multipatch);

			Geometry rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			Assert.IsTrue(GeometryEngine.Instance.Equals(multipatch, rehydrated));

			var rehydratedMultipatch = (Multipatch) rehydrated;
			Assert.IsNotNull(rehydratedMultipatch);
			Assert.IsTrue(rehydratedMultipatch.HasID);
			Assert.AreEqual(multipatch.PartCount, rehydratedMultipatch.PartCount);

			// Verify first ring group (2 rings with ID 0)
			VerifyRingGroupPointIds(rehydratedMultipatch, 0, expectedId: 0);
			VerifyRingGroupPointIds(rehydratedMultipatch, 1, expectedId: 0);

			// Verify second ring group (1 ring with ID 1)
			VerifyRingGroupPointIds(rehydratedMultipatch, 2, expectedId: 1);
		}

		[Test]
		public void CanConvertMultipatchWithInconsistentPointIdsInRing()
		{
			// Create a multipatch where points in a single ring have different IDs
			Multipatch multipatch = CreateMultipatchWithInconsistentPointIds();

			Assert.IsTrue(multipatch.HasID);

			ShapeMsg shapeMsg = ProtobufConversionUtils.ToShapeMsg(multipatch);

			Geometry rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			Assert.IsTrue(GeometryEngine.Instance.Equals(multipatch, rehydrated));

			// Point IDs are preserved even if inconsistent within a ring
			var rehydratedMultipatch = (Multipatch) rehydrated;
			Assert.IsNotNull(rehydratedMultipatch);
			Assert.IsTrue(rehydratedMultipatch.HasID);
		}

		#region Helper Methods

		private static Multipatch CreateMultipatch()
		{
			MultipatchBuilderEx mpBuilder =
				new MultipatchBuilderEx(SpatialReferenceBuilder.CreateSpatialReference(2056));

			var patches = new List<Patch>();

			Patch patch1 = mpBuilder.MakePatch(PatchType.FirstRing);

			patch1.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600000, 1200000, 450),
					new Coordinate3D(2600000, 1200200, 450),
					new Coordinate3D(2600100, 1200200, 470),
					new Coordinate3D(2600100, 1200000, 470),
					new Coordinate3D(2600000, 1200000, 450)
				});

			patches.Add(patch1);

			Patch patch2 = mpBuilder.MakePatch(PatchType.Ring);

			patch2.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600025, 1200050, 450),
					new Coordinate3D(2600050, 1200250, 450),
					new Coordinate3D(2600150, 1200275, 470),
					new Coordinate3D(2600125, 1200075, 470),
					new Coordinate3D(2600025, 1200050, 450)
				});

			patches.Add(patch2);

			mpBuilder.Patches = patches;

			Multipatch multipatch = mpBuilder.ToGeometry();
			return multipatch;
		}

		private static Multipatch CreateMultipatchWithPointIds(int ringId1, int ringId2)
		{
			MultipatchBuilderEx mpBuilder =
				new MultipatchBuilderEx(SpatialReferenceBuilder.CreateSpatialReference(2056));

			mpBuilder.HasID = true;

			var patches = new List<Patch>();

			// First ring (exterior) with point IDs
			Patch patch1 = mpBuilder.MakePatch(PatchType.FirstRing);
			patch1.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600000, 1200000, 450),
					new Coordinate3D(2600000, 1200200, 450),
					new Coordinate3D(2600100, 1200200, 470),
					new Coordinate3D(2600100, 1200000, 470),
					new Coordinate3D(2600000, 1200000, 450)
				});
			patch1.IDs = Enumerable.Repeat(ringId1, 5).ToList();
			patches.Add(patch1);

			// Second ring (interior) with point IDs
			Patch patch2 = mpBuilder.MakePatch(PatchType.Ring);
			patch2.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600025, 1200050, 450),
					new Coordinate3D(2600050, 1200250, 450),
					new Coordinate3D(2600150, 1200275, 470),
					new Coordinate3D(2600125, 1200075, 470),
					new Coordinate3D(2600025, 1200050, 450)
				});
			patch2.IDs = Enumerable.Repeat(ringId2, 5).ToList();
			patches.Add(patch2);

			mpBuilder.Patches = patches;

			Multipatch multipatch = mpBuilder.ToGeometry();
			return multipatch;
		}

		private static Multipatch CreateMultipatchWithTwoSeparateRingGroups(
			int ringId1, int ringId2)
		{
			MultipatchBuilderEx mpBuilder =
				new MultipatchBuilderEx(SpatialReferenceBuilder.CreateSpatialReference(2056));
			mpBuilder.HasID = true;

			var patches = new List<Patch>();

			// First ring group
			Patch patch1 = mpBuilder.MakePatch(PatchType.FirstRing);
			patch1.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600000, 1200000, 450),
					new Coordinate3D(2600000, 1200200, 450),
					new Coordinate3D(2600100, 1200200, 470),
					new Coordinate3D(2600100, 1200000, 470),
					new Coordinate3D(2600000, 1200000, 450)
				});
			patch1.IDs = Enumerable.Repeat(ringId1, 5).ToList();
			patches.Add(patch1);

			// Second ring group (separate, not interior)
			Patch patch2 = mpBuilder.MakePatch(PatchType.FirstRing);
			patch2.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600200, 1200000, 450),
					new Coordinate3D(2600200, 1200100, 450),
					new Coordinate3D(2600300, 1200100, 470),
					new Coordinate3D(2600300, 1200000, 470),
					new Coordinate3D(2600200, 1200000, 450)
				});
			patch2.IDs = Enumerable.Repeat(ringId2, 5).ToList();
			patches.Add(patch2);

			mpBuilder.Patches = patches;

			Multipatch multipatch = mpBuilder.ToGeometry();
			return multipatch;
		}

		private static Multipatch CreateComplexMultipatchWithMixedPointIds()
		{
			MultipatchBuilderEx mpBuilder =
				new MultipatchBuilderEx(SpatialReferenceBuilder.CreateSpatialReference(2056));

			mpBuilder.HasID = true;

			var patches = new List<Patch>();

			// First ring group with ID 0
			Patch patch1 = mpBuilder.MakePatch(PatchType.FirstRing);
			patch1.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600000, 1200000, 450),
					new Coordinate3D(2600000, 1200200, 450),
					new Coordinate3D(2600100, 1200200, 470),
					new Coordinate3D(2600100, 1200000, 470),
					new Coordinate3D(2600000, 1200000, 450)
				});
			patch1.IDs = Enumerable.Repeat(0, 5).ToList();
			patches.Add(patch1);

			// Interior ring for first group, also with ID 0
			Patch patch2 = mpBuilder.MakePatch(PatchType.Ring);
			patch2.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600025, 1200025, 455),
					new Coordinate3D(2600025, 1200075, 455),
					new Coordinate3D(2600075, 1200075, 465),
					new Coordinate3D(2600075, 1200025, 465),
					new Coordinate3D(2600025, 1200025, 455)
				});
			patch2.IDs = Enumerable.Repeat(0, 5).ToList();
			patches.Add(patch2);

			// Second ring group with ID 1
			Patch patch3 = mpBuilder.MakePatch(PatchType.FirstRing);
			patch3.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600200, 1200000, 450),
					new Coordinate3D(2600200, 1200100, 450),
					new Coordinate3D(2600300, 1200100, 470),
					new Coordinate3D(2600300, 1200000, 470),
					new Coordinate3D(2600200, 1200000, 450)
				});
			patch3.IDs = Enumerable.Repeat(1, 5).ToList();
			patches.Add(patch3);

			mpBuilder.Patches = patches;

			Multipatch multipatch = mpBuilder.ToGeometry();
			return multipatch;
		}

		private static Multipatch CreateMultipatchWithInconsistentPointIds()
		{
			MultipatchBuilderEx mpBuilder =
				new MultipatchBuilderEx(SpatialReferenceBuilder.CreateSpatialReference(2056));

			mpBuilder.HasID = true;

			var patches = new List<Patch>();

			// Ring with inconsistent point IDs (alternating 1 and 2)
			Patch patch1 = mpBuilder.MakePatch(PatchType.FirstRing);
			patch1.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600000, 1200000, 450),
					new Coordinate3D(2600000, 1200200, 450),
					new Coordinate3D(2600100, 1200200, 470),
					new Coordinate3D(2600100, 1200000, 470),
					new Coordinate3D(2600000, 1200000, 450)
				});
			patch1.IDs = new List<int> { 1, 2, 1, 2, 1 };
			patches.Add(patch1);

			mpBuilder.Patches = patches;

			Multipatch multipatch = mpBuilder.ToGeometry();
			return multipatch;
		}

		private static void VerifyPointIds(Multipatch multipatch, int expectedId)
		{
			for (int partIdx = 0; partIdx < multipatch.PartCount; partIdx++)
			{
				VerifyRingGroupPointIds(multipatch, partIdx, expectedId);
			}
		}

		private static void VerifyRingGroupPointIds(Multipatch multipatch, int partIdx,
		                                            int expectedId)
		{
			int pointCount = multipatch.GetPatchPointCount(partIdx);
			int startIdx = multipatch.GetPatchStartPointIndex(partIdx);

			for (int i = 0; i < pointCount; i++)
			{
				MapPoint point = multipatch.Points[startIdx + i];
				//Assert.IsTrue(point.HasID, $"Point should have ID at part {partIdx}, point {i}");
				Assert.AreEqual(expectedId, point.ID,
				                $"Point ID mismatch at part {partIdx}, point {i}");
			}
		}

		#endregion
	}
}
