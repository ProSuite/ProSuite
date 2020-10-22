using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	[Ignore("Used as repro cases")]
	public class GeometryIssuesReproTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void Repro_QueryClippedModifiesInputPolygon()
		{
			string polygonBeforeXml = TestUtils.GetGeometryTestDataPath("queryclipped_before.xml");
			string clipEnvelopeXml = TestUtils.GetGeometryTestDataPath("queryclipped_envelope.xml");

			var originalPolygon = (IPolygon) GeometryUtils.FromXmlFile(polygonBeforeXml);
			var clipperEnvelope = (IEnvelope) GeometryUtils.FromXmlFile(clipEnvelopeXml);

			// Note: A corner (north-east) of the clipper envelope touches the boundary of input polygon.
			//       We assume that this (rare but allowed) situation causes QueryClipped() to fail.
			//       The failure by itself is not a problem for us, as long as we can catch it as an 
			//       exception (and fall back to ITopologicalOperator.Intersect(), which is 10x slower,
			//       but produces the correct result). However in a certain percentage of these cases, 
			//       NO exception is thrown, however th INPUT IS MODIFIED. This is a very serious violation  
			//       of the QueryClipped() contract, and causes data loss in one of our use cases.
			//
			//       The actual percentage of calls resulting in modified input varies. With 1000 
			//       repetitions (callCount constant) we've seen percentages between 0% and almost 100%.

			var exceptionCount = 0;
			var modifiedInputCount = 0;
			const int callCount = 1000;

			for (var i = 0; i < callCount; i++)
			{
				var inputPolygon = (IPolygon) ((IClone) originalPolygon).Clone();

				var failed = false;
				try
				{
					IPolygon clippedGeometry = new PolygonClass();
					((ITopologicalOperator) inputPolygon).QueryClipped(
						clipperEnvelope, clippedGeometry);

					// alternative using QueryClippedDense() seems to result in 100% exceptions, 0% modified input
					// -> this would be a valid workaround for us, if 0% modified input can be confirmed based on code review

					// ((ITopologicalOperator)inputPolygon).QueryClippedDense(clipperEnvelope, double.MaxValue, clippedGeometry);
				}
				catch (Exception)
				{
					exceptionCount++;
					failed = true;
				}

				if (! failed &&
				    ! ((IClone) originalPolygon).IsEqual((IClone) inputPolygon))
				{
					modifiedInputCount++;
				}
			}

			Console.WriteLine($@"{exceptionCount} calls resulting in exception");
			Console.WriteLine($@"{modifiedInputCount} calls resulting in modified input");

			Assert.AreEqual(0, modifiedInputCount, "Input modification issue reproduced");
		}

		[Test]
		public void TestQueryClippedDoesNotModifyInputPolyline()
		{
			// Succeeds with 10.6.1

			string polygonBeforeXml = TestUtils.GetGeometryTestDataPath("queryclipped_before.xml");
			string clipEnvelopeXml = TestUtils.GetGeometryTestDataPath("queryclipped_envelope.xml");

			var originalPolygon = (IPolygon) GeometryUtils.FromXmlFile(polygonBeforeXml);
			var clipperEnvelope = (IEnvelope) GeometryUtils.FromXmlFile(clipEnvelopeXml);
			var originalPolyline =
				(IPolyline) ((ITopologicalOperator) originalPolygon).Boundary;

			// Note: A corner (north-east) of the clipper envelope touches the boundary of input polygon.
			//       We assume that this (rare but allowed) situation causes QueryClipped() to fail.
			//       The failure by itself is not a problem for us, as long as we can catch it as an 
			//       exception (and fall back to ITopologicalOperator.Intersect(), which is 10x slower,
			//       but produces the correct result). However in a certain percentage of these cases, 
			//       NO exception is thrown, however th INPUT IS MODIFIED. This is a very serious violation  
			//       of the QueryClipped() contract, and causes data loss in one of our use cases.
			//
			//       The actual percentage of calls resulting in modified input varies. With 1000 
			//       repetitions (callCount constant) we've seen percentages between 0% and almost 100%.

			var exceptionCount = 0;
			var modifiedInputCount = 0;
			const int callCount = 1000;

			for (var i = 0; i < callCount; i++)
			{
				var inputPolyline = (IPolyline) ((IClone) originalPolyline).Clone();

				var failed = false;
				try
				{
					IPolyline clippedGeometry = new PolylineClass();
					((ITopologicalOperator) inputPolyline).QueryClipped(
						clipperEnvelope, clippedGeometry);
				}
				catch (Exception)
				{
					exceptionCount++;
					failed = true;
				}

				if (! failed &&
				    ! ((IClone) originalPolyline).IsEqual((IClone) inputPolyline))
				{
					modifiedInputCount++;
				}
			}

			Console.WriteLine($@"{exceptionCount} calls resulting in exception");
			Console.WriteLine($@"{modifiedInputCount} calls resulting in modified input");

			Assert.AreEqual(0, exceptionCount);
			Assert.AreEqual(0, modifiedInputCount, "Input modification issue reproduced");
		}

		[Test]
		public void LearningTestClipResult()
		{
			string polygonBeforeXml = TestUtils.GetGeometryTestDataPath("queryclipped_before.xml");
			string clipEnvelopeXml = TestUtils.GetGeometryTestDataPath("queryclipped_envelope.xml");

			var polygonBefore = (IPolygon) GeometryUtils.FromXmlFile(polygonBeforeXml);
			var clipEnvelope = (IEnvelope) GeometryUtils.FromXmlFile(clipEnvelopeXml);

			IPolygon expected =
				GeometryUtils.GetClippedPolygon(polygonBefore, clipEnvelope);

			var clipResult = (IPolygon) ((IClone) polygonBefore).Clone();
			((ITopologicalOperator) clipResult).Clip(clipEnvelope);

			Assert.True(GeometryUtils.AreEqual(expected, clipResult));
			Assert.False(clipResult.IsEmpty);
		}

		[Test]
		public void LearningTestQueryClippedDenseEquivalence()
		{
			string polygonBeforeXml = TestUtils.GetGeometryTestDataPath("queryclipped_before.xml");
			string clipEnvelopeXml = TestUtils.GetGeometryTestDataPath("queryclipped_envelope.xml");

			const int repetition = 100;
			for (var i = 0; i < repetition; i++)
			{
				var inputPolygon = (IPolygon) GeometryUtils.FromXmlFile(polygonBeforeXml);
				var clipEnvelope = (IEnvelope) GeometryUtils.FromXmlFile(clipEnvelopeXml);

				clipEnvelope.Expand(1, 1, false); // results are correct for this envelope

				IPolygon resultQueryClipped = new PolygonClass();
				IPolygon resultQueryClippedDense = new PolygonClass();

				var topoOp = (ITopologicalOperator) inputPolygon;
				topoOp.QueryClipped(clipEnvelope, resultQueryClipped);
				topoOp.QueryClippedDense(clipEnvelope, double.MaxValue,
				                         resultQueryClippedDense);

				Assert.False(resultQueryClipped.IsEmpty);
				Assert.False(resultQueryClippedDense.IsEmpty);
				Assert.True(
					GeometryUtils.AreEqual(resultQueryClipped, resultQueryClippedDense));
			}
		}

		[Test]
		public void LearningTestClipMethodPerformance()
		{
			string polygonBeforeXml = TestUtils.GetGeometryTestDataPath("queryclipped_before.xml");
			string clipEnvelopeXml = TestUtils.GetGeometryTestDataPath("queryclipped_envelope.xml");

			var polygonBefore = (IPolygon) GeometryUtils.FromXmlFile(polygonBeforeXml);
			var clipEnvelope = (IEnvelope) GeometryUtils.FromXmlFile(clipEnvelopeXml);

			clipEnvelope.Expand(1, 1, false); // results are correct for this envelope

			MeasureClipMethodPerformance(polygonBefore, clipEnvelope);
		}

		private static void MeasureClipMethodPerformance([NotNull] IPolygon polygonBefore,
		                                                 [NotNull] IEnvelope clipEnvelope)
		{
			var polygonBeforeCopy = (IPolygon) ((IClone) polygonBefore).Clone();
			const int count = 1000;

			GeometryUtils.AllowIndexing(polygonBefore);

			Stopwatch watch = Stopwatch.StartNew();

			IPolygon intersectResult = null;

			for (var i = 0; i < count; i++)
			{
				intersectResult =
					(IPolygon) ((ITopologicalOperator) polygonBefore).Intersect(
						clipEnvelope, esriGeometryDimension.esriGeometry2Dimension);
			}

			watch.Stop();
			Console.WriteLine(@"ITopologicalOperator.Intersect(): {0:N3} ms per call",
			                  watch.ElapsedMilliseconds / (double) count);
			watch.Reset();

			Assert.True(GeometryUtils.AreEqual(polygonBeforeCopy, polygonBefore));

			watch.Start();

			var result = new PolygonClass();
			for (var i = 0; i < count; i++)
			{
				result.SetEmpty();
				((ITopologicalOperator) polygonBefore).QueryClipped(clipEnvelope, result);
			}

			watch.Stop();
			Console.WriteLine(@"ITopologicalOperator.QueryClipped(): {0:N3} ms per call",
			                  watch.ElapsedMilliseconds / (double) count);

			Assert.True(GeometryUtils.AreEqual(polygonBeforeCopy, polygonBefore));
			Assert.True(GeometryUtils.AreEqualInXY(intersectResult, result));

			watch.Start();

			result = new PolygonClass();
			for (var i = 0; i < count; i++)
			{
				result.SetEmpty();
				((ITopologicalOperator) polygonBefore).QueryClippedDense(
					clipEnvelope, double.MaxValue, result);
			}

			watch.Stop();
			Console.WriteLine(
				@"ITopologicalOperator.QueryClippedDense(): {0:N3} ms per call",
				watch.ElapsedMilliseconds / (double) count);

			Assert.True(GeometryUtils.AreEqual(polygonBeforeCopy, polygonBefore));
			Assert.True(GeometryUtils.AreEqualInXY(intersectResult, result));

			watch.Start();
			result = new PolygonClass();

			for (var i = 0; i < count; i++)
			{
				IPolygon input = GeometryFactory.Clone(polygonBefore);
				GeometryUtils.AllowIndexing(input);

				result.SetEmpty();
				((ITopologicalOperator) input).QueryClipped(clipEnvelope, result);
			}

			watch.Stop();
			Console.WriteLine(
				@"Clone() + ITopologicalOperator.QueryClipped(): {0:N3} ms per call",
				watch.ElapsedMilliseconds / (double) count);

			Assert.True(GeometryUtils.AreEqual(polygonBeforeCopy, polygonBefore));
			Assert.True(GeometryUtils.AreEqualInXY(intersectResult, result));
		}

		[Test]
		public void Confirm_IsSimpleDoesNotIgnoreDuplicatePaths()
		{
			AssertIsNotSimple(CreatePolyline(0, 0, 100, 100),
			                  CreatePolyline(0, 0, 100, 100));
		}

		[Test]
		public void Repro_IncorrectDifferenceResult()
		{
			string coveredXml =
				TestUtils.GetGeometryTestDataPath("differenceissue_coveredpoly.xml");
			string coveringXml =
				TestUtils.GetGeometryTestDataPath("differenceissue_coveringpoly.xml");

			IGeometry coveredPoly = GeometryUtils.FromXmlFile(coveredXml);
			IGeometry coveringPoly = GeometryUtils.FromXmlFile(coveringXml);

			var difference =
				(IPolygon4) ((ITopologicalOperator) coveringPoly).Difference(coveredPoly);

			Console.WriteLine(@"before simplify:");
			WriteRings(difference);

			((ITopologicalOperator2) difference).IsKnownSimple_2 = false;
			((ITopologicalOperator2) difference).Simplify();

			// none of the other methods to simplify work either (SimplifyAsFeature(), SimplifyPreserveFromTo(), SimplifyEx())

			Console.WriteLine(@"after simplify:");
			WriteRings(difference);

			Assert.IsTrue(difference.IsEmpty || ((IArea) difference).Area > 0);
		}

		[Test]
		public void Repro_IncorrectDifferenceResult_SliversPlusLargeDifference()
		{
			string coveredXml =
				TestUtils.GetGeometryTestDataPath("differenceissue_coveredpoly_largediff.xml");
			string coveringXml =
				TestUtils.GetGeometryTestDataPath("differenceissue_coveringpoly.xml");

			IGeometry coveredPoly = GeometryUtils.FromXmlFile(coveredXml);
			IGeometry coveringPoly = GeometryUtils.FromXmlFile(coveringXml);

			var difference =
				(IPolygon4) ((ITopologicalOperator) coveringPoly).Difference(coveredPoly);

			Console.WriteLine(@"before simplify:");
			WriteRings(difference);

			((ITopologicalOperator2) difference).IsKnownSimple_2 = false;
			((ITopologicalOperator2) difference).Simplify();

			// none of the other methods to simplify work either (SimplifyAsFeature(), SimplifyPreserveFromTo(), SimplifyEx())

			Console.WriteLine(@"after simplify:");
			WriteRings(difference);

			Assert.IsTrue(difference.IsEmpty || ((IArea) difference).Area > 0);
			Assert.AreEqual(1, ((IGeometryCollection) difference).GeometryCount);
		}

		private static void WriteRings(IPolygon4 difference)
		{
			if (difference.IsEmpty)
			{
				Console.WriteLine(@"difference is empty");
				return;
			}

			((ITopologicalOperator2) difference).IsKnownSimple_2 = false;
			esriNonSimpleReasonEnum reason;
			if (((ITopologicalOperator4) difference).get_IsSimpleEx(out reason))
			{
				Console.WriteLine(@"difference is simple");
			}
			else
			{
				Console.WriteLine(@"difference is non-simple (reason: {0})", reason);
			}

			var rings = (IGeometryCollection) difference;

			for (var i = 0; i < rings.GeometryCount; i++)
			{
				var ring = (IRing) rings.Geometry[i];

				Console.WriteLine(@"ring {0}: area={1} exterior={2}",
				                  i, ((IArea) ring).Area, ring.IsExterior);
			}
		}

		[Test]
		[Ignore("possibly crashes the test runner")]
		public void Repro_Crashing9IMRelation_NoSimplify()
		{
			string sourceXmlPath = TestUtils.GetGeometryTestDataPath("9IMrelation_source.xml");
			string targetXmlPath = TestUtils.GetGeometryTestDataPath("9IMrelation_target.xml");

			IGeometry sourceGeometry = GeometryUtils.FromXmlFile(sourceXmlPath);
			IGeometry targetGeometry = GeometryUtils.FromXmlFile(targetXmlPath);

			var sourceRelOp = (IRelationalOperator) sourceGeometry;

			// NOTE: the target geometry is not simple, but this is due to an (allowed) self-intersection of the polyline
			//       all IRelationalOperator methods except Relation() have no problem
			Assert.IsFalse(sourceRelOp.Disjoint(targetGeometry));
			Assert.IsTrue(sourceRelOp.Crosses(targetGeometry));
			Assert.IsFalse(sourceRelOp.Touches(targetGeometry));
			Assert.IsFalse(sourceRelOp.Overlaps(targetGeometry));
			Assert.IsFalse(sourceRelOp.Equals(targetGeometry));
			Assert.IsFalse(sourceRelOp.Contains(targetGeometry));
			Assert.IsFalse(sourceRelOp.Within(targetGeometry));

			// NOTE: same exception after:
			// ((IPolyline) sourceGeometry).SimplifyNetwork();
			// ((IPolyline) targetGeometry).SimplifyNetwork();

			// AccessViolationException on next line, at 10.2.2
			sourceRelOp.Relation(targetGeometry, "T********");
		}

		[Test]
		public void Repro_Crashing9IMRelation_AfterSimplify()
		{
			string sourceXmlPath = TestUtils.GetGeometryTestDataPath("9IMrelation_source.xml");
			string targetXmlPath = TestUtils.GetGeometryTestDataPath("9IMrelation_target.xml");

			IGeometry sourceGeometry = GeometryUtils.FromXmlFile(sourceXmlPath);
			IGeometry targetGeometry = GeometryUtils.FromXmlFile(targetXmlPath);

			var sourceRelOp = (IRelationalOperator) sourceGeometry;

			// NOTE: the target geometry is not simple, but this is due to an (allowed) self-intersection of the polyline
			//       all IRelationalOperator methods except Relation() have no problem
			Assert.IsFalse(sourceRelOp.Disjoint(targetGeometry));
			Assert.IsTrue(sourceRelOp.Crosses(targetGeometry));
			Assert.IsFalse(sourceRelOp.Touches(targetGeometry));
			Assert.IsFalse(sourceRelOp.Overlaps(targetGeometry));
			Assert.IsFalse(sourceRelOp.Equals(targetGeometry));
			Assert.IsFalse(sourceRelOp.Contains(targetGeometry));
			Assert.IsFalse(sourceRelOp.Within(targetGeometry));

			var sourceTopoOp = (ITopologicalOperator2) sourceGeometry;
			sourceTopoOp.IsKnownSimple_2 = false;
			sourceTopoOp.Simplify();

			var targetTopoOp = (ITopologicalOperator2) targetGeometry;
			targetTopoOp.IsKnownSimple_2 = false;
			targetTopoOp.Simplify();

			Assert.IsTrue(targetTopoOp.IsSimple);

			// Note: after Simplify() the following exception is raised (10.2.2):
			// System.Runtime.InteropServices.COMException : Exception from HRESULT: 0x80040239 
			sourceRelOp.Relation(targetGeometry, "T********");
		}

		[Test]
		public void
			Repro_TopologicalOperator_XyClusterToleranceException_AtDefaultTolerance
			()
		{
			// Succeeds with 10.6.1

			ISpatialReference sr = CreateSpatialReference(0.001, 0.0001);

			// ReSharper disable JoinDeclarationAndInitializer
			IPolyline p1;
			IPolyline p2;
			// ReSharper restore JoinDeclarationAndInitializer

			// regular case (length much longer than xy tolerance)
			// second polyline has an y offset of <= the xy tolerance
			p1 = CreatePolyline(sr, x: 0, y: 0, length: 10);
			p2 = CreatePolyline(sr, x: 1, y: 0.001, length: 10);

			Assert.True(TryGetDifference(p1, p2));
			Assert.True(TryGetPointIntersection(p1, p2));
			Assert.True(TryGetLinearIntersection(p1, p2));

			// length is equal to 5x the tolerance (or greater)
			// second polyline has an y offset of <= the xy tolerance			// --> NO exception
			p1 = CreatePolyline(sr, x: 1, y: 1, length: 0.004);
			p2 = CreatePolyline(sr, x: 1, y: 1.0021, length: 0.004);
			Assert.True(TryGetLinearIntersection(p1, p2));
			Assert.True(TryGetPointIntersection(p1, p2));
		}

		[Test]
		public void
			Repro_TopologicalOperator_XyClusterToleranceException_AtMinimumTolerance
			()
		{
			// Succeeds with 10.6.1

			// the xy tolerance is just 2x the resolution:
			ISpatialReference sr = CreateSpatialReference(0.0002, 0.0001);

			// ReSharper disable JoinDeclarationAndInitializer
			IPolyline p1;
			IPolyline p2;
			// ReSharper restore JoinDeclarationAndInitializer

			// regular case (length much longer than xy tolerance)
			// second polyline has an y offset of <= the xy tolerance
			p1 = CreatePolyline(sr, x: 0, y: 0, length: 10);
			p2 = CreatePolyline(sr, x: 1, y: 0.001, length: 10);

			Assert.True(TryGetDifference(p1, p2));
			Assert.True(TryGetPointIntersection(p1, p2));
			Assert.True(TryGetLinearIntersection(p1, p2));

			// length is equal to 5x the tolerance (or greater)
			// second polyline has an y offset of <= the xy tolerance
			// --> NO exception
			p1 = CreatePolyline(sr, x: 1, y: 1, length: 0.001);
			p2 = CreatePolyline(sr, x: 1, y: 1.0002, length: 0.001);

			Assert.True(TryGetDifference(p1, p2));
			Assert.True(TryGetLinearIntersection(p1, p2));
			Assert.True(TryGetPointIntersection(p1, p2));

			// length is less than 5x the tolerance
			// second polyline has an y offset of <= the xy tolerance
			// --> EXCEPTION
			p1 = CreatePolyline(sr, x: 1, y: 1, length: 0.0009);
			p2 = CreatePolyline(sr, x: 1, y: 1.0002, length: 0.0009);

			Assert.False(TryGetDifference(p1, p2));
			Assert.False(TryGetLinearIntersection(p1, p2));
			Assert.False(TryGetPointIntersection(p1, p2));

			// length is less than 5x the tolerance
			// second polyline has an y offset of > the xy tolerance
			// --> NO exception for difference, but EXCEPTION for linear and point intersection
			p1 = CreatePolyline(sr, x: 1, y: 1, length: 0.0009);
			p2 = CreatePolyline(sr, x: 1, y: 1.0003, length: 0.0009);

			Assert.True(TryGetDifference(p1, p2));
			Assert.False(TryGetLinearIntersection(p1, p2)); // EXCEPTION!!
			Assert.False(TryGetPointIntersection(p1, p2)); // EXCEPTION!!

			// conclusion:
			// --> the factor between xy tolerance and xy resolution plays no role in this
			// --> Only the relation of xy tolerance vs. line length and line offset seems to be important

			// possible workaround:
			// 1. catch this exception
			// 2. assign the geometries a new spatial reference with resolution/tolernance values that are BOTH reduced by a factor of ten
			// 3. retry the operation
			// 4. if exception: pass it on --> exit
			// 5. assign the original spatial reference (first operand) to the result
			// 6. simplify the result (in case of Difference(), it will probably be empty afterwards)
		}

		private static bool TryGetDifference([NotNull] IPolyline p1,
		                                     [NotNull] IPolyline p2)
		{
			try
			{
				((ITopologicalOperator) p1).Difference(p2);
				return true;
			}
			catch (COMException e)
			{
				if (e.ErrorCode == -2147220888)
				{
					// The xy cluster tolerance was too large for the extent of the data.
					Console.WriteLine(e.Message);
					return false;
				}

				throw;
			}
		}

		private static bool TryGetPointIntersection([NotNull] IPolyline p1,
		                                            [NotNull] IPolyline p2)
		{
			try
			{
				((ITopologicalOperator) p1).Intersect(p2,
				                                      esriGeometryDimension
					                                      .esriGeometry0Dimension);
				return true;
			}
			catch (COMException e)
			{
				if (e.ErrorCode == -2147220888)
				{
					// The xy cluster tolerance was too large for the extent of the data.
					Console.WriteLine(e.Message);
					return false;
				}

				throw;
			}
		}

		private static bool TryGetLinearIntersection([NotNull] IPolyline p1,
		                                             [NotNull] IPolyline p2)
		{
			try
			{
				((ITopologicalOperator) p1).Intersect(p2,
				                                      esriGeometryDimension
					                                      .esriGeometry1Dimension);
				return true;
			}
			catch (COMException e)
			{
				if (e.ErrorCode == -2147220888)
				{
					// The xy cluster tolerance was too large for the extent of the data.
					Console.WriteLine(e.Message);
					return false;
				}

				throw;
			}
		}

		[NotNull]
		private static ISpatialReference CreateSpatialReference(double xyTolerance,
		                                                        double xyResolution)
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, setDefaultXyDomain: true);

			((ISpatialReferenceResolution) result).XYResolution[true] = xyResolution;
			((ISpatialReferenceTolerance) result).XYTolerance = xyTolerance;

			return result;
		}

		[NotNull]
		private static IPolyline CreatePolyline(ISpatialReference spatialReference,
		                                        double x,
		                                        double y, double length)
		{
			IPolyline result = GeometryFactory.CreatePolyline(x, y, x + length, y);
			result.SpatialReference = spatialReference;
			return result;
		}

		[Test]
		public void Repro_IncorrectDisjointResult()
		{
			string containingPolygonXml =
				TestUtils.GetGeometryTestDataPath("containing_polygon.xml");
			string containedPolygonXml = TestUtils.GetGeometryTestDataPath("contained_polygon.xml");

			var containing = (IPolygon) GeometryUtils.FromXmlFile(containingPolygonXml);
			var contained = (IPolygon) GeometryUtils.FromXmlFile(containedPolygonXml);

			TestRelOps(contained, containing);

			IPolygon containingCopy = GeometryFactory.Clone(containing);
			WeedContainingPolygon(containingCopy, 1);

			TestRelOps(contained, containingCopy);

			containingCopy = GeometryFactory.Clone(containing);
			WeedContainingPolygon(containingCopy, 0.1);

			TestRelOps(contained, containingCopy);

			containingCopy = GeometryFactory.Clone(containing);
			WeedContainingPolygon(containingCopy, 0.01);

			TestRelOps(contained, containingCopy);

			containingCopy = GeometryFactory.Clone(containing);
			WeedContainingPolygon(containingCopy, 0.001);

			TestRelOps(contained, containingCopy);
		}

		private static void WeedContainingPolygon([NotNull] IPolygon containing,
		                                          double factor)
		{
			Console.WriteLine();
			Console.WriteLine(@"Weeding containing polygon with tolerance factor {0}",
			                  factor);
			Console.WriteLine();

			GeometryUtils.Weed(containing, factor);
		}

		private static void TestRelOps([NotNull] IPolygon contained,
		                               [NotNull] IPolygon containing)
		{
			Console.WriteLine(@"containing polygon vertex count: {0}",
			                  GeometryUtils.GetPointCount(containing));
			Console.WriteLine(@"containing polygon has non-linear segments: {0}",
			                  GeometryUtils.HasNonLinearSegments(containing));

			GeometryUtils.AllowIndexing(containing);
			GeometryUtils.AllowIndexing(contained);

			var containedRelOp = (IRelationalOperator) contained;
			var containingRelOp = (IRelationalOperator) containing;

			bool isDisjoint = containedRelOp.Disjoint(containing);
			bool isWithin = containedRelOp.Within(containing);
			bool isContained = containingRelOp.Contains(contained);

			Console.WriteLine(@"is disjoint: " + isDisjoint);
			Console.WriteLine(@"is within: " + isWithin);
			Console.WriteLine(@"is contained: " + isContained);
		}

		[Test]
		public void Repro_IsSimpleIgnoresDuplicateRings()
		{
			// NIM090612
			// Fails in 10.2
			// Succeeds in 10.2

			// rings overlap 
			// --> OK (found to be non-simple)
			Console.WriteLine(@"2 overlapping rings");
			AssertIsNotSimple(CreatePolygon(0, 0, 100, 100, 0),
			                  CreatePolygon(50, 50, 150, 150, 0));
			Console.WriteLine();

			// one ring is contained in the other 
			// --> OK (found to be non-simple)
			Console.WriteLine(@"2 rings, one containing the other:");
			AssertIsNotSimple(CreatePolygon(0, 0, 100, 100, 0),
			                  CreatePolygon(1, 1, 99, 99, 0));
			Console.WriteLine();

			// two congruent rings, but one has more vertices than the other 
			// --> OK (found to be non-simple)
			Console.WriteLine(@"2 identical rings, but one is densified:");
			IPolygon poly2 = CreatePolygon(0, 0, 100, 100, 0);
			((IPolycurve3D) poly2).Densify3D(10, 0);
			AssertIsNotSimple(CreatePolygon(0, 0, 100, 100, 0),
			                  poly2);
			Console.WriteLine();

			// two congruent rings (same vertices, same orientation, DIFFERENT Z)
			// --> NOT OK (found to be SIMPLE)
			// Note: this also affects the GP tools "check geometry" and "repair geometry"
			//       --> these features are not detected/repaired by those tools
			Console.WriteLine(@"2 congruent rings with different Z values:");
			AssertIsNotSimple(CreatePolygon(0, 0, 100, 100, 0),
			                  CreatePolygon(0, 0, 100, 100, 100));

			// two identical rings (same vertices, same orientation)
			// --> NOT OK (found to be SIMPLE)
			// Note: this also affects the GP tools "check geometry" and "repair geometry"
			//       --> these features are not detected/repaired by those tools
			Console.WriteLine(@"2 identical rings:");
			AssertIsNotSimple(CreatePolygon(0, 0, 100, 100, 0),
			                  CreatePolygon(0, 0, 100, 100, 0));
		}

		[Test]
		[Ignore("This test will stall the process (no exception)")]
		public void Repro_LinearCircularArc_PureVirtualFunctionCall()
		{
			// NIM090330
			var locator = new TestDataLocator(@"..\..\ProSuite\src");
			string mdbPath = locator.GetPath("linearcirculararcs_crash.mdb");

			IWorkspace workspace = WorkspaceUtils.OpenPgdbWorkspace(mdbPath);

			// The error was found for polygons containing linear CircularArcs (CircularArc.IsLine = true). 
			// The feature class contains two such features. It might also affect other situations

			IFeatureClass featureClass =
				DatasetUtils.OpenFeatureClass(workspace, "polygons");

			// The error only occurs for recycling cursors
			IFeatureCursor cursor = featureClass.Search(new QueryFilterClass(), true);

			IFeature feature = cursor.NextFeature();
			while (feature != null)
			{
				// the crash only occurs when accessing Shape, not when accessing ShapeCopy
				var polygon = (IPolygon) feature.Shape;

				var segments = (ISegmentCollection) polygon;

				IEnumSegment enumSegment = segments.EnumSegments;

				// enumSegment.Reset();  // if resetting here the error still occurs

				ISegment segment;
				int partIndex = -1;
				int segmentIndex = -1;
				enumSegment.Next(out segment, ref partIndex, ref segmentIndex);

				while (segment != null)
				{
					// releasing the segment here avoids the crash. 
					// However that can't be done if IEnumSegment is non-recycling and the segments are kept around
					// Marshal.ReleaseComObject(segment);

					enumSegment.Next(out segment, ref partIndex, ref segmentIndex);
				}

				// enumSegment.Reset();  // if resetting here the error still occurs

				// if a GC collection happens here, there will be a "pure virtual function call" message box on the next IEnumSegment.Next()
				// (-> on the next feature from the recycling cursor)
				// To reproduce the crash, induce a GC collection here:
				GC.Collect();

				feature = cursor.NextFeature();
			}
		}

		[Test]
		public void Repro_ProjectionResult_IncorrectEnvelope_SmallPolygon()
		{
			// NIM090607
			// Fails in 10.2
			// Succeeds in 10.2.2
			const string lv03PolygonXml =
				@"<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>584239.74399929866</XMin><YMin>219900.84313070029</YMin><XMax>584242.52499929816</XMax><YMax>219902.10303070024</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>584239.97299930081</X><Y>219901.44203069806</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>584242.52499929816</X><Y>219902.10303070024</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>584242.52499929816</X><Y>219902.10303070024</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>584239.74399929866</X><Y>219900.84313070029</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>584239.74399929866</X><Y>219900.84313070029</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>584239.97299930081</X><Y>219901.44203069806</Y></ToPoint><CenterPoint xsi:nil='true'/><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>true</IsLine></Segment></SegmentArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903_LV03&quot;,GEOGCS[&quot;GCS_CH1903&quot;,DATUM[&quot;D_CH1903&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,600000.0],PARAMETER[&quot;False_Northing&quot;,200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,21781]]</WKT><XOrigin>-29386399.999800701</XOrigin><YOrigin>-33067899.9997693</YOrigin><XYScale>10000</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>21781</WKID></SpatialReference></PolygonN>";
			const string lv95FineltraPolygonXml =
				@"<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>2584239.9302993007</XMin><YMin>1219901.3345306963</YMin><XMax>2584242.7112993002</XMax><YMax>1219902.5944306999</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2584240.1592992991</X><Y>1219901.9335307032</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2584242.7112993002</X><Y>1219902.5944306999</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2584242.7112993002</X><Y>1219902.5944306999</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2584239.9302993007</X><Y>1219901.3345306963</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2584239.9302993007</X><Y>1219901.3345306963</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2584240.1592992991</X><Y>1219901.9335307032</Y></ToPoint><CenterPoint xsi:nil='true'/><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>true</IsLine></Segment></SegmentArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-29386399.999800701</XOrigin><YOrigin>-33067899.9997693</YOrigin><XYScale>10000</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID></SpatialReference></PolygonN>";

			var lv03Polygon = (IPolygon) FromXmlString(lv03PolygonXml);
			var lv95FineltraPolygon = (IPolygon) FromXmlString(lv95FineltraPolygonXml);

			// Note: the polygon has linear CircularArcs (IsLine == true)

			ReproProjectIssues(lv03Polygon, lv95FineltraPolygon);
		}

		[Test]
		public void Repro_ProjectionResult_IncorrectEnvelope_LargePolygon()
		{
			//Fails in 10.2
			//Succeeds in 10.4.1
			const string lv03PolygonXml =
				@"<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>588433.09499929845</XMin><YMin>223225.04103070125</YMin><XMax>589816.70249930024</XMax><YMax>223737.42003069818</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589478.31999929994</X><Y>223734.21003070101</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589477.13899929821</X><Y>223730.0760307014</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589477.13899929821</X><Y>223730.0760307014</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589477.32899929956</X><Y>223725.7560307011</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589477.32899929956</X><Y>223725.7560307011</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589489.09799930081</X><Y>223617.39203070104</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589489.09799930081</X><Y>223617.39203070104</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589490.38899929821</X><Y>223610.5220307</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589490.38899929821</X><Y>223610.5220307</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589493.6289993003</X><Y>223608.05203070119</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589493.6289993003</X><Y>223608.05203070119</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589509.42599929869</X><Y>223615.35103069991</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589509.42599929869</X><Y>223615.35103069991</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589538.59499929845</X><Y>223634.90503070131</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589538.59499929845</X><Y>223634.90503070131</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589539.89299929887</X><Y>223632.96903070062</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589539.89299929887</X><Y>223632.96903070062</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589524.95199929923</X><Y>223622.3820306994</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589524.95199929923</X><Y>223622.3820306994</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589511.94499929994</X><Y>223614.18803070113</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589511.94499929994</X><Y>223614.18803070113</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589504.75899929926</X><Y>223610.19103069976</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589504.75899929926</X><Y>223610.19103069976</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589494.97099930048</X><Y>223605.83603069931</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589494.97099930048</X><Y>223605.83603069931</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589493.3799992986</X><Y>223602.89303069934</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589495.72192398796</X><Y>223603.52852605027</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589493.3799992986</X><Y>223602.89303069934</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589494.80399930105</X><Y>223599.17003069818</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589494.80399930105</X><Y>223599.17003069818</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589495.76099929959</X><Y>223598.07403070107</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589495.76099929959</X><Y>223598.07403070107</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589511.17399929836</X><Y>223559.28103069961</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589511.17399929836</X><Y>223559.28103069961</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589515.40999929979</X><Y>223548.27503069863</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589515.40999929979</X><Y>223548.27503069863</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589525.68599930033</X><Y>223516.97003069893</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589525.68599930033</X><Y>223516.97003069893</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589531.05199930072</X><Y>223509.54803070053</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589539.52118525608</X><Y>223521.32190187395</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589531.05199930072</X><Y>223509.54803070053</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589540.09999930114</X><Y>223508.19703070074</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589538.48457155668</X><Y>223528.35199907632</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589540.09999930114</X><Y>223508.19703070074</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589563.86499929801</X><Y>223512.24503070116</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589563.86499929801</X><Y>223512.24503070116</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589587.42999929935</X><Y>223516.40403069928</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589587.42999929935</X><Y>223516.40403069928</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589611.01799929887</X><Y>223521.03303069994</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589611.01799929887</X><Y>223521.03303069994</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589659.81799929962</X><Y>223530.96603069827</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589659.81799929962</X><Y>223530.96603069827</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589701.22299930081</X><Y>223540.1320306994</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589701.22299930081</X><Y>223540.1320306994</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589726.26699930057</X><Y>223546.3270306997</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589726.26699930057</X><Y>223546.3270306997</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589743.1999992989</X><Y>223550.51003069803</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589743.1999992989</X><Y>223550.51003069803</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589769.37299929932</X><Y>223557.99303070083</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589769.37299929932</X><Y>223557.99303070083</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589775.87299929932</X><Y>223566.30303069949</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589765.85219618934</X><Y>223567.44408608193</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589775.87299929932</X><Y>223566.30303069949</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589780.46999929845</X><Y>223570.74303070083</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589780.14278573997</X><Y>223566.4820388612</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589780.46999929845</X><Y>223570.74303070083</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589780.90699930117</X><Y>223570.71003070101</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589781.40277685109</X><Y>223580.18529717307</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589780.90699930117</X><Y>223570.71003070101</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589786.44699930027</X><Y>223572.72703069821</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589781.25496121065</X><Y>223578.37102997335</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589786.44699930027</X><Y>223572.72703069821</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589791.10799929872</X><Y>223577.61803070083</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589791.10799929872</X><Y>223577.61803070083</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589801.49799929932</X><Y>223585.55703070015</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589817.43160172051</X><Y>223553.93591472981</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589801.49799929932</X><Y>223585.55703070015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589810.74699930102</X><Y>223590.25503069907</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589810.74699930102</X><Y>223590.25503069907</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589813.19879930094</X><Y>223591.88413070142</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589806.11817189981</X><Y>223599.8809615983</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589813.19879930094</X><Y>223591.88413070142</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589816.70249930024</X><Y>223593.26303070039</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589816.70249930024</X><Y>223593.26303070039</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589811.54599929973</X><Y>223588.64203070104</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589806.11535850528</X><Y>223599.88952386079</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589811.54599929973</X><Y>223588.64203070104</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589802.3099992983</X><Y>223583.9510307014</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589802.3099992983</X><Y>223583.9510307014</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589792.42999929935</X><Y>223576.39503069967</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589817.42657880264</X><Y>223553.94764618081</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589792.42999929935</X><Y>223576.39503069967</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589786.79099930078</X><Y>223569.64503069967</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589786.79099930078</X><Y>223569.64503069967</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589787.27199929953</X><Y>223567.66603070125</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589787.27199929953</X><Y>223567.66603070125</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589787.48499929905</X><Y>223566.78503070027</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589787.48499929905</X><Y>223566.78503070027</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589805.21199930087</X><Y>223563.08203069866</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589805.21199930087</X><Y>223563.08203069866</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589808.87199930102</X><Y>223563.51203069836</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589808.87199930102</X><Y>223563.51203069836</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589793.73299929872</X><Y>223559.05303069949</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589793.73299929872</X><Y>223559.05303069949</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589792.23099929839</X><Y>223560.28403069824</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589792.47583355557</X><Y>223559.05093447873</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589792.23099929839</X><Y>223560.28403069824</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589786.10299929976</X><Y>223558.703030698</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589786.10299929976</X><Y>223558.703030698</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589743.8399992995</X><Y>223547.32003070042</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589743.8399992995</X><Y>223547.32003070042</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589701.93299929798</X><Y>223536.94203069806</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589701.93299929798</X><Y>223536.94203069806</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589660.28799929842</X><Y>223527.72603069991</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589660.28799929842</X><Y>223527.72603069991</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589611.50799930096</X><Y>223517.89303069934</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589611.50799930096</X><Y>223517.89303069934</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589540.6989993006</X><Y>223505.03703070059</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589540.6989993006</X><Y>223505.03703070059</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589493.44399929792</X><Y>223496.50903069973</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589493.44399929792</X><Y>223496.50903069973</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589434.50099929795</X><Y>223485.07903070003</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589434.50099929795</X><Y>223485.07903070003</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589375.87699929997</X><Y>223472.07903070003</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589375.87699929997</X><Y>223472.07903070003</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589311.83599929884</X><Y>223455.82103069872</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589311.83599929884</X><Y>223455.82103069872</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589248.37599929795</X><Y>223437.41403070092</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589248.37599929795</X><Y>223437.41403070092</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589236.93599930033</X><Y>223433.60503070056</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589236.93599930033</X><Y>223433.60503070056</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589226.04699929804</X><Y>223452.61903069913</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589226.04699929804</X><Y>223452.61903069913</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589197.58799929917</X><Y>223502.29903069884</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589197.58799929917</X><Y>223502.29903069884</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589195.98799929768</X><Y>223508.55003070086</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589195.98799929768</X><Y>223508.55003070086</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589196.34799930081</X><Y>223510.24003069848</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589196.34799930081</X><Y>223510.24003069848</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589196.77799930051</X><Y>223527.57103069872</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589196.77799930051</X><Y>223527.57103069872</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589192.96099929884</X><Y>223528.74403069913</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589194.18333836424</X><Y>223525.92472908719</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589192.96099929884</X><Y>223528.74403069913</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589186.96799929813</X><Y>223524.29203069955</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589194.78982560779</X><Y>223520.02248250128</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589186.96799929813</X><Y>223524.29203069955</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589185.92799929902</X><Y>223518.60203069821</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589195.11562765564</X><Y>223519.86278931375</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589185.92799929902</X><Y>223518.60203069821</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589186.34799930081</X><Y>223516.8420307003</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589195.13357708638</X><Y>223519.8687026827</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589186.34799930081</X><Y>223516.8420307003</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589188.93699929863</X><Y>223509.54203069955</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589121.27769739961</X><Y>223489.65525371942</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589188.93699929863</X><Y>223509.54203069955</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589192.44299929962</X><Y>223500.55903070047</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589263.77844875446</X><Y>223533.57642448699</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589192.44299929962</X><Y>223500.55903070047</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589214.73699929938</X><Y>223452.21003070101</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>571007.49313711724</X><Y>215086.0603575916</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589214.73699929938</X><Y>223452.21003070101</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589216.53999929875</X><Y>223448.12403069809</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589216.53999929875</X><Y>223448.12403069809</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589207.96699929982</X><Y>223445.35703070089</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589207.96699929982</X><Y>223445.35703070089</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589177.93799930066</X><Y>223510.68203070015</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589177.93799930066</X><Y>223510.68203070015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589171.47799929976</X><Y>223516.32203070074</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589165.83256653138</X><Y>223503.33619813152</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589171.47799929976</X><Y>223516.32203070074</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589162.89799929783</X><Y>223516.54203069955</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589166.92729409283</X><Y>223506.26452761461</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589162.89799929783</X><Y>223516.54203069955</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589159.14799929783</X><Y>223515.12203070149</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589159.14799929783</X><Y>223515.12203070149</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589152.27399929985</X><Y>223512.52003069967</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589152.27399929985</X><Y>223512.52003069967</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589151.49799929932</X><Y>223508.61203069985</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589153.2313516516</X><Y>223510.29888806262</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589151.49799929932</X><Y>223508.61203069985</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589127.39199930057</X><Y>223499.46903070062</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589127.39199930057</X><Y>223499.46903070062</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589123.13899929821</X><Y>223501.4670307003</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589124.08298373385</X><Y>223497.95089421209</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589123.13899929821</X><Y>223501.4670307003</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589086.55099929869</X><Y>223487.70503069833</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589086.55099929869</X><Y>223487.70503069833</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589014.68999930099</X><Y>223460.47403069958</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589014.68999930099</X><Y>223460.47403069958</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588983.94299929962</X><Y>223448.86803070083</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588983.94299929962</X><Y>223448.86803070083</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588982.23299929872</X><Y>223444.50803070143</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588985.52029874176</X><Y>223445.73407839617</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588982.23299929872</X><Y>223444.50803070143</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588973.37299929932</X><Y>223441.15803069994</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588973.37299929932</X><Y>223441.15803069994</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588968.97299930081</X><Y>223443.20803070068</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588970.18515925133</X><Y>223440.06278864597</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588968.97299930081</X><Y>223443.20803070068</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588870.16799930111</X><Y>223405.78403069824</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588870.16799930111</X><Y>223405.78403069824</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588868.16799930111</X><Y>223405.02403070033</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588868.16799930111</X><Y>223405.02403070033</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588804.13999930024</X><Y>223380.8120306991</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588804.13999930024</X><Y>223380.8120306991</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588804.09809929878</X><Y>223380.79583070055</Y></ToPoint><CenterPoint xsi:nil='true'/><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>true</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588804.09809929878</X><Y>223380.79583070055</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588790.86799930036</X><Y>223375.79203069955</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588790.86799930036</X><Y>223375.79203069955</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588746.55299929902</X><Y>223359.02903069928</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588746.55299929902</X><Y>223359.02903069928</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588740.76199929789</X><Y>223356.83903069794</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588740.76199929789</X><Y>223356.83903069794</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588718.72999930009</X><Y>223348.50503069907</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588718.72999930009</X><Y>223348.50503069907</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588696.31399929896</X><Y>223340.0610307008</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588696.31399929896</X><Y>223340.0610307008</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588681.99699930102</X><Y>223334.67503070086</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588681.99699930102</X><Y>223334.67503070086</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588667.83799929917</X><Y>223329.578030698</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588667.83799929917</X><Y>223329.578030698</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588657.48899929971</X><Y>223325.83903069794</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588657.48899929971</X><Y>223325.83903069794</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588630.31699929759</X><Y>223316.47603069991</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588630.31699929759</X><Y>223316.47603069991</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588614.42499930039</X><Y>223311.16203070059</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588614.42499930039</X><Y>223311.16203070059</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588598.27199929953</X><Y>223305.65803069994</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588598.27199929953</X><Y>223305.65803069994</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588584.2709992975</X><Y>223300.63403069973</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588584.2709992975</X><Y>223300.63403069973</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588570.92899930105</X><Y>223295.47203069925</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588570.92899930105</X><Y>223295.47203069925</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588557.52799930051</X><Y>223290.00903069973</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588557.52799930051</X><Y>223290.00903069973</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588543.57599930093</X><Y>223284.0070306994</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588543.57599930093</X><Y>223284.0070306994</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588529.79499929771</X><Y>223277.70503069833</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588529.79499929771</X><Y>223277.70503069833</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588517.2539993003</X><Y>223271.70403070003</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588517.2539993003</X><Y>223271.70403070003</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588504.54299930111</X><Y>223265.30203070119</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588504.54299930111</X><Y>223265.30203070119</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588491.38199929893</X><Y>223258.37003070116</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588491.38199929893</X><Y>223258.37003070116</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588480.31099930033</X><Y>223252.20903069898</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588480.31099930033</X><Y>223252.20903069898</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588468.29999930039</X><Y>223245.30803069845</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588468.29999930039</X><Y>223245.30803069845</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588461.98999929801</X><Y>223241.54703069851</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588461.98999929801</X><Y>223241.54703069851</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588455.33899929747</X><Y>223237.29703069851</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588455.33899929747</X><Y>223237.29703069851</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588447.8089993</X><Y>223232.13603070006</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588447.8089993</X><Y>223232.13603070006</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588444.69799929857</X><Y>223229.78603069857</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588444.69799929857</X><Y>223229.78603069857</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588440.03199930117</X><Y>223225.04103070125</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588451.5089729802</X><Y>223218.42179599128</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588440.03199930117</X><Y>223225.04103070125</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588437.83799929917</X><Y>223228.70503069833</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588437.83799929917</X><Y>223228.70503069833</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588433.09499929845</X><Y>223236.88003069907</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588433.09499929845</X><Y>223236.88003069907</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588433.23999929801</X><Y>223236.93003069982</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588432.3027892852</X><Y>223239.41268969211</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588433.23999929801</X><Y>223236.93003069982</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588438.02799930051</X><Y>223239.24603069946</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588438.02799930051</X><Y>223239.24603069946</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588441.86799930036</X><Y>223241.22603069991</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588441.86799930036</X><Y>223241.22603069991</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588457.52899929881</X><Y>223249.99803069979</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588457.52899929881</X><Y>223249.99803069979</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588467.45999930054</X><Y>223255.85903070122</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588467.45999930054</X><Y>223255.85903070122</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588495.01599929854</X><Y>223271.14503069967</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588778.79427651828</X><Y>222727.09873518336</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588495.01599929854</X><Y>223271.14503069967</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588559.98099929839</X><Y>223301.55003070086</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588838.92983004008</X><Y>222620.92617234492</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588559.98099929839</X><Y>223301.55003070086</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588637.47799929976</X><Y>223328.92803069949</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589133.25496729591</X><Y>221802.19497356811</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588637.47799929976</X><Y>223328.92803069949</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588646.04899929836</X><Y>223335.54903069884</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588635.44354473846</X><Y>223340.41982212575</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588646.04899929836</X><Y>223335.54903069884</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588646.12799929827</X><Y>223343.74403069913</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588634.36613596557</X><Y>223339.75953456748</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588646.12799929827</X><Y>223343.74403069913</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588650.69099929929</X><Y>223345.23203070089</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588650.69099929929</X><Y>223345.23203070089</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588650.75999929756</X><Y>223345.12003070116</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588651.74699103227</X><Y>223345.80534255857</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588650.75999929756</X><Y>223345.12003070116</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588653.45999930054</X><Y>223341.01803069934</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588653.45999930054</X><Y>223341.01803069934</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588654.01499930024</X><Y>223340.16803070158</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588654.01499930024</X><Y>223340.16803070158</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588655.71299929917</X><Y>223341.2730306983</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588654.86679020198</X><Y>223340.71624205535</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588655.71299929917</X><Y>223341.2730306983</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588655.10699930042</X><Y>223342.11303069815</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588655.10699930042</X><Y>223342.11303069815</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588652.67299930006</X><Y>223345.87803069875</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588652.67299930006</X><Y>223345.87803069875</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588658.90199929848</X><Y>223347.91003070027</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588658.90199929848</X><Y>223347.91003070027</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588683.56599929929</X><Y>223345.33403069898</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588674.04132127238</X><Y>223373.50083082408</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588683.56599929929</X><Y>223345.33403069898</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588695.88399929926</X><Y>223349.9910307005</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588695.88399929926</X><Y>223349.9910307005</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588958.79899929836</X><Y>223449.52603070065</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588958.79899929836</X><Y>223449.52603070065</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588963.46099929884</X><Y>223453.2560307011</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588955.14877147332</X><Y>223458.86676370914</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588963.46099929884</X><Y>223453.2560307011</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588983.98299929872</X><Y>223460.84903069958</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588983.98299929872</X><Y>223460.84903069958</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588993.11199929938</X><Y>223464.31703069806</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588993.11199929938</X><Y>223464.31703069806</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588992.76199929789</X><Y>223465.30703070015</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588992.76199929789</X><Y>223465.30703070015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588998.02199929953</X><Y>223467.29703069851</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588998.02199929953</X><Y>223467.29703069851</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588998.37199930102</X><Y>223466.30703070015</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588998.37199930102</X><Y>223466.30703070015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589015.98799929768</X><Y>223472.97203069925</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589015.98799929768</X><Y>223472.97203069925</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589060.32699929923</X><Y>223489.74903069809</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589060.32699929923</X><Y>223489.74903069809</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589147.63099930063</X><Y>223522.78103069961</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589147.63099930063</X><Y>223522.78103069961</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589157.88799929991</X><Y>223526.66203070059</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589157.88799929991</X><Y>223526.66203070059</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589162.64799929783</X><Y>223530.82303069904</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589152.78423760773</X><Y>223537.30362314096</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589162.64799929783</X><Y>223530.82303069904</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589164.56799929962</X><Y>223536.58303070068</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589152.76323995204</X><Y>223537.31795048446</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589164.56799929962</X><Y>223536.58303070068</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589173.50799930096</X><Y>223535.96303069964</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589173.50799930096</X><Y>223535.96303069964</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589173.87799929827</X><Y>223533.99303070083</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589173.87799929827</X><Y>223533.99303070083</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589175.65799929947</X><Y>223531.85303070024</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589176.45915927098</X><Y>223534.32969647233</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589175.65799929947</X><Y>223531.85303070024</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589178.32799929753</X><Y>223532.5230306983</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589176.45560726675</X><Y>223534.32957805425</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589178.32799929753</X><Y>223532.5230306983</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589179.88799929991</X><Y>223533.61303069815</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589184.82891325746</X><Y>223524.88030061522</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589179.88799929991</X><Y>223533.61303069815</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589181.68799930066</X><Y>223534.4130306989</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589184.85381030501</X><Y>223524.86495594267</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589181.68799930066</X><Y>223534.4130306989</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589184.4109992981</X><Y>223535.07503069937</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589184.4109992981</X><Y>223535.07503069937</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589199.68699929863</X><Y>223538.79103070125</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589199.68699929863</X><Y>223538.79103070125</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589200.50799930096</X><Y>223538.9910307005</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589200.50799930096</X><Y>223538.9910307005</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589220.40799929947</X><Y>223543.81703069806</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589220.40799929947</X><Y>223543.81703069806</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589276.09799930081</X><Y>223557.31803070009</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589276.09799930081</X><Y>223557.31803070009</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589343.3409992978</X><Y>223573.63303070143</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589343.3409992978</X><Y>223573.63303070143</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589343.57099929824</X><Y>223572.75303069875</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589343.57099929824</X><Y>223572.75303069875</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589347.67999929935</X><Y>223573.203030698</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589347.67999929935</X><Y>223573.203030698</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589482.36199929938</X><Y>223605.02903069928</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589482.36199929938</X><Y>223605.02903069928</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589484.97899929807</X><Y>223606.10203069821</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589484.97899929807</X><Y>223606.10203069821</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589486.41899929941</X><Y>223609.96203070134</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589486.41899929941</X><Y>223609.96203070134</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589473.84899929911</X><Y>223725.21603069827</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589473.84899929911</X><Y>223725.21603069827</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589473.31899929792</X><Y>223731.59603070095</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589473.31899929792</X><Y>223731.59603070095</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589475.8099992983</X><Y>223737.42003069818</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589475.8099992983</X><Y>223737.42003069818</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589478.31999929994</X><Y>223734.21003070101</Y></ToPoint></Segment></SegmentArray></Ring><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589471.17299930006</X><Y>223599.34703069925</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589344.67699930072</X><Y>223569.15903069824</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589344.67699930072</X><Y>223569.15903069824</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589345.18099929765</X><Y>223567.5220307</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589345.18099929765</X><Y>223567.5220307</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589345.63099930063</X><Y>223565.87203070149</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589345.63099930063</X><Y>223565.87203070149</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589340.90599929914</X><Y>223564.72503070161</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589340.90599929914</X><Y>223564.72503070161</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589336.59299929813</X><Y>223563.67803069949</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589336.59299929813</X><Y>223563.67803069949</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589334.73699929938</X><Y>223563.22803070024</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589334.73699929938</X><Y>223563.22803070024</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589300.75199929997</X><Y>223554.97903069854</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589300.75199929997</X><Y>223554.97903069854</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589299.71999929845</X><Y>223556.20503069833</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589299.71999929845</X><Y>223556.20503069833</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589291.83799929917</X><Y>223553.54203069955</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589291.83799929917</X><Y>223553.54203069955</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589290.61999930069</X><Y>223552.52003069967</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589297.05043855379</X><Y>223546.0931510487</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589290.61999930069</X><Y>223552.52003069967</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589203.10699930042</X><Y>223531.21403069794</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589203.10699930042</X><Y>223531.21403069794</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589202.46799929813</X><Y>223505.44903070107</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589202.46799929813</X><Y>223505.44903070107</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589204.26799929887</X><Y>223498.37803069875</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589215.87936982128</X><Y>223505.09843622148</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589204.26799929887</X><Y>223498.37803069875</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589212.90799929947</X><Y>223483.20503069833</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589212.90799929947</X><Y>223483.20503069833</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589228.19699930027</X><Y>223456.76903070137</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589228.19699930027</X><Y>223456.76903070137</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589232.08599929884</X><Y>223455.91003070027</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589230.66605274216</X><Y>223458.71437160295</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589232.08599929884</X><Y>223455.91003070027</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589241.08599929884</X><Y>223440.57503069937</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589241.08599929884</X><Y>223440.57503069937</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589247.50599930063</X><Y>223440.21503069997</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589247.50599930063</X><Y>223440.21503069997</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589311.01599929854</X><Y>223458.7010307014</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589814.69686369959</X><Y>221609.92910371308</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589311.01599929854</X><Y>223458.7010307014</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589375.13699929789</X><Y>223474.97903069854</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589812.38834394596</X><Y>221618.16422312881</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589375.13699929789</X><Y>223474.97903069854</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589433.81099930033</X><Y>223487.99903069809</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589819.64528818405</X><Y>221610.53970612938</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589433.81099930033</X><Y>223487.99903069809</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589492.85499930009</X><Y>223499.17003069818</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589819.39969842089</X><Y>221611.60420820024</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589492.85499930009</X><Y>223499.17003069818</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589516.4339993</X><Y>223503.93903069943</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589516.4339993</X><Y>223503.93903069943</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589521.6839993</X><Y>223507.94903070107</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589521.6839993</X><Y>223507.94903070107</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589521.91699929908</X><Y>223515.94003070146</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589521.91699929908</X><Y>223515.94003070146</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589512.16999929771</X><Y>223546.99503070116</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589512.16999929771</X><Y>223546.99503070116</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589492.64799929783</X><Y>223595.75203070045</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589492.64799929783</X><Y>223595.75203070045</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589486.27899929881</X><Y>223600.80203070119</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589485.31987869798</X><Y>223593.05114563252</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589486.27899929881</X><Y>223600.80203070119</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589472.17699930072</X><Y>223599.58703070134</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589472.17699930072</X><Y>223599.58703070134</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589471.17299930006</X><Y>223599.34703069925</Y></ToPoint></Segment></SegmentArray></Ring><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588683.94699930027</X><Y>223339.98303069919</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588683.35599929839</X><Y>223341.53103069961</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588683.64708344394</X><Y>223340.75534480112</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588683.35599929839</X><Y>223341.53103069961</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588682.69199929759</X><Y>223341.27903069928</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588682.69199929759</X><Y>223341.27903069928</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588683.29999930039</X><Y>223339.73903070018</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588683.29999930039</X><Y>223339.73903070018</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588683.94699930027</X><Y>223339.98303069919</Y></ToPoint></Segment></SegmentArray></Ring><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588643.19299929962</X><Y>223325.35703070089</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588643.84799930081</X><Y>223325.58203069866</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588643.84799930081</X><Y>223325.58203069866</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588643.30199930072</X><Y>223327.12603069842</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588643.30199930072</X><Y>223327.12603069842</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588642.62399929762</X><Y>223326.89303069934</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588642.62399929762</X><Y>223326.89303069934</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588643.19299929962</X><Y>223325.35703070089</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588642.90114945034</X><Y>223326.12230800241</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment></SegmentArray></Ring><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588438.24699930102</X><Y>223229.96903070062</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588442.10799929872</X><Y>223233.53603069857</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588442.10799929872</X><Y>223233.53603069857</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588444.92799929902</X><Y>223236.0060307011</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588444.92799929902</X><Y>223236.0060307011</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588447.01599929854</X><Y>223237.73003070056</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588447.01599929854</X><Y>223237.73003070056</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588446.46399929747</X><Y>223238.55803069845</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588446.69822583196</X><Y>223238.11618172203</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588446.46399929747</X><Y>223238.55803069845</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588444.00799930096</X><Y>223237.2560307011</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588444.00799930096</X><Y>223237.2560307011</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588440.64799929783</X><Y>223235.54603070021</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>588440.64799929783</X><Y>223235.54603070021</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588435.86699929833</X><Y>223233.18403070047</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588435.86699929833</X><Y>223233.18403070047</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588435.68999930099</X><Y>223232.43303069845</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588436.05588540295</X><Y>223232.74315474814</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588435.68999930099</X><Y>223232.43303069845</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588437.46399929747</X><Y>223230.10603069887</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588415.1514946497</X><Y>223214.93569091367</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>588437.46399929747</X><Y>223230.10603069887</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>588438.24699930102</X><Y>223229.96903070062</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>588437.90746763011</X><Y>223230.33454678571</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment></SegmentArray></Ring><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589182.63799929991</X><Y>223516.01203069836</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589182.21799929813</X><Y>223516.93203070015</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>589182.21799929813</X><Y>223516.93203070015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589180.34799930081</X><Y>223516.10203069821</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589180.34799930081</X><Y>223516.10203069821</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589180.75799930096</X><Y>223515.18203070015</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589093.70240080636</X><Y>223476.93687257887</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>589180.75799930096</X><Y>223515.18203070015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>589182.63799929991</X><Y>223516.01203069836</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>589181.69852774194</X><Y>223515.59583374744</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment></SegmentArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903_LV03&quot;,GEOGCS[&quot;GCS_CH1903&quot;,DATUM[&quot;D_CH1903&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,600000.0],PARAMETER[&quot;False_Northing&quot;,200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,21781]]</WKT><XOrigin>-29386399.999800701</XOrigin><YOrigin>-33067899.9997693</YOrigin><XYScale>10000</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>21781</WKID></SpatialReference></PolygonN>";
			const string lv95FineltraPolygonXml =
				@"<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>2588433.3325993009</XMin><YMin>1223225.5483307019</YMin><XMax>2589816.9605992995</XMax><YMax>1223737.9292306975</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589478.5709992982</X><Y>1223734.7192306966</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589477.3899993002</X><Y>1223730.5852307007</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589477.3899993002</X><Y>1223730.5852307007</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589477.5799992979</X><Y>1223726.2651306987</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589477.5799992979</X><Y>1223726.2651306987</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589489.3503993005</X><Y>1223617.9001306966</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589489.3503993005</X><Y>1223617.9001306966</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589490.6414992996</X><Y>1223611.0300306976</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589490.6414992996</X><Y>1223611.0300306976</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589493.8815992996</X><Y>1223608.5600306988</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589493.8815992996</X><Y>1223608.5600306988</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589509.6787992977</X><Y>1223615.8590307012</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589509.6787992977</X><Y>1223615.8590307012</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589538.8480992988</X><Y>1223635.4131307006</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589538.8480992988</X><Y>1223635.4131307006</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589540.1461993009</X><Y>1223633.4771306962</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589540.1461993009</X><Y>1223633.4771306962</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589525.2049992979</X><Y>1223622.8901306987</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589525.2049992979</X><Y>1223622.8901306987</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589512.1978993006</X><Y>1223614.6960306987</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589512.1978993006</X><Y>1223614.6960306987</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589505.0117992982</X><Y>1223610.6990306973</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589505.0117992982</X><Y>1223610.6990306973</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589495.2236992978</X><Y>1223606.3440307006</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589495.2236992978</X><Y>1223606.3440307006</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589493.6326992996</X><Y>1223603.4010306969</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589495.973080392</X><Y>1223604.0373605278</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589493.6326992996</X><Y>1223603.4010306969</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589495.0567993</X><Y>1223599.6779306978</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589495.0567993</X><Y>1223599.6779306978</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589496.0137992986</X><Y>1223598.581930697</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589496.0137992986</X><Y>1223598.581930697</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589511.427499298</X><Y>1223559.7885306999</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589511.427499298</X><Y>1223559.7885306999</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589515.6636992991</X><Y>1223548.782430701</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589515.6636992991</X><Y>1223548.782430701</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589525.9401993006</X><Y>1223517.4771306962</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589525.9401993006</X><Y>1223517.4771306962</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589531.3063993007</X><Y>1223510.0550306961</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589539.7756953523</X><Y>1223521.8292956478</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589531.3063993007</X><Y>1223510.0550306961</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589540.3545993008</X><Y>1223508.7040307</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589538.7391006942</X><Y>1223528.8596248536</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589540.3545993008</X><Y>1223508.7040307</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589564.1199993007</X><Y>1223512.7520307004</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589564.1199993007</X><Y>1223512.7520307004</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589587.6852992997</X><Y>1223516.9109307006</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589587.6852992997</X><Y>1223516.9109307006</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589611.2736992985</X><Y>1223521.5399307013</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589611.2736992985</X><Y>1223521.5399307013</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589660.0744992979</X><Y>1223531.4729306996</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589660.0744992979</X><Y>1223531.4729306996</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589701.4800992981</X><Y>1223540.6389307007</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589701.4800992981</X><Y>1223540.6389307007</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589726.5244993009</X><Y>1223546.833930701</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589726.5244993009</X><Y>1223546.833930701</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589743.4577993006</X><Y>1223551.0168306977</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589743.4577993006</X><Y>1223551.0168306977</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589769.6311993003</X><Y>1223558.4998307005</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589769.6311993003</X><Y>1223558.4998307005</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589776.1311993003</X><Y>1223566.8099306971</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589766.1104509672</X><Y>1223567.9508295057</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589776.1311993003</X><Y>1223566.8099306971</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589780.7281992994</X><Y>1223571.2499307021</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589780.4009767924</X><Y>1223566.988948127</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589780.7281992994</X><Y>1223571.2499307021</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589781.1651992984</X><Y>1223571.2169307023</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589781.6609981894</X><Y>1223580.6924796768</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589781.1651992984</X><Y>1223571.2169307023</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589786.7052992992</X><Y>1223573.2339306995</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589781.5132297114</X><Y>1223578.8779992373</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589786.7052992992</X><Y>1223573.2339306995</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589791.3662992977</X><Y>1223578.1250306964</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589791.3662992977</X><Y>1223578.1250306964</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589801.7563992999</X><Y>1223586.0640306994</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589817.6897646557</X><Y>1223554.4428934178</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589801.7563992999</X><Y>1223586.0640306994</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589811.0055992976</X><Y>1223590.7620306984</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589811.0055992976</X><Y>1223590.7620306984</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589813.4575993009</X><Y>1223592.391030699</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589806.3464553864</X><Y>1223600.4349548165</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589813.4575993009</X><Y>1223592.391030699</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589816.9605992995</X><Y>1223593.7700306997</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589816.9605992995</X><Y>1223593.7700306997</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589811.8045993</X><Y>1223589.1490307003</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589806.3788938369</X><Y>1223600.389871615</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589811.8045993</X><Y>1223589.1490307003</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589802.5684993006</X><Y>1223584.4580307007</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589802.5684993006</X><Y>1223584.4580307007</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589792.6883993</X><Y>1223576.902030699</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589817.6848445348</X><Y>1223554.454621684</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589792.6883993</X><Y>1223576.902030699</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589787.0493992977</X><Y>1223570.1519306973</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589787.0493992977</X><Y>1223570.1519306973</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589787.5303993002</X><Y>1223568.1729307026</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589787.5303993002</X><Y>1223568.1729307026</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589787.7433992997</X><Y>1223567.2919306979</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589787.7433992997</X><Y>1223567.2919306979</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589805.4707993008</X><Y>1223563.588830702</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589805.4707993008</X><Y>1223563.588830702</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589809.130799301</X><Y>1223564.0188307017</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589809.130799301</X><Y>1223564.0188307017</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589793.991599299</X><Y>1223559.5598307028</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589793.991599299</X><Y>1223559.5598307028</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589792.4895992987</X><Y>1223560.7908307016</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589792.7343769539</X><Y>1223559.5576654195</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589792.4895992987</X><Y>1223560.7908307016</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589786.3614992984</X><Y>1223559.2098307014</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589786.3614992984</X><Y>1223559.2098307014</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589744.0977993011</X><Y>1223547.8268307</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589744.0977993011</X><Y>1223547.8268307</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589702.1901993006</X><Y>1223537.4488307014</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589702.1901993006</X><Y>1223537.4488307014</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589660.5444993004</X><Y>1223528.2329306975</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589660.5444993004</X><Y>1223528.2329306975</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589611.7637992986</X><Y>1223518.3999307007</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589611.7637992986</X><Y>1223518.3999307007</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589540.9535993002</X><Y>1223505.5440306962</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589540.9535993002</X><Y>1223505.5440306962</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589493.6978993006</X><Y>1223497.016030699</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589493.6978993006</X><Y>1223497.016030699</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589434.7538992986</X><Y>1223485.586130701</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589434.7538992986</X><Y>1223485.586130701</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589376.1289993003</X><Y>1223472.586130701</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589376.1289993003</X><Y>1223472.586130701</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589312.0869993009</X><Y>1223456.3281306997</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589312.0869993009</X><Y>1223456.3281306997</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589248.6260992996</X><Y>1223437.9211307019</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589248.6260992996</X><Y>1223437.9211307019</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589237.1858992986</X><Y>1223434.1121307015</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589237.1858992986</X><Y>1223434.1121307015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589226.2964993007</X><Y>1223453.1263307035</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589226.2964993007</X><Y>1223453.1263307035</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589197.8364992999</X><Y>1223502.8068306968</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589197.8364992999</X><Y>1223502.8068306968</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589196.2363993004</X><Y>1223509.0579307005</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589196.2363993004</X><Y>1223509.0579307005</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589196.5963992998</X><Y>1223510.7479306981</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589196.5963992998</X><Y>1223510.7479306981</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589197.0261992998</X><Y>1223528.0791307017</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589197.0261992998</X><Y>1223528.0791307017</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589193.2090993002</X><Y>1223529.2521307021</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589194.4314706265</X><Y>1223526.4327128718</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589193.2090993002</X><Y>1223529.2521307021</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589187.2159992978</X><Y>1223524.8001307026</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589195.0378075824</X><Y>1223520.5305656905</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589187.2159992978</X><Y>1223524.8001307026</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589186.1760993004</X><Y>1223519.1100307032</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589195.3636249211</X><Y>1223520.3710291276</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589186.1760993004</X><Y>1223519.1100307032</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589186.5960992984</X><Y>1223517.3500306979</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589195.3817515355</X><Y>1223520.3767204224</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589186.5960992984</X><Y>1223517.3500306979</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589189.185199298</X><Y>1223510.0499306992</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589121.7330644955</X><Y>1223490.2361099557</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589189.185199298</X><Y>1223510.0499306992</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589192.6913992986</X><Y>1223501.0668307021</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589264.0275580566</X><Y>1223534.0859000636</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589192.6913992986</X><Y>1223501.0668307021</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589214.9862992987</X><Y>1223452.7173307016</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2570993.8017322505</X><Y>1215079.8878403557</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589214.9862992987</X><Y>1223452.7173307016</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589216.7893992998</X><Y>1223448.6313306987</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589216.7893992998</X><Y>1223448.6313306987</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589208.2162992992</X><Y>1223445.8643307015</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589208.2162992992</X><Y>1223445.8643307015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589178.1859993003</X><Y>1223511.1900307015</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589178.1859993003</X><Y>1223511.1900307015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589171.7257992998</X><Y>1223516.8300307021</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589166.0802769405</X><Y>1223503.8436662396</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589171.7257992998</X><Y>1223516.8300307021</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589163.1456992999</X><Y>1223517.0501307026</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589167.1749203168</X><Y>1223506.7722535506</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589163.1456992999</X><Y>1223517.0501307026</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589159.3955992982</X><Y>1223515.6300306991</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589159.3955992982</X><Y>1223515.6300306991</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589152.5214992985</X><Y>1223513.028030701</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589152.5214992985</X><Y>1223513.028030701</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589151.7455992997</X><Y>1223509.1200307012</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589153.4789024387</X><Y>1223510.8069223338</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589151.7455992997</X><Y>1223509.1200307012</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589127.6391992979</X><Y>1223499.9770307019</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589127.6391992979</X><Y>1223499.9770307019</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589123.3860992976</X><Y>1223501.9750306979</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589124.3300921582</X><Y>1223498.4587465259</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589123.3860992976</X><Y>1223501.9750306979</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589086.7975993007</X><Y>1223488.2130307034</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589086.7975993007</X><Y>1223488.2130307034</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589014.935599301</X><Y>1223460.9819307029</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589014.935599301</X><Y>1223460.9819307029</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588984.1881993003</X><Y>1223449.3759306967</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588984.1881993003</X><Y>1223449.3759306967</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588982.4781992994</X><Y>1223445.0159306973</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588985.7654602751</X><Y>1223446.2419934792</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588982.4781992994</X><Y>1223445.0159306973</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588973.6180992983</X><Y>1223441.6659307033</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588973.6180992983</X><Y>1223441.6659307033</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588969.2179992981</X><Y>1223443.7159307003</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588970.4301685416</X><Y>1223440.5705530806</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588969.2179992981</X><Y>1223443.7159307003</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588870.4115993008</X><Y>1223406.2918306962</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588870.4115993008</X><Y>1223406.2918306962</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588868.4115993008</X><Y>1223405.5318306983</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588868.4115993008</X><Y>1223405.5318306983</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588804.3826992996</X><Y>1223381.3198307008</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588804.3826992996</X><Y>1223381.3198307008</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588804.3406993002</X><Y>1223381.3038306981</Y></ToPoint><CenterPoint xsi:nil='true'/><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>true</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588804.3406993002</X><Y>1223381.3038306981</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588791.1104993001</X><Y>1223376.2998306975</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588791.1104993001</X><Y>1223376.2998306975</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588746.7948992997</X><Y>1223359.5367306992</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588746.7948992997</X><Y>1223359.5367306992</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588741.0037993006</X><Y>1223357.3467307016</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588741.0037993006</X><Y>1223357.3467307016</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588718.9714992978</X><Y>1223349.0127307028</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588718.9714992978</X><Y>1223349.0127307028</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588696.555199299</X><Y>1223340.568730697</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588696.555199299</X><Y>1223340.568730697</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588682.2379992977</X><Y>1223335.1827306971</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588682.2379992977</X><Y>1223335.1827306971</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588668.0787992999</X><Y>1223330.0857307017</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588668.0787992999</X><Y>1223330.0857307017</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588657.7296992987</X><Y>1223326.3467307016</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588657.7296992987</X><Y>1223326.3467307016</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588630.557299301</X><Y>1223316.9836307019</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588630.557299301</X><Y>1223316.9836307019</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588614.6650993004</X><Y>1223311.6696306989</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588614.6650993004</X><Y>1223311.6696306989</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588598.5117992982</X><Y>1223306.1656306982</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588598.5117992982</X><Y>1223306.1656306982</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588584.5105993003</X><Y>1223301.1416307017</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588584.5105993003</X><Y>1223301.1416307017</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588571.1683993004</X><Y>1223295.9796307012</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588571.1683993004</X><Y>1223295.9796307012</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588557.7672992982</X><Y>1223290.5166307017</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588557.7672992982</X><Y>1223290.5166307017</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588543.815099299</X><Y>1223284.5146306977</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588543.815099299</X><Y>1223284.5146306977</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588530.0338992998</X><Y>1223278.2126306966</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588530.0338992998</X><Y>1223278.2126306966</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588517.492699299</X><Y>1223272.2115307003</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588517.492699299</X><Y>1223272.2115307003</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588504.7815992981</X><Y>1223265.8095306978</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588504.7815992981</X><Y>1223265.8095306978</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588491.6203993</X><Y>1223258.8775307015</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588491.6203993</X><Y>1223258.8775307015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588480.5492992997</X><Y>1223252.716530703</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588480.5492992997</X><Y>1223252.716530703</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588468.5381992981</X><Y>1223245.8154307008</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588468.5381992981</X><Y>1223245.8154307008</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588462.2280992977</X><Y>1223242.0544307008</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588462.2280992977</X><Y>1223242.0544307008</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588455.5769992992</X><Y>1223237.8044307008</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588455.5769992992</X><Y>1223237.8044307008</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588448.0468993001</X><Y>1223232.6434307024</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588448.0468993001</X><Y>1223232.6434307024</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588444.9358992986</X><Y>1223230.2933306992</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588444.9358992986</X><Y>1223230.2933306992</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588440.2698992975</X><Y>1223225.5483307019</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588451.7468060534</X><Y>1223218.9291617956</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588440.2698992975</X><Y>1223225.5483307019</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588438.0757992975</X><Y>1223229.212330699</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588438.0757992975</X><Y>1223229.212330699</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588433.3325993009</X><Y>1223237.3874306977</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588433.3325993009</X><Y>1223237.3874306977</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588433.4775993004</X><Y>1223237.4374307021</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588432.5403803601</X><Y>1223239.9201153961</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588433.4775993004</X><Y>1223237.4374307021</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588438.2656993009</X><Y>1223239.7534307018</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588438.2656993009</X><Y>1223239.7534307018</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588442.1056993008</X><Y>1223241.7335307002</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588442.1056993008</X><Y>1223241.7335307002</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588457.7668992989</X><Y>1223250.5055307001</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588457.7668992989</X><Y>1223250.5055307001</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588467.6979992986</X><Y>1223256.3665307015</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588467.6979992986</X><Y>1223256.3665307015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588495.2542992979</X><Y>1223271.6526307017</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588779.0324089085</X><Y>1222727.6039863094</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588495.2542992979</X><Y>1223271.6526307017</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588560.2201992981</X><Y>1223302.0577306971</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588839.1678837165</X><Y>1222621.4282800918</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588560.2201992981</X><Y>1223302.0577306971</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588637.7182992995</X><Y>1223329.4357307032</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589133.4939467069</X><Y>1221802.6833789665</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588637.7182992995</X><Y>1223329.4357307032</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588646.2893992998</X><Y>1223336.0568306968</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588635.6839195788</X><Y>1223340.9275118676</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588646.2893992998</X><Y>1223336.0568306968</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588646.368299298</X><Y>1223344.2518306971</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588634.606463898</X><Y>1223340.2671917325</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588646.368299298</X><Y>1223344.2518306971</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588650.931299299</X><Y>1223345.7398307025</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588650.931299299</X><Y>1223345.7398307025</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588651.000299301</X><Y>1223345.6278306991</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588651.9872950362</X><Y>1223346.3131450373</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588651.000299301</X><Y>1223345.6278306991</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588653.7003992982</X><Y>1223341.525830701</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588653.7003992982</X><Y>1223341.525830701</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588654.2554992996</X><Y>1223340.6758306995</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588654.2554992996</X><Y>1223340.6758306995</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588655.9534992985</X><Y>1223341.7808306962</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588655.1081571938</X><Y>1223341.2227097889</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588655.9534992985</X><Y>1223341.7808306962</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588655.3474992998</X><Y>1223342.6208306998</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588655.3474992998</X><Y>1223342.6208306998</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588652.9133992977</X><Y>1223346.3858307004</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588652.9133992977</X><Y>1223346.3858307004</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588659.1424992979</X><Y>1223348.4178306982</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588659.1424992979</X><Y>1223348.4178306982</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588683.806899298</X><Y>1223345.8418307006</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588674.2820664812</X><Y>1223374.0094996477</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588683.806899298</X><Y>1223345.8418307006</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588696.1250992976</X><Y>1223350.4988306984</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588696.1250992976</X><Y>1223350.4988306984</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588959.0436992981</X><Y>1223450.0340306982</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588959.0436992981</X><Y>1223450.0340306982</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588963.7057993002</X><Y>1223453.7640307024</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588955.3936293628</X><Y>1223459.3747892089</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588963.7057993002</X><Y>1223453.7640307024</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588984.2280992977</X><Y>1223461.3570306972</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588984.2280992977</X><Y>1223461.3570306972</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588993.3571993001</X><Y>1223464.8250306994</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588993.3571993001</X><Y>1223464.8250306994</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588993.0071992986</X><Y>1223465.8150307015</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588993.0071992986</X><Y>1223465.8150307015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588998.2671993002</X><Y>1223467.8050306961</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588998.2671993002</X><Y>1223467.8050306961</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588998.6172992997</X><Y>1223466.8150307015</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588998.6172992997</X><Y>1223466.8150307015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589016.2334992997</X><Y>1223473.4800307006</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589016.2334992997</X><Y>1223473.4800307006</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589060.5730993003</X><Y>1223490.2571306974</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589060.5730993003</X><Y>1223490.2571306974</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589147.8782992996</X><Y>1223523.2891307026</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589147.8782992996</X><Y>1223523.2891307026</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589158.1354992986</X><Y>1223527.1702307016</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589158.1354992986</X><Y>1223527.1702307016</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589162.8954993002</X><Y>1223531.3312307</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589153.031775997</X><Y>1223537.8117792334</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589162.8954993002</X><Y>1223531.3312307</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589164.8154992983</X><Y>1223537.0912306979</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589153.0108345575</X><Y>1223537.8261189437</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589164.8154992983</X><Y>1223537.0912306979</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589173.7556992993</X><Y>1223536.4712307006</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589173.7556992993</X><Y>1223536.4712307006</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589174.1256993003</X><Y>1223534.5012307018</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589174.1256993003</X><Y>1223534.5012307018</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589175.9056992978</X><Y>1223532.3612307012</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589176.7069073915</X><Y>1223534.8379364957</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589175.9056992978</X><Y>1223532.3612307012</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589178.5757992975</X><Y>1223533.031230703</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589176.7033754745</X><Y>1223534.8377856892</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589178.5757992975</X><Y>1223533.031230703</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589180.1357992999</X><Y>1223534.1212306991</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589185.0767099243</X><Y>1223525.388505361</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589180.1357992999</X><Y>1223534.1212306991</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589181.9357993007</X><Y>1223534.9212306961</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589185.1016453761</X><Y>1223525.3730769888</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589181.9357993007</X><Y>1223534.9212306961</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589184.6588992998</X><Y>1223535.5832306966</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589184.6588992998</X><Y>1223535.5832306966</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589199.9350993</X><Y>1223539.2992307022</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589199.9350993</X><Y>1223539.2992307022</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589200.7560992986</X><Y>1223539.4992306978</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589200.7560992986</X><Y>1223539.4992306978</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589220.6563992985</X><Y>1223544.3251307011</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589220.6563992985</X><Y>1223544.3251307011</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589276.3472993001</X><Y>1223557.8261307031</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589276.3472993001</X><Y>1223557.8261307031</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589343.5912992992</X><Y>1223574.1411307007</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589343.5912992992</X><Y>1223574.1411307007</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589343.8212992996</X><Y>1223573.261130698</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589343.8212992996</X><Y>1223573.261130698</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589347.9303992987</X><Y>1223573.711130701</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589347.9303992987</X><Y>1223573.711130701</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589482.6144993007</X><Y>1223605.5370306969</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589482.6144993007</X><Y>1223605.5370306969</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589485.2314992994</X><Y>1223606.6100307032</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589485.2314992994</X><Y>1223606.6100307032</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589486.6714993007</X><Y>1223610.4700307027</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589486.6714993007</X><Y>1223610.4700307027</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589474.0999993011</X><Y>1223725.7251306996</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589474.0999993011</X><Y>1223725.7251306996</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589473.5698992983</X><Y>1223732.1052306965</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589473.5698992983</X><Y>1223732.1052306965</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589476.0608992986</X><Y>1223737.9292306975</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589476.0608992986</X><Y>1223737.9292306975</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589478.5709992982</X><Y>1223734.7192306966</Y></ToPoint></Segment></SegmentArray></Ring><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589471.425299298</X><Y>1223599.8550307006</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589344.9273993</X><Y>1223569.6671307012</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589344.9273993</X><Y>1223569.6671307012</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589345.4313993007</X><Y>1223568.0300306976</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589345.4313993007</X><Y>1223568.0300306976</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589345.8813992999</X><Y>1223566.3800306991</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589345.8813992999</X><Y>1223566.3800306991</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589341.1563992985</X><Y>1223565.2330306992</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589341.1563992985</X><Y>1223565.2330306992</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589336.8432992995</X><Y>1223564.1860307008</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589336.8432992995</X><Y>1223564.1860307008</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589334.9872993007</X><Y>1223563.7360306978</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589334.9872993007</X><Y>1223563.7360306978</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589301.0016992986</X><Y>1223555.4870306998</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589301.0016992986</X><Y>1223555.4870306998</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589299.9696993008</X><Y>1223556.7131306976</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589299.9696993008</X><Y>1223556.7131306976</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589292.0875992998</X><Y>1223554.0500307009</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589292.0875992998</X><Y>1223554.0500307009</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589290.8695992976</X><Y>1223553.028030701</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589297.3000169322</X><Y>1223546.6011767955</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589290.8695992976</X><Y>1223553.028030701</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589203.3551992998</X><Y>1223531.7221307009</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589203.3551992998</X><Y>1223531.7221307009</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589202.7164992988</X><Y>1223505.9568307027</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589202.7164992988</X><Y>1223505.9568307027</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589204.5165992975</X><Y>1223498.8858307004</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589216.1280056895</X><Y>1223505.6064350191</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589204.5165992975</X><Y>1223498.8858307004</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589213.1569993012</X><Y>1223483.7126306966</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589213.1569993012</X><Y>1223483.7126306966</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589228.4464992993</X><Y>1223457.276330702</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589228.4464992993</X><Y>1223457.276330702</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589232.3355992995</X><Y>1223456.4173306972</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589230.9156036</X><Y>1223459.2217365429</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589232.3355992995</X><Y>1223456.4173306972</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589241.3358993009</X><Y>1223441.082230702</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589241.3358993009</X><Y>1223441.082230702</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589247.7560992986</X><Y>1223440.7221307009</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589247.7560992986</X><Y>1223440.7221307009</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589311.2669993006</X><Y>1223459.2081307024</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589814.9478911459</X><Y>1221610.4084953133</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589311.2669993006</X><Y>1223459.2081307024</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589375.3889992982</X><Y>1223475.4861306995</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589812.640319753</X><Y>1221618.6406178421</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589375.3889992982</X><Y>1223475.4861306995</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589434.063899301</X><Y>1223488.5061307028</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589819.8987877993</X><Y>1221611.0133780681</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589434.063899301</X><Y>1223488.5061307028</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589493.1087993011</X><Y>1223499.6770306975</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589819.6513781631</X><Y>1221612.0745513882</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589493.1087993011</X><Y>1223499.6770306975</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589516.6881993003</X><Y>1223504.4460306987</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589516.6881993003</X><Y>1223504.4460306987</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589521.9382992983</X><Y>1223508.4560306966</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589521.9382992983</X><Y>1223508.4560306966</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589522.1711992994</X><Y>1223516.4471307024</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589522.1711992994</X><Y>1223516.4471307024</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589512.423599299</X><Y>1223547.5024306998</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589512.423599299</X><Y>1223547.5024306998</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589492.9007993005</X><Y>1223596.2599307001</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589492.9007993005</X><Y>1223596.2599307001</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589486.5315992981</X><Y>1223601.3100306988</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589485.5724365781</X><Y>1223593.5588557634</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589486.5315992981</X><Y>1223601.3100306988</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589472.4292992987</X><Y>1223600.0950307027</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589472.4292992987</X><Y>1223600.0950307027</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589471.425299298</X><Y>1223599.8550307006</Y></ToPoint></Segment></SegmentArray></Ring><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588684.1879993007</X><Y>1223340.4907307029</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588683.5969992988</X><Y>1223342.0387307033</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588683.8863865733</X><Y>1223341.2623969684</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588683.5969992988</X><Y>1223342.0387307033</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588682.932999298</X><Y>1223341.7867306992</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588682.932999298</X><Y>1223341.7867306992</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588683.5409993008</X><Y>1223340.2467307001</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588683.5409993008</X><Y>1223340.2467307001</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588684.1879993007</X><Y>1223340.4907307029</Y></ToPoint></Segment></SegmentArray></Ring><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588643.433399301</X><Y>1223325.8647307009</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588644.0883992985</X><Y>1223326.0897307023</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588644.0883992985</X><Y>1223326.0897307023</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588643.5423992984</X><Y>1223327.6337307021</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588643.5423992984</X><Y>1223327.6337307021</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588642.864399299</X><Y>1223327.4007306993</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588642.864399299</X><Y>1223327.4007306993</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588643.433399301</X><Y>1223325.8647307009</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588643.140442038</X><Y>1223326.6295977689</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment></SegmentArray></Ring><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588438.4847992994</X><Y>1223230.4764306992</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588442.3457993008</X><Y>1223234.0434307009</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588442.3457993008</X><Y>1223234.0434307009</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588445.1657993011</X><Y>1223236.5134306997</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588445.1657993011</X><Y>1223236.5134306997</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588447.2538992986</X><Y>1223238.2374306992</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588447.2538992986</X><Y>1223238.2374306992</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588446.7017992996</X><Y>1223239.0654307008</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588446.9362047925</X><Y>1223238.623662666</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588446.7017992996</X><Y>1223239.0654307008</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588444.2457992993</X><Y>1223237.7634306997</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588444.2457992993</X><Y>1223237.7634306997</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588440.8857992999</X><Y>1223236.0534306988</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2588440.8857992999</X><Y>1223236.0534306988</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588436.1046992987</X><Y>1223233.6914307028</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588436.1046992987</X><Y>1223233.6914307028</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588435.9276992977</X><Y>1223232.9404307008</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588436.2935693595</X><Y>1223233.25055853</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588435.9276992977</X><Y>1223232.9404307008</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588437.7017992996</X><Y>1223230.6134307012</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588415.7118128515</X><Y>1223215.6880954637</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2588437.7017992996</X><Y>1223230.6134307012</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2588438.4847992994</X><Y>1223230.4764306992</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2588438.1451927284</X><Y>1223230.8415186882</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment></SegmentArray></Ring><Ring xsi:type='typens:Ring'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589182.8859992996</X><Y>1223516.5200306997</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589182.4659992978</X><Y>1223517.4400307015</Y></ToPoint></Segment><Segment xsi:type='typens:Line'><FromPoint xsi:type='typens:PointN'><X>2589182.4659992978</X><Y>1223517.4400307015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589180.5959993005</X><Y>1223516.6100307032</Y></ToPoint></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589180.5959993005</X><Y>1223516.6100307032</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589181.0059993006</X><Y>1223515.6900307015</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589093.0799369463</X><Y>1223477.056948628</Y></CenterPoint><IsCounterClockwise>false</IsCounterClockwise><IsMinor>false</IsMinor><IsLine>false</IsLine></Segment><Segment xsi:type='typens:CircularArc'><FromPoint xsi:type='typens:PointN'><X>2589181.0059993006</X><Y>1223515.6900307015</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>2589182.8859992996</X><Y>1223516.5200306997</Y></ToPoint><CenterPoint xsi:type='typens:PointN'><X>2589181.9480605307</X><Y>1223516.1003618888</Y></CenterPoint><IsCounterClockwise>true</IsCounterClockwise><IsMinor>true</IsMinor><IsLine>false</IsLine></Segment></SegmentArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-29386399.999800701</XOrigin><YOrigin>-33067899.9997693</YOrigin><XYScale>10000</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID></SpatialReference></PolygonN>";

			var lv03Polygon = (IPolygon) FromXmlString(lv03PolygonXml);
			var lv95FineltraPolygon = (IPolygon) FromXmlString(lv95FineltraPolygonXml);

			// Note: the polygon has linear CircularArcs (IsLine == true)

			ReproProjectIssues(lv03Polygon, lv95FineltraPolygon);
		}

		[Test]
		public void Repro_IRelationalOperator_Equals_ReturnsCorrectResult_Polygon()
		{
			// this is to confirm that NIM088082 does not affect polygons
			// (only polylines were found to be affected so far)
			// Succeeds in 10.2.2

			// polygon1: 
			// 
			//        x
			//      / |
			//    /   |
			//   x----x
			// 

			IPolyline polyline1 = CreatePolyline(0, 0,
			                                     10, 0,
			                                     10, 10);

			IPolygon polygon1 = GeometryFactory.CreatePolygon(polyline1);
			polygon1.Close();
			GeometryUtils.Simplify(polygon1);

			// polygon2:
			//
			//   x----x
			//   |    |
			//   |    |
			//   x----x
			// 

			IPolyline polyline2 = CreatePolyline(0, 0,
			                                     10, 0,
			                                     10, 10,
			                                     0, 10);

			IPolygon polygon2 = GeometryFactory.CreatePolygon(polyline2);
			polygon2.Close();
			GeometryUtils.Simplify(polygon2);

			// symmetric difference is not empty (expected)
			IGeometry difference =
				((ITopologicalOperator) polygon1).SymmetricDifference(polygon2);
			Assert.IsFalse(difference.IsEmpty, "difference is empty");

			// IClone.IsEqual() returns false (expected)
			Assert.IsFalse(((IClone) polygon1).IsEqual((IClone) polygon2),
			               "IClone.IsEqual() returned true");

			// test workaround (should detect difference)
			Assert.IsFalse(GeometryUtils.AreEqualInXY(polygon1, polygon2));

			// IRelationalOperator.Equals returns false (expected):
			Assert.IsFalse(((IRelationalOperator) polygon1).Equals(polygon2),
			               "IRelationalOperator.Equals() returned true");
		}

		[Test]
		public void Repro_IRelationalOperator_Equals_ReturnsIncorrectResult_PolylineZ()
		{
			// NIM088082 
			// this test fails starting with ArcGIS 10.1
			// Succeeds in 10.2

			// polyline1 (z-aware, z=constant=0): 
			// 
			//        x
			//        |
			//        |
			//   x----x
			// 

			IPolyline polyline1 = CreatePolylineZ(0, 0, 0,
			                                      10, 0, 0,
			                                      10, 10, 0);

			// polyline2 (z-aware, z=constant=0):
			//
			//   x----x
			//   |
			//   |
			//   x
			// 

			IPolyline polyline2 = CreatePolylineZ(0, 0, 0,
			                                      0, 10, 0,
			                                      10, 10, 0);

			// symmetric difference is not empty (expected)
			IGeometry difference =
				((ITopologicalOperator) polyline1).SymmetricDifference(polyline2);
			Assert.IsFalse(difference.IsEmpty, "difference is empty");

			// IClone.IsEqual() returns false (expected)
			Assert.IsFalse(((IClone) polyline1).IsEqual((IClone) polyline2),
			               "IClone.IsEqual() returned true");

			// test workaround (should detect difference)
			Assert.IsFalse(GeometryUtils.AreEqualInXY(polyline1, polyline2));

			// IRelationalOperator.Equals incorrectly returns true starting with ArcGIS 10.1:
			Assert.IsFalse(((IRelationalOperator) polyline1).Equals(polyline2),
			               "IRelationalOperator.Equals() returned true");
		}

		[Test]
		public void Repro_IRelationalOperator_Equals_ReturnsIncorrectResult_Polyline()
		{
			// NIM088082
			// Fails in ArcGIS 10.1, but succeeds in ArcGIS 10.0
			// Succeeds in 10.2

			// polyline:
			// 
			//        x
			//        |
			//        |
			//   x----x
			// (Start)
			IPolyline polyline = CreatePolyline(0, 0,
			                                    10, 0,
			                                    10, 10);

			// polyline1:
			//
			//   x----x
			//   |
			//   |
			//   x (Start)

			IPolyline polyline1 = CreatePolyline(0, 0,
			                                     0, 10,
			                                     10, 10);

			bool equals1 = ((IRelationalOperator) polyline).Equals(polyline1);
			Console.WriteLine(
				@"same envelope, same end points, same point count, but different interior vertices: Equals() returns {0}",
				equals1);

			// polyline2:
			//
			//        x
			//      /
			//     /
			//   x (Start)

			IPolyline polyline2 = CreatePolyline(0, 0,
			                                     10, 10);

			bool equals2 = ((IRelationalOperator) polyline).Equals(polyline2);
			Console.WriteLine(
				@"same envelope, same end points, different point count: Equals() returns {0}",
				equals2);

			// polyline3:
			//
			//   x----x (Start)
			//   |
			//   |
			//   x

			IPolyline polyline3 = CreatePolyline(10, 10,
			                                     0, 10,
			                                     0, 0);

			bool equals3 = ((IRelationalOperator) polyline).Equals(polyline3);
			Console.WriteLine(
				@"same envelope, flipped end points, different interior vertices, same point count: Equals() returns {0}",
				equals3);

			// polyline3:
			//
			//   x-
			//   |  \
			//   |   -x
			//   x

			IPolyline polyline4 = CreatePolyline(0, 0,
			                                     0, 10,
			                                     10, 5);

			bool equals4 = ((IRelationalOperator) polyline).Equals(polyline4);
			Console.WriteLine(
				@"same envelope, different end points, same point count: Equals() returns {0}",
				equals4);

			// test workaround (should detect difference)
			Assert.IsFalse(GeometryUtils.AreEqualInXY(polyline, polyline1));
			Assert.IsFalse(GeometryUtils.AreEqualInXY(polyline, polyline2));
			Assert.IsFalse(GeometryUtils.AreEqualInXY(polyline, polyline3));
			Assert.IsFalse(GeometryUtils.AreEqualInXY(polyline, polyline4));

			// this one fails starting with ArcGIS 10.1:
			Assert.IsFalse(equals1, "equals1");
			Assert.IsFalse(equals2, "equals2");
			Assert.IsFalse(equals3, "equals3");
			Assert.IsFalse(equals4, "equals4");

			// conclusion: it seems that polylines are treated as equal as soon as their envelopes are equal
			//             (observed at least for single-part polylines)
		}

		[Test]
		public void LearningTestPolylineIsEqual()
		{
			// polyline1: 
			// 
			//        x z=0
			//        |
			//        |
			//   x----x z=0
			//   z=0  
			IPolyline polyline1 = CreatePolylineZ(0, 0, 0,
			                                      10, 0, 0,
			                                      10, 10, 0);

			// polyline2: 
			// 
			//        x z=0
			//        |
			//        |
			//   x----x z=0
			//   z=1  
			IPolyline polyline2 = CreatePolylineZ(0, 0, 1,
			                                      10, 0, 0,
			                                      10, 10, 0);

			// IClone.IsEqual() is sensitive to z-only differences
			Assert.IsFalse(((IClone) polyline1).IsEqual((IClone) polyline2),
			               "IClone.IsEqual() returned true");

			// IRelationalOperator.Equals() ignores z-only differences
			Assert.IsTrue(((IRelationalOperator) polyline1).Equals(polyline2),
			              "IRelationalOperator.Equals() returned false");
		}

		/// <summary>
		/// Repro case for the Clip()/QueryClipped() exception in ArcGIS 10.0, when a vertex is very close to the boundary.
		/// </summary>
		[Test]
		public void ReproClipWithVertexNearEnvelope()
		{
			// Fails in 10.2
			// Succeeds in 10.2.2

			// failing polygon
			var polygon = (IPolygon) FromXmlString(
				"<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>32793643.734899998</XMin><YMin>5441054.0731000006</YMin><XMax>32795422.098299999</XMax><YMax>5442041.6487000007</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32795227.480599999</X><Y>5441899.5470000003</Y></Point><Point xsi:type='typens:PointN'><X>32795174.235300001</X><Y>5441850.8463000003</Y></Point><Point xsi:type='typens:PointN'><X>32795182.5264</X><Y>5441834.0930000003</Y></Point><Point xsi:type='typens:PointN'><X>32795199.3693</X><Y>5441841.5446000006</Y></Point><Point xsi:type='typens:PointN'><X>32795258.216899998</X><Y>5441892.9964000005</Y></Point><Point xsi:type='typens:PointN'><X>32795281.654100001</X><Y>5441899.9757000003</Y></Point><Point xsi:type='typens:PointN'><X>32795319.254799999</X><Y>5441830.0125999991</Y></Point><Point xsi:type='typens:PointN'><X>32795212.3915</X><Y>5441772.5091999993</Y></Point><Point xsi:type='typens:PointN'><X>32795219.925999999</X><Y>5441740.1579999998</Y></Point><Point xsi:type='typens:PointN'><X>32795251.5636</X><Y>5441741.8465</Y></Point><Point xsi:type='typens:PointN'><X>32795341.8717</X><Y>5441679.6561999992</Y></Point><Point xsi:type='typens:PointN'><X>32795363.9593</X><Y>5441707.9497999996</Y></Point><Point xsi:type='typens:PointN'><X>32795406.203200001</X><Y>5441702.9059999995</Y></Point><Point xsi:type='typens:PointN'><X>32795422.098299999</X><Y>5441688.1972000003</Y></Point><Point xsi:type='typens:PointN'><X>32795388.330499999</X><Y>5441632.1653000005</Y></Point><Point xsi:type='typens:PointN'><X>32795339.213300001</X><Y>5441652.4890000001</Y></Point><Point xsi:type='typens:PointN'><X>32795320.734700002</X><Y>5441625.0457000006</Y></Point><Point xsi:type='typens:PointN'><X>32795196.657400001</X><Y>5441615.4031000007</Y></Point><Point xsi:type='typens:PointN'><X>32795033.660700001</X><Y>5441666.2963999994</Y></Point><Point xsi:type='typens:PointN'><X>32795032.5825</X><Y>5441646.0008000005</Y></Point><Point xsi:type='typens:PointN'><X>32795097.631900001</X><Y>5441591.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32795212.388999999</X><Y>5441524.9702000003</Y></Point><Point xsi:type='typens:PointN'><X>32795138.566399999</X><Y>5441427.3739999998</Y></Point><Point xsi:type='typens:PointN'><X>32795065.968000002</X><Y>5441383.8164000008</Y></Point><Point xsi:type='typens:PointN'><X>32794873.847599998</X><Y>5441473.9607999995</Y></Point><Point xsi:type='typens:PointN'><X>32794841.837899998</X><Y>5441464.3869000003</Y></Point><Point xsi:type='typens:PointN'><X>32794838.430300001</X><Y>5441424.9773999993</Y></Point><Point xsi:type='typens:PointN'><X>32794830.0297</X><Y>5441373.5668000001</Y></Point><Point xsi:type='typens:PointN'><X>32794938.816300001</X><Y>5441309.3344000001</Y></Point><Point xsi:type='typens:PointN'><X>32794919.522799999</X><Y>5441141.7095999997</Y></Point><Point xsi:type='typens:PointN'><X>32794766.7423</X><Y>5441167.0229000002</Y></Point><Point xsi:type='typens:PointN'><X>32794733.4329</X><Y>5441054.0731000006</Y></Point><Point xsi:type='typens:PointN'><X>32794663.666000001</X><Y>5441068.1989999991</Y></Point><Point xsi:type='typens:PointN'><X>32794714.966600001</X><Y>5441221.2898999993</Y></Point><Point xsi:type='typens:PointN'><X>32794672.982099999</X><Y>5441222.3857000005</Y></Point><Point xsi:type='typens:PointN'><X>32794571.566100001</X><Y>5441206.0079999994</Y></Point><Point xsi:type='typens:PointN'><X>32794511.238899998</X><Y>5441201.2249999996</Y></Point><Point xsi:type='typens:PointN'><X>32794433.4505</X><Y>5441216.3864999991</Y></Point><Point xsi:type='typens:PointN'><X>32794274.653499998</X><Y>5441312.1027000006</Y></Point><Point xsi:type='typens:PointN'><X>32794235.395</X><Y>5441298.7367000002</Y></Point><Point xsi:type='typens:PointN'><X>32794212.237</X><Y>5441290.4397</Y></Point><Point xsi:type='typens:PointN'><X>32794230.379900001</X><Y>5441245.1228</Y></Point><Point xsi:type='typens:PointN'><X>32794201.688999999</X><Y>5441222.4119000006</Y></Point><Point xsi:type='typens:PointN'><X>32794122.3902</X><Y>5441306.5242999997</Y></Point><Point xsi:type='typens:PointN'><X>32794067.205400001</X><Y>5441232.5801999997</Y></Point><Point xsi:type='typens:PointN'><X>32794010.414700001</X><Y>5441287.9238000009</Y></Point><Point xsi:type='typens:PointN'><X>32794042.59</X><Y>5441343.3375000004</Y></Point><Point xsi:type='typens:PointN'><X>32794056.6899</X><Y>5441374.7445</Y></Point><Point xsi:type='typens:PointN'><X>32794008.651900001</X><Y>5441390.5569000002</Y></Point><Point xsi:type='typens:PointN'><X>32793903.277400002</X><Y>5441429.4452</Y></Point><Point xsi:type='typens:PointN'><X>32793827.061999999</X><Y>5441423.1009999998</Y></Point><Point xsi:type='typens:PointN'><X>32793765.724399999</X><Y>5441416.7119999994</Y></Point><Point xsi:type='typens:PointN'><X>32793741.764600001</X><Y>5441441.1714999992</Y></Point><Point xsi:type='typens:PointN'><X>32793726.771400001</X><Y>5441439.1162</Y></Point><Point xsi:type='typens:PointN'><X>32793727.339200001</X><Y>5441416.5571999997</Y></Point><Point xsi:type='typens:PointN'><X>32793743.148499999</X><Y>5441397.3149999995</Y></Point><Point xsi:type='typens:PointN'><X>32793694.3785</X><Y>5441388.8638000004</Y></Point><Point xsi:type='typens:PointN'><X>32793671.623999998</X><Y>5441410.7687999997</Y></Point><Point xsi:type='typens:PointN'><X>32793643.734899998</X><Y>5441411.2943999991</Y></Point><Point xsi:type='typens:PointN'><X>32793664.0374</X><Y>5441460.0019000005</Y></Point><Point xsi:type='typens:PointN'><X>32793712.9045</X><Y>5441448.2647999991</Y></Point><Point xsi:type='typens:PointN'><X>32793802.362599999</X><Y>5441474.0327000003</Y></Point><Point xsi:type='typens:PointN'><X>32793893.398699999</X><Y>5441460.8483000007</Y></Point><Point xsi:type='typens:PointN'><X>32793911.490899999</X><Y>5441491.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32793842.899099998</X><Y>5441545.7807999998</Y></Point><Point xsi:type='typens:PointN'><X>32793883.445</X><Y>5441570.8693000004</Y></Point><Point xsi:type='typens:PointN'><X>32793916.962899998</X><Y>5441539.2399000004</Y></Point><Point xsi:type='typens:PointN'><X>32793930.925999999</X><Y>5441522.909</Y></Point><Point xsi:type='typens:PointN'><X>32793942.5953</X><Y>5441540.1004000008</Y></Point><Point xsi:type='typens:PointN'><X>32793987.820299998</X><Y>5441513.3838</Y></Point><Point xsi:type='typens:PointN'><X>32794007.344499998</X><Y>5441534.5032000002</Y></Point><Point xsi:type='typens:PointN'><X>32794051.809100002</X><Y>5441503.2223000005</Y></Point><Point xsi:type='typens:PointN'><X>32794030.462499999</X><Y>5441478.4956</Y></Point><Point xsi:type='typens:PointN'><X>32794081.765299998</X><Y>5441460.6392999999</Y></Point><Point xsi:type='typens:PointN'><X>32794125.6472</X><Y>5441430.8323999997</Y></Point><Point xsi:type='typens:PointN'><X>32794165.025399998</X><Y>5441409.8354000002</Y></Point><Point xsi:type='typens:PointN'><X>32794181.6032</X><Y>5441428.2355000004</Y></Point><Point xsi:type='typens:PointN'><X>32794206.094900001</X><Y>5441458.5461999997</Y></Point><Point xsi:type='typens:PointN'><X>32794185.984000001</X><Y>5441479.7317999993</Y></Point><Point xsi:type='typens:PointN'><X>32794182.932700001</X><Y>5441503.6409000009</Y></Point><Point xsi:type='typens:PointN'><X>32794193.656500001</X><Y>5441524.8278999999</Y></Point><Point xsi:type='typens:PointN'><X>32794231.297899999</X><Y>5441508.2523999996</Y></Point><Point xsi:type='typens:PointN'><X>32794239.458999999</X><Y>5441525.7037000004</Y></Point><Point xsi:type='typens:PointN'><X>32794244.9421</X><Y>5441554.6692999993</Y></Point><Point xsi:type='typens:PointN'><X>32794226.599600002</X><Y>5441562.5756999999</Y></Point><Point xsi:type='typens:PointN'><X>32794119.367600001</X><Y>5441604.8714000005</Y></Point><Point xsi:type='typens:PointN'><X>32794171.8814</X><Y>5441691.6239999998</Y></Point><Point xsi:type='typens:PointN'><X>32794248.446600001</X><Y>5441667.2337999996</Y></Point><Point xsi:type='typens:PointN'><X>32794427.070299998</X><Y>5441685.9543999992</Y></Point><Point xsi:type='typens:PointN'><X>32794548.1261</X><Y>5441655.0434000008</Y></Point><Point xsi:type='typens:PointN'><X>32794568.2544</X><Y>5441638.6411000006</Y></Point><Point xsi:type='typens:PointN'><X>32794550.533</X><Y>5441602.1360999998</Y></Point><Point xsi:type='typens:PointN'><X>32794611.829800002</X><Y>5441595.7828000002</Y></Point><Point xsi:type='typens:PointN'><X>32794624.5854</X><Y>5441611.5179999992</Y></Point><Point xsi:type='typens:PointN'><X>32794648.952799998</X><Y>5441607.8008999992</Y></Point><Point xsi:type='typens:PointN'><X>32794676.268300001</X><Y>5441633.8512999993</Y></Point><Point xsi:type='typens:PointN'><X>32794700.6818</X><Y>5441629.1338999998</Y></Point><Point xsi:type='typens:PointN'><X>32794720.659400001</X><Y>5441632.2041999996</Y></Point><Point xsi:type='typens:PointN'><X>32794694.076400001</X><Y>5441688.3705000002</Y></Point><Point xsi:type='typens:PointN'><X>32794681.592700001</X><Y>5441713.4598999992</Y></Point><Point xsi:type='typens:PointN'><X>32794642.5559</X><Y>5441718.4124999996</Y></Point><Point xsi:type='typens:PointN'><X>32794638.275699999</X><Y>5441741.0192000009</Y></Point><Point xsi:type='typens:PointN'><X>32794582.310199998</X><Y>5441762.3999000005</Y></Point><Point xsi:type='typens:PointN'><X>32794559.363600001</X><Y>5441772.0436000004</Y></Point><Point xsi:type='typens:PointN'><X>32794580.201299999</X><Y>5441811.4894999992</Y></Point><Point xsi:type='typens:PointN'><X>32794581.477400001</X><Y>5441844.1878999993</Y></Point><Point xsi:type='typens:PointN'><X>32794547.189100001</X><Y>5441863.4134999998</Y></Point><Point xsi:type='typens:PointN'><X>32794564.789499998</X><Y>5441883.4630999994</Y></Point><Point xsi:type='typens:PointN'><X>32794625.989</X><Y>5441874.2074999996</Y></Point><Point xsi:type='typens:PointN'><X>32794644.362099998</X><Y>5441885.3830999993</Y></Point><Point xsi:type='typens:PointN'><X>32794649.1558</X><Y>5441896.9945</Y></Point><Point xsi:type='typens:PointN'><X>32794666.908300001</X><Y>5441902.4982999992</Y></Point><Point xsi:type='typens:PointN'><X>32794667.924599998</X><Y>5441884.9332999997</Y></Point><Point xsi:type='typens:PointN'><X>32794678.877900001</X><Y>5441868.7668999992</Y></Point><Point xsi:type='typens:PointN'><X>32794666.000500001</X><Y>5441826.6018000003</Y></Point><Point xsi:type='typens:PointN'><X>32794684.857999999</X><Y>5441784.4222999997</Y></Point><Point xsi:type='typens:PointN'><X>32794687.1884</X><Y>5441756.7638000008</Y></Point><Point xsi:type='typens:PointN'><X>32794723.055100001</X><Y>5441776.1153999995</Y></Point><Point xsi:type='typens:PointN'><X>32794738.9606</X><Y>5441754.5741000008</Y></Point><Point xsi:type='typens:PointN'><X>32794737.793700002</X><Y>5441716.9556000009</Y></Point><Point xsi:type='typens:PointN'><X>32794701.966800001</X><Y>5441701.7962999996</Y></Point><Point xsi:type='typens:PointN'><X>32794721.878699999</X><Y>5441658.2577</Y></Point><Point xsi:type='typens:PointN'><X>32794777.269000001</X><Y>5441704.6049000006</Y></Point><Point xsi:type='typens:PointN'><X>32794802.555199999</X><Y>5441757.3052999992</Y></Point><Point xsi:type='typens:PointN'><X>32794788.6241</X><Y>5441842.9425000008</Y></Point><Point xsi:type='typens:PointN'><X>32794790.782099999</X><Y>5441875.4204999991</Y></Point><Point xsi:type='typens:PointN'><X>32794803.455800001</X><Y>5441892.6676000003</Y></Point><Point xsi:type='typens:PointN'><X>32794775.923999999</X><Y>5441902.7346000001</Y></Point><Point xsi:type='typens:PointN'><X>32794799.538699999</X><Y>5441957.5080999993</Y></Point><Point xsi:type='typens:PointN'><X>32794833.919</X><Y>5442010.7605000008</Y></Point><Point xsi:type='typens:PointN'><X>32794895.6996</X><Y>5442020.1275999993</Y></Point><Point xsi:type='typens:PointN'><X>32794941.989399999</X><Y>5441986.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32794922.974799998</X><Y>5441927.5124999993</Y></Point><Point xsi:type='typens:PointN'><X>32794960.9023</X><Y>5441916.5629999992</Y></Point><Point xsi:type='typens:PointN'><X>32794971.965</X><Y>5441871.1712999996</Y></Point><Point xsi:type='typens:PointN'><X>32794915.513700001</X><Y>5441851.7902000006</Y></Point><Point xsi:type='typens:PointN'><X>32794908.172399998</X><Y>5441804.7263999991</Y></Point><Point xsi:type='typens:PointN'><X>32794954.2808</X><Y>5441817.8276000004</Y></Point><Point xsi:type='typens:PointN'><X>32795005.642999999</X><Y>5441840.2140999995</Y></Point><Point xsi:type='typens:PointN'><X>32795013.368500002</X><Y>5441901.6191000007</Y></Point><Point xsi:type='typens:PointN'><X>32795021.5407</X><Y>5441953.4080999997</Y></Point><Point xsi:type='typens:PointN'><X>32795056.630599998</X><Y>5442041.6487000007</Y></Point><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point></PointArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;ETRS_1989_UTM_Zone_32N&quot;,GEOGCS[&quot;GCS_ETRS_1989&quot;,DATUM[&quot;D_ETRS_1989&quot;,SPHEROID[&quot;GRS_1980&quot;,6378137.0,298.257222101]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Transverse_Mercator&quot;],PARAMETER[&quot;False_Easting&quot;,500000.0],PARAMETER[&quot;False_Northing&quot;,0.0],PARAMETER[&quot;Central_Meridian&quot;,9.0],PARAMETER[&quot;Scale_Factor&quot;,0.9996],PARAMETER[&quot;Latitude_Of_Origin&quot;,0.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,25832]]</WKT><XOrigin>26879100</XOrigin><YOrigin>-9998100</YOrigin><XYScale>450445547.3910538</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>25832</WKID></SpatialReference></PolygonN>");

			// non-failing polygon (one vertex moved)
			// var polygon = (IPolygon) GeometryUtils.FromXmlString("<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>32793643.734899998</XMin><YMin>5441054.0731000006</YMin><XMax>32795422.098299999</XMax><YMax>5442041.6487000007</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32795227.480599999</X><Y>5441899.5470000003</Y></Point><Point xsi:type='typens:PointN'><X>32795174.235300001</X><Y>5441850.8463000003</Y></Point><Point xsi:type='typens:PointN'><X>32795182.5264</X><Y>5441834.0930000003</Y></Point><Point xsi:type='typens:PointN'><X>32795199.3693</X><Y>5441841.5446000006</Y></Point><Point xsi:type='typens:PointN'><X>32795258.216899998</X><Y>5441892.9964000005</Y></Point><Point xsi:type='typens:PointN'><X>32795281.654100001</X><Y>5441899.9757000003</Y></Point><Point xsi:type='typens:PointN'><X>32795319.254799999</X><Y>5441830.0125999991</Y></Point><Point xsi:type='typens:PointN'><X>32795212.3915</X><Y>5441772.5091999993</Y></Point><Point xsi:type='typens:PointN'><X>32795219.925999999</X><Y>5441740.1579999998</Y></Point><Point xsi:type='typens:PointN'><X>32795251.5636</X><Y>5441741.8465</Y></Point><Point xsi:type='typens:PointN'><X>32795341.8717</X><Y>5441679.6561999992</Y></Point><Point xsi:type='typens:PointN'><X>32795363.9593</X><Y>5441707.9497999996</Y></Point><Point xsi:type='typens:PointN'><X>32795406.203200001</X><Y>5441702.9059999995</Y></Point><Point xsi:type='typens:PointN'><X>32795422.098299999</X><Y>5441688.1972000003</Y></Point><Point xsi:type='typens:PointN'><X>32795388.330499999</X><Y>5441632.1653000005</Y></Point><Point xsi:type='typens:PointN'><X>32795339.213300001</X><Y>5441652.4890000001</Y></Point><Point xsi:type='typens:PointN'><X>32795320.734700002</X><Y>5441625.0457000006</Y></Point><Point xsi:type='typens:PointN'><X>32795196.657400001</X><Y>5441615.4031000007</Y></Point><Point xsi:type='typens:PointN'><X>32795033.660700001</X><Y>5441666.2963999994</Y></Point><Point xsi:type='typens:PointN'><X>32795032.5825</X><Y>5441646.0008000005</Y></Point><Point xsi:type='typens:PointN'><X>32795097.631900001</X><Y>5441591.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32795212.388999999</X><Y>5441524.9702000003</Y></Point><Point xsi:type='typens:PointN'><X>32795138.566399999</X><Y>5441427.3739999998</Y></Point><Point xsi:type='typens:PointN'><X>32795065.968000002</X><Y>5441383.8164000008</Y></Point><Point xsi:type='typens:PointN'><X>32794873.847599998</X><Y>5441473.9607999995</Y></Point><Point xsi:type='typens:PointN'><X>32794841.837899998</X><Y>5441464.3869000003</Y></Point><Point xsi:type='typens:PointN'><X>32794838.430300001</X><Y>5441424.9773999993</Y></Point><Point xsi:type='typens:PointN'><X>32794830.0297</X><Y>5441373.5668000001</Y></Point><Point xsi:type='typens:PointN'><X>32794938.816300001</X><Y>5441309.3344000001</Y></Point><Point xsi:type='typens:PointN'><X>32794919.522799999</X><Y>5441141.7095999997</Y></Point><Point xsi:type='typens:PointN'><X>32794766.7423</X><Y>5441167.0229000002</Y></Point><Point xsi:type='typens:PointN'><X>32794733.4329</X><Y>5441054.0731000006</Y></Point><Point xsi:type='typens:PointN'><X>32794663.666000001</X><Y>5441068.1989999991</Y></Point><Point xsi:type='typens:PointN'><X>32794714.966600001</X><Y>5441221.2898999993</Y></Point><Point xsi:type='typens:PointN'><X>32794672.982099999</X><Y>5441222.3857000005</Y></Point><Point xsi:type='typens:PointN'><X>32794571.566100001</X><Y>5441206.0079999994</Y></Point><Point xsi:type='typens:PointN'><X>32794511.238899998</X><Y>5441201.2249999996</Y></Point><Point xsi:type='typens:PointN'><X>32794433.4505</X><Y>5441216.3864999991</Y></Point><Point xsi:type='typens:PointN'><X>32794274.653499998</X><Y>5441312.1027000006</Y></Point><Point xsi:type='typens:PointN'><X>32794235.395</X><Y>5441298.7367000002</Y></Point><Point xsi:type='typens:PointN'><X>32794212.237</X><Y>5441290.4397</Y></Point><Point xsi:type='typens:PointN'><X>32794230.379900001</X><Y>5441245.1228</Y></Point><Point xsi:type='typens:PointN'><X>32794201.688999999</X><Y>5441222.4119000006</Y></Point><Point xsi:type='typens:PointN'><X>32794122.3902</X><Y>5441306.5242999997</Y></Point><Point xsi:type='typens:PointN'><X>32794067.205400001</X><Y>5441232.5801999997</Y></Point><Point xsi:type='typens:PointN'><X>32794010.414700001</X><Y>5441287.9238000009</Y></Point><Point xsi:type='typens:PointN'><X>32794042.59</X><Y>5441343.3375000004</Y></Point><Point xsi:type='typens:PointN'><X>32794056.6899</X><Y>5441374.7445</Y></Point><Point xsi:type='typens:PointN'><X>32794008.651900001</X><Y>5441390.5569000002</Y></Point><Point xsi:type='typens:PointN'><X>32793903.277400002</X><Y>5441429.4452</Y></Point><Point xsi:type='typens:PointN'><X>32793827.061999999</X><Y>5441423.1009999998</Y></Point><Point xsi:type='typens:PointN'><X>32793765.724399999</X><Y>5441416.7119999994</Y></Point><Point xsi:type='typens:PointN'><X>32793741.764600001</X><Y>5441441.1714999992</Y></Point><Point xsi:type='typens:PointN'><X>32793726.771400001</X><Y>5441439.1162</Y></Point><Point xsi:type='typens:PointN'><X>32793727.339200001</X><Y>5441416.5571999997</Y></Point><Point xsi:type='typens:PointN'><X>32793743.148499999</X><Y>5441397.3149999995</Y></Point><Point xsi:type='typens:PointN'><X>32793694.3785</X><Y>5441388.8638000004</Y></Point><Point xsi:type='typens:PointN'><X>32793671.623999998</X><Y>5441410.7687999997</Y></Point><Point xsi:type='typens:PointN'><X>32793643.734899998</X><Y>5441411.2943999991</Y></Point><Point xsi:type='typens:PointN'><X>32793664.0374</X><Y>5441460.0019000005</Y></Point><Point xsi:type='typens:PointN'><X>32793712.9045</X><Y>5441448.2647999991</Y></Point><Point xsi:type='typens:PointN'><X>32793802.362599999</X><Y>5441474.0327000003</Y></Point><Point xsi:type='typens:PointN'><X>32793893.398699999</X><Y>5441460.8483000007</Y></Point><Point xsi:type='typens:PointN'><X>32793911.490899999</X><Y>5441491.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32793842.899099998</X><Y>5441545.7807999998</Y></Point><Point xsi:type='typens:PointN'><X>32793883.445</X><Y>5441570.8693000004</Y></Point><Point xsi:type='typens:PointN'><X>32793916.962899998</X><Y>5441539.2399000004</Y></Point><Point xsi:type='typens:PointN'><X>32793930.925999999</X><Y>5441522.909</Y></Point><Point xsi:type='typens:PointN'><X>32793942.5953</X><Y>5441540.1004000008</Y></Point><Point xsi:type='typens:PointN'><X>32793987.820299998</X><Y>5441513.3838</Y></Point><Point xsi:type='typens:PointN'><X>32794007.344499998</X><Y>5441534.5032000002</Y></Point><Point xsi:type='typens:PointN'><X>32794051.809100002</X><Y>5441503.2223000005</Y></Point><Point xsi:type='typens:PointN'><X>32794030.462499999</X><Y>5441478.4956</Y></Point><Point xsi:type='typens:PointN'><X>32794081.765299998</X><Y>5441460.6392999999</Y></Point><Point xsi:type='typens:PointN'><X>32794125.644000001</X><Y>5441430.8341000006</Y></Point><Point xsi:type='typens:PointN'><X>32794165.025399998</X><Y>5441409.8354000002</Y></Point><Point xsi:type='typens:PointN'><X>32794181.6032</X><Y>5441428.2355000004</Y></Point><Point xsi:type='typens:PointN'><X>32794206.094900001</X><Y>5441458.5461999997</Y></Point><Point xsi:type='typens:PointN'><X>32794185.984000001</X><Y>5441479.7317999993</Y></Point><Point xsi:type='typens:PointN'><X>32794182.932700001</X><Y>5441503.6409000009</Y></Point><Point xsi:type='typens:PointN'><X>32794193.656500001</X><Y>5441524.8278999999</Y></Point><Point xsi:type='typens:PointN'><X>32794231.297899999</X><Y>5441508.2523999996</Y></Point><Point xsi:type='typens:PointN'><X>32794239.458999999</X><Y>5441525.7037000004</Y></Point><Point xsi:type='typens:PointN'><X>32794244.9421</X><Y>5441554.6692999993</Y></Point><Point xsi:type='typens:PointN'><X>32794226.599600002</X><Y>5441562.5756999999</Y></Point><Point xsi:type='typens:PointN'><X>32794119.367600001</X><Y>5441604.8714000005</Y></Point><Point xsi:type='typens:PointN'><X>32794171.8814</X><Y>5441691.6239999998</Y></Point><Point xsi:type='typens:PointN'><X>32794248.446600001</X><Y>5441667.2337999996</Y></Point><Point xsi:type='typens:PointN'><X>32794427.070299998</X><Y>5441685.9543999992</Y></Point><Point xsi:type='typens:PointN'><X>32794548.1261</X><Y>5441655.0434000008</Y></Point><Point xsi:type='typens:PointN'><X>32794568.2544</X><Y>5441638.6411000006</Y></Point><Point xsi:type='typens:PointN'><X>32794550.533</X><Y>5441602.1360999998</Y></Point><Point xsi:type='typens:PointN'><X>32794611.829800002</X><Y>5441595.7828000002</Y></Point><Point xsi:type='typens:PointN'><X>32794624.5854</X><Y>5441611.5179999992</Y></Point><Point xsi:type='typens:PointN'><X>32794648.952799998</X><Y>5441607.8008999992</Y></Point><Point xsi:type='typens:PointN'><X>32794676.268300001</X><Y>5441633.8512999993</Y></Point><Point xsi:type='typens:PointN'><X>32794700.6818</X><Y>5441629.1338999998</Y></Point><Point xsi:type='typens:PointN'><X>32794720.659400001</X><Y>5441632.2041999996</Y></Point><Point xsi:type='typens:PointN'><X>32794694.076400001</X><Y>5441688.3705000002</Y></Point><Point xsi:type='typens:PointN'><X>32794681.592700001</X><Y>5441713.4598999992</Y></Point><Point xsi:type='typens:PointN'><X>32794642.5559</X><Y>5441718.4124999996</Y></Point><Point xsi:type='typens:PointN'><X>32794638.275699999</X><Y>5441741.0192000009</Y></Point><Point xsi:type='typens:PointN'><X>32794582.310199998</X><Y>5441762.3999000005</Y></Point><Point xsi:type='typens:PointN'><X>32794559.363600001</X><Y>5441772.0436000004</Y></Point><Point xsi:type='typens:PointN'><X>32794580.201299999</X><Y>5441811.4894999992</Y></Point><Point xsi:type='typens:PointN'><X>32794581.477400001</X><Y>5441844.1878999993</Y></Point><Point xsi:type='typens:PointN'><X>32794547.189100001</X><Y>5441863.4134999998</Y></Point><Point xsi:type='typens:PointN'><X>32794564.789499998</X><Y>5441883.4630999994</Y></Point><Point xsi:type='typens:PointN'><X>32794625.989</X><Y>5441874.2074999996</Y></Point><Point xsi:type='typens:PointN'><X>32794644.362099998</X><Y>5441885.3830999993</Y></Point><Point xsi:type='typens:PointN'><X>32794649.1558</X><Y>5441896.9945</Y></Point><Point xsi:type='typens:PointN'><X>32794666.908300001</X><Y>5441902.4982999992</Y></Point><Point xsi:type='typens:PointN'><X>32794667.924599998</X><Y>5441884.9332999997</Y></Point><Point xsi:type='typens:PointN'><X>32794678.877900001</X><Y>5441868.7668999992</Y></Point><Point xsi:type='typens:PointN'><X>32794666.000500001</X><Y>5441826.6018000003</Y></Point><Point xsi:type='typens:PointN'><X>32794684.857999999</X><Y>5441784.4222999997</Y></Point><Point xsi:type='typens:PointN'><X>32794687.1884</X><Y>5441756.7638000008</Y></Point><Point xsi:type='typens:PointN'><X>32794723.055100001</X><Y>5441776.1153999995</Y></Point><Point xsi:type='typens:PointN'><X>32794738.9606</X><Y>5441754.5741000008</Y></Point><Point xsi:type='typens:PointN'><X>32794737.793700002</X><Y>5441716.9556000009</Y></Point><Point xsi:type='typens:PointN'><X>32794701.966800001</X><Y>5441701.7962999996</Y></Point><Point xsi:type='typens:PointN'><X>32794721.878699999</X><Y>5441658.2577</Y></Point><Point xsi:type='typens:PointN'><X>32794777.269000001</X><Y>5441704.6049000006</Y></Point><Point xsi:type='typens:PointN'><X>32794802.555199999</X><Y>5441757.3052999992</Y></Point><Point xsi:type='typens:PointN'><X>32794788.6241</X><Y>5441842.9425000008</Y></Point><Point xsi:type='typens:PointN'><X>32794790.782099999</X><Y>5441875.4204999991</Y></Point><Point xsi:type='typens:PointN'><X>32794803.455800001</X><Y>5441892.6676000003</Y></Point><Point xsi:type='typens:PointN'><X>32794775.923999999</X><Y>5441902.7346000001</Y></Point><Point xsi:type='typens:PointN'><X>32794799.538699999</X><Y>5441957.5080999993</Y></Point><Point xsi:type='typens:PointN'><X>32794833.919</X><Y>5442010.7605000008</Y></Point><Point xsi:type='typens:PointN'><X>32794895.6996</X><Y>5442020.1275999993</Y></Point><Point xsi:type='typens:PointN'><X>32794941.989399999</X><Y>5441986.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32794922.974799998</X><Y>5441927.5124999993</Y></Point><Point xsi:type='typens:PointN'><X>32794960.9023</X><Y>5441916.5629999992</Y></Point><Point xsi:type='typens:PointN'><X>32794971.965</X><Y>5441871.1712999996</Y></Point><Point xsi:type='typens:PointN'><X>32794915.513700001</X><Y>5441851.7902000006</Y></Point><Point xsi:type='typens:PointN'><X>32794908.172399998</X><Y>5441804.7263999991</Y></Point><Point xsi:type='typens:PointN'><X>32794954.2808</X><Y>5441817.8276000004</Y></Point><Point xsi:type='typens:PointN'><X>32795005.642999999</X><Y>5441840.2140999995</Y></Point><Point xsi:type='typens:PointN'><X>32795013.368500002</X><Y>5441901.6191000007</Y></Point><Point xsi:type='typens:PointN'><X>32795021.5407</X><Y>5441953.4080999997</Y></Point><Point xsi:type='typens:PointN'><X>32795056.630599998</X><Y>5442041.6487000007</Y></Point><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point></PointArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;ETRS_1989_UTM_Zone_32N_8stellen&quot;,GEOGCS[&quot;GCS_ETRS_1989&quot;,DATUM[&quot;D_ETRS_1989&quot;,SPHEROID[&quot;GRS_1980&quot;,6378137.0,298.257222101]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Transverse_Mercator&quot;],PARAMETER[&quot;False_Easting&quot;,32500000.0],PARAMETER[&quot;False_Northing&quot;,0.0],PARAMETER[&quot;Central_Meridian&quot;,9.0],PARAMETER[&quot;Scale_Factor&quot;,0.9996],PARAMETER[&quot;Latitude_Of_Origin&quot;,0.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;ESRI&quot;,102329]]</WKT><XOrigin>26879100</XOrigin><YOrigin>-9998100</YOrigin><XYScale>450445547.3910538</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>102329</WKID></SpatialReference></PolygonN>");

			var clipEnvelope = (IEnvelope) FromXmlString(
				"<EnvelopeN xsi:type='typens:EnvelopeN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><XMin>32769125.6472</XMin><YMin>5433612.645299999</YMin><XMax>32794125.648199998</XMax><YMax>5458612.6462999992</YMax><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;ETRS_1989_UTM_Zone_32N&quot;,GEOGCS[&quot;GCS_ETRS_1989&quot;,DATUM[&quot;D_ETRS_1989&quot;,SPHEROID[&quot;GRS_1980&quot;,6378137.0,298.257222101]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Transverse_Mercator&quot;],PARAMETER[&quot;False_Easting&quot;,500000.0],PARAMETER[&quot;False_Northing&quot;,0.0],PARAMETER[&quot;Central_Meridian&quot;,9.0],PARAMETER[&quot;Scale_Factor&quot;,0.9996],PARAMETER[&quot;Latitude_Of_Origin&quot;,0.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,25832]]</WKT><XOrigin>26879100</XOrigin><YOrigin>-9998100</YOrigin><XYScale>10000</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>25832</WKID></SpatialReference></EnvelopeN>");

			var intersection =
				(IPolygon) ((ITopologicalOperator) polygon).Intersect(
					clipEnvelope, esriGeometryDimension.esriGeometry2Dimension);

			((ITopologicalOperator) polygon).Clip(clipEnvelope);

			Assert.IsTrue(GeometryUtils.AreEqualInXY(intersection, polygon));
		}

		[Test]
		public void ClipWorkaroundTest()
		{
			// from failed test:
			var polygon = (IPolygon) FromXmlString(
				"<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>32793643.734899998</XMin><YMin>5441054.0731000006</YMin><XMax>32795422.098299999</XMax><YMax>5442041.6487000007</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32795227.480599999</X><Y>5441899.5470000003</Y></Point><Point xsi:type='typens:PointN'><X>32795174.235300001</X><Y>5441850.8463000003</Y></Point><Point xsi:type='typens:PointN'><X>32795182.5264</X><Y>5441834.0930000003</Y></Point><Point xsi:type='typens:PointN'><X>32795199.3693</X><Y>5441841.5446000006</Y></Point><Point xsi:type='typens:PointN'><X>32795258.216899998</X><Y>5441892.9964000005</Y></Point><Point xsi:type='typens:PointN'><X>32795281.654100001</X><Y>5441899.9757000003</Y></Point><Point xsi:type='typens:PointN'><X>32795319.254799999</X><Y>5441830.0125999991</Y></Point><Point xsi:type='typens:PointN'><X>32795212.3915</X><Y>5441772.5091999993</Y></Point><Point xsi:type='typens:PointN'><X>32795219.925999999</X><Y>5441740.1579999998</Y></Point><Point xsi:type='typens:PointN'><X>32795251.5636</X><Y>5441741.8465</Y></Point><Point xsi:type='typens:PointN'><X>32795341.8717</X><Y>5441679.6561999992</Y></Point><Point xsi:type='typens:PointN'><X>32795363.9593</X><Y>5441707.9497999996</Y></Point><Point xsi:type='typens:PointN'><X>32795406.203200001</X><Y>5441702.9059999995</Y></Point><Point xsi:type='typens:PointN'><X>32795422.098299999</X><Y>5441688.1972000003</Y></Point><Point xsi:type='typens:PointN'><X>32795388.330499999</X><Y>5441632.1653000005</Y></Point><Point xsi:type='typens:PointN'><X>32795339.213300001</X><Y>5441652.4890000001</Y></Point><Point xsi:type='typens:PointN'><X>32795320.734700002</X><Y>5441625.0457000006</Y></Point><Point xsi:type='typens:PointN'><X>32795196.657400001</X><Y>5441615.4031000007</Y></Point><Point xsi:type='typens:PointN'><X>32795033.660700001</X><Y>5441666.2963999994</Y></Point><Point xsi:type='typens:PointN'><X>32795032.5825</X><Y>5441646.0008000005</Y></Point><Point xsi:type='typens:PointN'><X>32795097.631900001</X><Y>5441591.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32795212.388999999</X><Y>5441524.9702000003</Y></Point><Point xsi:type='typens:PointN'><X>32795138.566399999</X><Y>5441427.3739999998</Y></Point><Point xsi:type='typens:PointN'><X>32795065.968000002</X><Y>5441383.8164000008</Y></Point><Point xsi:type='typens:PointN'><X>32794873.847599998</X><Y>5441473.9607999995</Y></Point><Point xsi:type='typens:PointN'><X>32794841.837899998</X><Y>5441464.3869000003</Y></Point><Point xsi:type='typens:PointN'><X>32794838.430300001</X><Y>5441424.9773999993</Y></Point><Point xsi:type='typens:PointN'><X>32794830.0297</X><Y>5441373.5668000001</Y></Point><Point xsi:type='typens:PointN'><X>32794938.816300001</X><Y>5441309.3344000001</Y></Point><Point xsi:type='typens:PointN'><X>32794919.522799999</X><Y>5441141.7095999997</Y></Point><Point xsi:type='typens:PointN'><X>32794766.7423</X><Y>5441167.0229000002</Y></Point><Point xsi:type='typens:PointN'><X>32794733.4329</X><Y>5441054.0731000006</Y></Point><Point xsi:type='typens:PointN'><X>32794663.666000001</X><Y>5441068.1989999991</Y></Point><Point xsi:type='typens:PointN'><X>32794714.966600001</X><Y>5441221.2898999993</Y></Point><Point xsi:type='typens:PointN'><X>32794672.982099999</X><Y>5441222.3857000005</Y></Point><Point xsi:type='typens:PointN'><X>32794571.566100001</X><Y>5441206.0079999994</Y></Point><Point xsi:type='typens:PointN'><X>32794511.238899998</X><Y>5441201.2249999996</Y></Point><Point xsi:type='typens:PointN'><X>32794433.4505</X><Y>5441216.3864999991</Y></Point><Point xsi:type='typens:PointN'><X>32794274.653499998</X><Y>5441312.1027000006</Y></Point><Point xsi:type='typens:PointN'><X>32794235.395</X><Y>5441298.7367000002</Y></Point><Point xsi:type='typens:PointN'><X>32794212.237</X><Y>5441290.4397</Y></Point><Point xsi:type='typens:PointN'><X>32794230.379900001</X><Y>5441245.1228</Y></Point><Point xsi:type='typens:PointN'><X>32794201.688999999</X><Y>5441222.4119000006</Y></Point><Point xsi:type='typens:PointN'><X>32794122.3902</X><Y>5441306.5242999997</Y></Point><Point xsi:type='typens:PointN'><X>32794067.205400001</X><Y>5441232.5801999997</Y></Point><Point xsi:type='typens:PointN'><X>32794010.414700001</X><Y>5441287.9238000009</Y></Point><Point xsi:type='typens:PointN'><X>32794042.59</X><Y>5441343.3375000004</Y></Point><Point xsi:type='typens:PointN'><X>32794056.6899</X><Y>5441374.7445</Y></Point><Point xsi:type='typens:PointN'><X>32794008.651900001</X><Y>5441390.5569000002</Y></Point><Point xsi:type='typens:PointN'><X>32793903.277400002</X><Y>5441429.4452</Y></Point><Point xsi:type='typens:PointN'><X>32793827.061999999</X><Y>5441423.1009999998</Y></Point><Point xsi:type='typens:PointN'><X>32793765.724399999</X><Y>5441416.7119999994</Y></Point><Point xsi:type='typens:PointN'><X>32793741.764600001</X><Y>5441441.1714999992</Y></Point><Point xsi:type='typens:PointN'><X>32793726.771400001</X><Y>5441439.1162</Y></Point><Point xsi:type='typens:PointN'><X>32793727.339200001</X><Y>5441416.5571999997</Y></Point><Point xsi:type='typens:PointN'><X>32793743.148499999</X><Y>5441397.3149999995</Y></Point><Point xsi:type='typens:PointN'><X>32793694.3785</X><Y>5441388.8638000004</Y></Point><Point xsi:type='typens:PointN'><X>32793671.623999998</X><Y>5441410.7687999997</Y></Point><Point xsi:type='typens:PointN'><X>32793643.734899998</X><Y>5441411.2943999991</Y></Point><Point xsi:type='typens:PointN'><X>32793664.0374</X><Y>5441460.0019000005</Y></Point><Point xsi:type='typens:PointN'><X>32793712.9045</X><Y>5441448.2647999991</Y></Point><Point xsi:type='typens:PointN'><X>32793802.362599999</X><Y>5441474.0327000003</Y></Point><Point xsi:type='typens:PointN'><X>32793893.398699999</X><Y>5441460.8483000007</Y></Point><Point xsi:type='typens:PointN'><X>32793911.490899999</X><Y>5441491.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32793842.899099998</X><Y>5441545.7807999998</Y></Point><Point xsi:type='typens:PointN'><X>32793883.445</X><Y>5441570.8693000004</Y></Point><Point xsi:type='typens:PointN'><X>32793916.962899998</X><Y>5441539.2399000004</Y></Point><Point xsi:type='typens:PointN'><X>32793930.925999999</X><Y>5441522.909</Y></Point><Point xsi:type='typens:PointN'><X>32793942.5953</X><Y>5441540.1004000008</Y></Point><Point xsi:type='typens:PointN'><X>32793987.820299998</X><Y>5441513.3838</Y></Point><Point xsi:type='typens:PointN'><X>32794007.344499998</X><Y>5441534.5032000002</Y></Point><Point xsi:type='typens:PointN'><X>32794051.809100002</X><Y>5441503.2223000005</Y></Point><Point xsi:type='typens:PointN'><X>32794030.462499999</X><Y>5441478.4956</Y></Point><Point xsi:type='typens:PointN'><X>32794081.765299998</X><Y>5441460.6392999999</Y></Point><Point xsi:type='typens:PointN'><X>32794125.6472</X><Y>5441430.8323999997</Y></Point><Point xsi:type='typens:PointN'><X>32794165.025399998</X><Y>5441409.8354000002</Y></Point><Point xsi:type='typens:PointN'><X>32794181.6032</X><Y>5441428.2355000004</Y></Point><Point xsi:type='typens:PointN'><X>32794206.094900001</X><Y>5441458.5461999997</Y></Point><Point xsi:type='typens:PointN'><X>32794185.984000001</X><Y>5441479.7317999993</Y></Point><Point xsi:type='typens:PointN'><X>32794182.932700001</X><Y>5441503.6409000009</Y></Point><Point xsi:type='typens:PointN'><X>32794193.656500001</X><Y>5441524.8278999999</Y></Point><Point xsi:type='typens:PointN'><X>32794231.297899999</X><Y>5441508.2523999996</Y></Point><Point xsi:type='typens:PointN'><X>32794239.458999999</X><Y>5441525.7037000004</Y></Point><Point xsi:type='typens:PointN'><X>32794244.9421</X><Y>5441554.6692999993</Y></Point><Point xsi:type='typens:PointN'><X>32794226.599600002</X><Y>5441562.5756999999</Y></Point><Point xsi:type='typens:PointN'><X>32794119.367600001</X><Y>5441604.8714000005</Y></Point><Point xsi:type='typens:PointN'><X>32794171.8814</X><Y>5441691.6239999998</Y></Point><Point xsi:type='typens:PointN'><X>32794248.446600001</X><Y>5441667.2337999996</Y></Point><Point xsi:type='typens:PointN'><X>32794427.070299998</X><Y>5441685.9543999992</Y></Point><Point xsi:type='typens:PointN'><X>32794548.1261</X><Y>5441655.0434000008</Y></Point><Point xsi:type='typens:PointN'><X>32794568.2544</X><Y>5441638.6411000006</Y></Point><Point xsi:type='typens:PointN'><X>32794550.533</X><Y>5441602.1360999998</Y></Point><Point xsi:type='typens:PointN'><X>32794611.829800002</X><Y>5441595.7828000002</Y></Point><Point xsi:type='typens:PointN'><X>32794624.5854</X><Y>5441611.5179999992</Y></Point><Point xsi:type='typens:PointN'><X>32794648.952799998</X><Y>5441607.8008999992</Y></Point><Point xsi:type='typens:PointN'><X>32794676.268300001</X><Y>5441633.8512999993</Y></Point><Point xsi:type='typens:PointN'><X>32794700.6818</X><Y>5441629.1338999998</Y></Point><Point xsi:type='typens:PointN'><X>32794720.659400001</X><Y>5441632.2041999996</Y></Point><Point xsi:type='typens:PointN'><X>32794694.076400001</X><Y>5441688.3705000002</Y></Point><Point xsi:type='typens:PointN'><X>32794681.592700001</X><Y>5441713.4598999992</Y></Point><Point xsi:type='typens:PointN'><X>32794642.5559</X><Y>5441718.4124999996</Y></Point><Point xsi:type='typens:PointN'><X>32794638.275699999</X><Y>5441741.0192000009</Y></Point><Point xsi:type='typens:PointN'><X>32794582.310199998</X><Y>5441762.3999000005</Y></Point><Point xsi:type='typens:PointN'><X>32794559.363600001</X><Y>5441772.0436000004</Y></Point><Point xsi:type='typens:PointN'><X>32794580.201299999</X><Y>5441811.4894999992</Y></Point><Point xsi:type='typens:PointN'><X>32794581.477400001</X><Y>5441844.1878999993</Y></Point><Point xsi:type='typens:PointN'><X>32794547.189100001</X><Y>5441863.4134999998</Y></Point><Point xsi:type='typens:PointN'><X>32794564.789499998</X><Y>5441883.4630999994</Y></Point><Point xsi:type='typens:PointN'><X>32794625.989</X><Y>5441874.2074999996</Y></Point><Point xsi:type='typens:PointN'><X>32794644.362099998</X><Y>5441885.3830999993</Y></Point><Point xsi:type='typens:PointN'><X>32794649.1558</X><Y>5441896.9945</Y></Point><Point xsi:type='typens:PointN'><X>32794666.908300001</X><Y>5441902.4982999992</Y></Point><Point xsi:type='typens:PointN'><X>32794667.924599998</X><Y>5441884.9332999997</Y></Point><Point xsi:type='typens:PointN'><X>32794678.877900001</X><Y>5441868.7668999992</Y></Point><Point xsi:type='typens:PointN'><X>32794666.000500001</X><Y>5441826.6018000003</Y></Point><Point xsi:type='typens:PointN'><X>32794684.857999999</X><Y>5441784.4222999997</Y></Point><Point xsi:type='typens:PointN'><X>32794687.1884</X><Y>5441756.7638000008</Y></Point><Point xsi:type='typens:PointN'><X>32794723.055100001</X><Y>5441776.1153999995</Y></Point><Point xsi:type='typens:PointN'><X>32794738.9606</X><Y>5441754.5741000008</Y></Point><Point xsi:type='typens:PointN'><X>32794737.793700002</X><Y>5441716.9556000009</Y></Point><Point xsi:type='typens:PointN'><X>32794701.966800001</X><Y>5441701.7962999996</Y></Point><Point xsi:type='typens:PointN'><X>32794721.878699999</X><Y>5441658.2577</Y></Point><Point xsi:type='typens:PointN'><X>32794777.269000001</X><Y>5441704.6049000006</Y></Point><Point xsi:type='typens:PointN'><X>32794802.555199999</X><Y>5441757.3052999992</Y></Point><Point xsi:type='typens:PointN'><X>32794788.6241</X><Y>5441842.9425000008</Y></Point><Point xsi:type='typens:PointN'><X>32794790.782099999</X><Y>5441875.4204999991</Y></Point><Point xsi:type='typens:PointN'><X>32794803.455800001</X><Y>5441892.6676000003</Y></Point><Point xsi:type='typens:PointN'><X>32794775.923999999</X><Y>5441902.7346000001</Y></Point><Point xsi:type='typens:PointN'><X>32794799.538699999</X><Y>5441957.5080999993</Y></Point><Point xsi:type='typens:PointN'><X>32794833.919</X><Y>5442010.7605000008</Y></Point><Point xsi:type='typens:PointN'><X>32794895.6996</X><Y>5442020.1275999993</Y></Point><Point xsi:type='typens:PointN'><X>32794941.989399999</X><Y>5441986.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32794922.974799998</X><Y>5441927.5124999993</Y></Point><Point xsi:type='typens:PointN'><X>32794960.9023</X><Y>5441916.5629999992</Y></Point><Point xsi:type='typens:PointN'><X>32794971.965</X><Y>5441871.1712999996</Y></Point><Point xsi:type='typens:PointN'><X>32794915.513700001</X><Y>5441851.7902000006</Y></Point><Point xsi:type='typens:PointN'><X>32794908.172399998</X><Y>5441804.7263999991</Y></Point><Point xsi:type='typens:PointN'><X>32794954.2808</X><Y>5441817.8276000004</Y></Point><Point xsi:type='typens:PointN'><X>32795005.642999999</X><Y>5441840.2140999995</Y></Point><Point xsi:type='typens:PointN'><X>32795013.368500002</X><Y>5441901.6191000007</Y></Point><Point xsi:type='typens:PointN'><X>32795021.5407</X><Y>5441953.4080999997</Y></Point><Point xsi:type='typens:PointN'><X>32795056.630599998</X><Y>5442041.6487000007</Y></Point><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point></PointArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;ETRS_1989_UTM_Zone_32N&quot;,GEOGCS[&quot;GCS_ETRS_1989&quot;,DATUM[&quot;D_ETRS_1989&quot;,SPHEROID[&quot;GRS_1980&quot;,6378137.0,298.257222101]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Transverse_Mercator&quot;],PARAMETER[&quot;False_Easting&quot;,500000.0],PARAMETER[&quot;False_Northing&quot;,0.0],PARAMETER[&quot;Central_Meridian&quot;,9.0],PARAMETER[&quot;Scale_Factor&quot;,0.9996],PARAMETER[&quot;Latitude_Of_Origin&quot;,0.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,25832]]</WKT><XOrigin>26879100</XOrigin><YOrigin>-9998100</YOrigin><XYScale>450445547.3910538</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>25832</WKID></SpatialReference></PolygonN>");

			var clipEnvelope = (IEnvelope) FromXmlString(
				"<EnvelopeN xsi:type='typens:EnvelopeN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><XMin>32769125.6472</XMin><YMin>5433612.645299999</YMin><XMax>32794125.648199998</XMax><YMax>5458612.6462999992</YMax><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;ETRS_1989_UTM_Zone_32N&quot;,GEOGCS[&quot;GCS_ETRS_1989&quot;,DATUM[&quot;D_ETRS_1989&quot;,SPHEROID[&quot;GRS_1980&quot;,6378137.0,298.257222101]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Transverse_Mercator&quot;],PARAMETER[&quot;False_Easting&quot;,500000.0],PARAMETER[&quot;False_Northing&quot;,0.0],PARAMETER[&quot;Central_Meridian&quot;,9.0],PARAMETER[&quot;Scale_Factor&quot;,0.9996],PARAMETER[&quot;Latitude_Of_Origin&quot;,0.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,25832]]</WKT><XOrigin>26879100</XOrigin><YOrigin>-9998100</YOrigin><XYScale>10000</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>25832</WKID></SpatialReference></EnvelopeN>");

			IPolygon result = GeometryUtils.GetClippedPolygon(polygon, clipEnvelope);

			Console.WriteLine(GeometryUtils.ToString(result));
		}

		/// <summary>
		/// Repro case for the fact that IRelationalOperator.Equals treats bezier segments as straight lines in ArcGIS 10.1.
		/// </summary>
		[Test]
		public void ReproTestRelopEqualsIgnoringBeziers()
		{
			// Succeeds in 10.2.2

			const string linearXml =
				@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/9.3'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>100</XMin><YMin>100</YMin><XMax>130</XMax><YMax>130</YMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>100</X><Y>100</Y></Point><Point xsi:type='typens:PointN'><X>130</X><Y>130</Y></Point></PointArray></Path></PathArray></PolylineN>";
			const string bezierXml =
				@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/9.3'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>100</XMin><YMin>100</YMin><XMax>130</XMax><YMax>130</YMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><SegmentArray xsi:type='typens:ArrayOfSegment'><Segment xsi:type='typens:BezierCurve'><FromPoint xsi:type='typens:PointN'><X>100</X><Y>100</Y></FromPoint><ToPoint xsi:type='typens:PointN'><X>130</X><Y>130</Y></ToPoint><Degree>3</Degree><ControlPointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>130</X><Y>100</Y></Point><Point xsi:type='typens:PointN'><X>100</X><Y>130</Y></Point></ControlPointArray></Segment></SegmentArray></Path></PathArray></PolylineN>";

			var linear = (IPolyline) FromXmlString(linearXml);
			var bezier = (IPolyline) FromXmlString(bezierXml);

			Assert.AreEqual(42.426406871192853, linear.Length,
			                "Unexpected line length");
			Assert.AreEqual(50.296278822608542, bezier.Length,
			                "Unexpected bezier length");

			Assert.IsFalse(((IRelationalOperator) linear).Equals(bezier),
			               "Equals() does not detect difference line/bezier");
		}

		[Test]
		public void ReproTestDifferenceException()
		{
			// from test failing under 10.1: GeometryUtilsTest.CanAssignZToOneEndUncontainedPolylineDrape()
			// Succeeds in 10.2.2

			const string xml1 =
				@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.1'><HasID>false</HasID><HasZ>true</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>2000000.0000019073</XMin><YMin>1000005.0000019073</YMin><XMax>2000011.0000019073</XMax><YMax>1000005.0000019073</YMax><ZMin>NaN</ZMin><ZMax>NaN</ZMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>2000000.0000019073</X><Y>1000005.0000019073</Y><Z>NaN</Z></Point><Point xsi:type='typens:PointN'><X>2000011.0000019073</X><Y>1000005.0000019073</Y><Z>NaN</Z></Point></PointArray></Path></PathArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]],VERTCS[&quot;LHN95&quot;,VDATUM[&quot;Landeshohennetz_1995&quot;],PARAMETER[&quot;Vertical_Shift&quot;,0.0],PARAMETER[&quot;Direction&quot;,1.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,5729]]</WKT><XOrigin>-1998759890</XOrigin><YOrigin>-1998759890</YOrigin><XYScale>2251507.1852963744</XYScale><ZOrigin>-100</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>8.8829385624933387e-007</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID><LatestWKID>2056</LatestWKID><VCSWKID>5729</VCSWKID><LatestVCSWKID>5729</LatestVCSWKID></SpatialReference></PolylineN>";
			const string xml2 =
				@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.1'><HasID>false</HasID><HasZ>true</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>2000000.1111106873</XMin><YMin>1000005.0000021458</YMin><XMax>2000009.8888907433</XMax><YMax>1000005.0000021458</YMax><ZMin>NaN</ZMin><ZMax>NaN</ZMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>2000000.1111106873</X><Y>1000005.0000021458</Y><Z>NaN</Z></Point><Point xsi:type='typens:PointN'><X>2000009.8888907433</X><Y>1000005.0000021458</Y><Z>NaN</Z></Point></PointArray></Path></PathArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]],VERTCS[&quot;LHN95&quot;,VDATUM[&quot;Landeshohennetz_1995&quot;],PARAMETER[&quot;Vertical_Shift&quot;,0.0],PARAMETER[&quot;Direction&quot;,1.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,5729]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>10000</XYScale><ZOrigin>-100</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID><LatestWKID>2056</LatestWKID><VCSWKID>5729</VCSWKID><LatestVCSWKID>5729</LatestVCSWKID></SpatialReference></PolylineN>";

			// both polylines have NaN z values

			CalculateDifference(xml1, xml2);
		}

		[Test]
		public void ReproTestDifferenceException2()
		{
			// from test failing under 10.1: GeometryUtilsTest.CanAssignZToUncontainedPolylineDrape()
			// Succeeds in 10.2.2

			const string xml1 =
				@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.1'><HasID>false</HasID><HasZ>true</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>1999999.0000019073</XMin><YMin>1000005.0000019073</YMin><XMax>2000011.0000019073</XMax><YMax>1000005.0000019073</YMax><ZMin>10.000001907348633</ZMin><ZMax>10.000001907348633</ZMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>1999999.0000019073</X><Y>1000005.0000019073</Y><Z>10.000001907348633</Z></Point><Point xsi:type='typens:PointN'><X>2000011.0000019073</X><Y>1000005.0000019073</Y><Z>10.000001907348633</Z></Point></PointArray></Path></PathArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]],VERTCS[&quot;LHN95&quot;,VDATUM[&quot;Landeshohennetz_1995&quot;],PARAMETER[&quot;Vertical_Shift&quot;,0.0],PARAMETER[&quot;Direction&quot;,1.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,5729]]</WKT><XOrigin>-1998759890</XOrigin><YOrigin>-1998759890</YOrigin><XYScale>2251507.1852963744</XYScale><ZOrigin>-100</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>8.8829385624933387e-007</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID><LatestWKID>2056</LatestWKID><VCSWKID>5729</VCSWKID><LatestVCSWKID>5729</LatestVCSWKID></SpatialReference></PolylineN>";
			const string xml2 =
				@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.1'><HasID>false</HasID><HasZ>true</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>2000000.1111106873</XMin><YMin>1000005.0000021458</YMin><XMax>2000009.8888907433</XMax><YMax>1000005.0000021458</YMax><ZMin>10</ZMin><ZMax>10</ZMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>2000000.1111106873</X><Y>1000005.0000021458</Y><Z>10</Z></Point><Point xsi:type='typens:PointN'><X>2000009.8888907433</X><Y>1000005.0000021458</Y><Z>10</Z></Point></PointArray></Path></PathArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]],VERTCS[&quot;LHN95&quot;,VDATUM[&quot;Landeshohennetz_1995&quot;],PARAMETER[&quot;Vertical_Shift&quot;,0.0],PARAMETER[&quot;Direction&quot;,1.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,5729]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>10000</XYScale><ZOrigin>-100</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID><LatestWKID>2056</LatestWKID><VCSWKID>5729</VCSWKID><LatestVCSWKID>5729</LatestVCSWKID></SpatialReference></PolylineN>";

			CalculateDifference(xml1, xml2);
		}

		[Test]
		public void ReproTestDifferenceReturnsUnnecessaryPart()
		{
			// TODO: Report to esri inc. 
			IGeometry reference =
				FromXmlString(
					@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.4'><HasID>false</HasID><HasZ>true</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>729284.67500000075</XMin><YMin>229911.11250000075</YMin><XMax>729749.1799999997</XMax><YMax>230124.94000000134</YMax><ZMin>1443.1812499999796</ZMin><ZMax>1497.4862499999872</ZMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>729284.67500000075</X><Y>229911.11250000075</Y><Z>1497.2787499999831</Z></Point><Point xsi:type='typens:PointN'><X>729294.04125000164</X><Y>229912.3825000003</Y><Z>1497.4862499999872</Z></Point><Point xsi:type='typens:PointN'><X>729303.08999999985</X><Y>229917.93874999881</Y><Z>1496.7937499999825</Z></Point><Point xsi:type='typens:PointN'><X>729320.39375000075</X><Y>229919.3674999997</Y><Z>1494.1849999999831</Z></Point><Point xsi:type='typens:PointN'><X>729328.80750000104</X><Y>229921.74875000119</Y><Z>1492.758749999979</Z></Point><Point xsi:type='typens:PointN'><X>729332.77625000104</X><Y>229927.62249999866</Y><Z>1491.8862499999814</Z></Point><Point xsi:type='typens:PointN'><X>729347.85750000179</X><Y>229934.28999999911</Y><Z>1490.164999999979</Z></Point><Point xsi:type='typens:PointN'><X>729371.51125000045</X><Y>229932.38500000164</Y><Z>1486.8112499999843</Z></Point><Point xsi:type='typens:PointN'><X>729386.11625000089</X><Y>229936.98874999955</Y><Z>1486.7437499999796</Z></Point><Point xsi:type='typens:PointN'><X>729408.65875000134</X><Y>229938.73624999821</Y><Z>1486.5649999999878</Z></Point><Point xsi:type='typens:PointN'><X>729420.40625</X><Y>229937.30750000104</Y><Z>1484.8149999999878</Z></Point><Point xsi:type='typens:PointN'><X>729430.72500000149</X><Y>229937.46624999866</Y><Z>1483.0149999999849</Z></Point><Point xsi:type='typens:PointN'><X>729441.83749999851</X><Y>229938.73624999821</Y><Z>1480.7412499999919</Z></Point><Point xsi:type='typens:PointN'><X>729456.28375000134</X><Y>229943.81625000015</Y><Z>1480.9062499999854</Z></Point><Point xsi:type='typens:PointN'><X>729473.1099999994</X><Y>229943.49875000119</Y><Z>1477.3124999999854</Z></Point><Point xsi:type='typens:PointN'><X>729482.4537499994</X><Y>229945.84124999866</Y><Z>1476.6362499999814</Z></Point><Point xsi:type='typens:PointN'><X>729495.15374999866</X><Y>229947.84124999866</Y><Z>1477.5287499999831</Z></Point><Point xsi:type='typens:PointN'><X>729508.15374999866</X><Y>229954.34124999866</Y><Z>1476.1924999999901</Z></Point><Point xsi:type='typens:PointN'><X>729518.6724999994</X><Y>229961.12000000104</Y><Z>1477.6324999999924</Z></Point><Point xsi:type='typens:PointN'><X>729533.59499999881</X><Y>229960.4849999994</Y><Z>1476.946249999979</Z></Point><Point xsi:type='typens:PointN'><X>729549.31125000119</X><Y>229958.89750000089</Y><Z>1476.4074999999866</Z></Point><Point xsi:type='typens:PointN'><X>729570.90125000104</X><Y>229958.10375000164</Y><Z>1476.5287499999831</Z></Point><Point xsi:type='typens:PointN'><X>729578.6799999997</X><Y>229951.91250000149</Y><Z>1475.0324999999866</Z></Point><Point xsi:type='typens:PointN'><X>729585.66499999911</X><Y>229951.75375000015</Y><Z>1473.9274999999907</Z></Point><Point xsi:type='typens:PointN'><X>729593.44375000149</X><Y>229954.13500000164</Y><Z>1472.5812499999884</Z></Point><Point xsi:type='typens:PointN'><X>729597.41250000149</X><Y>229960.00874999911</Y><Z>1472.6937499999913</Z></Point><Point xsi:type='typens:PointN'><X>729607.4137500003</X><Y>229972.55000000075</Y><Z>1469.758749999979</Z></Point><Point xsi:type='typens:PointN'><X>729615.50874999911</X><Y>229987.15500000119</Y><Z>1467.1699999999837</Z></Point><Point xsi:type='typens:PointN'><X>729620.74749999866</X><Y>229992.55249999836</Y><Z>1465.8649999999907</Z></Point><Point xsi:type='typens:PointN'><X>729622.15249999985</X><Y>229997.33999999985</Y><Z>1463.7162499999831</Z></Point><Point xsi:type='typens:PointN'><X>729623.85249999911</X><Y>230000.23999999836</Y><Z>1461.8587499999849</Z></Point><Point xsi:type='typens:PointN'><X>729621.05249999836</X><Y>230018.53999999911</Y><Z>1457.352499999979</Z></Point><Point xsi:type='typens:PointN'><X>729620.85249999911</X><Y>230027.03999999911</Y><Z>1456.539999999979</Z></Point><Point xsi:type='typens:PointN'><X>729627.55249999836</X><Y>230040.23999999836</Y><Z>1453.5462499999849</Z></Point><Point xsi:type='typens:PointN'><X>729627.05249999836</X><Y>230053.53999999911</Y><Z>1455.5812499999884</Z></Point><Point xsi:type='typens:PointN'><X>729633.45125000179</X><Y>230063.94000000134</Y><Z>1454.6149999999907</Z></Point><Point xsi:type='typens:PointN'><X>729645.8512500003</X><Y>230070.53999999911</Y><Z>1452.3012499999895</Z></Point><Point xsi:type='typens:PointN'><X>729658.15125000104</X><Y>230081.44000000134</Y><Z>1450.7524999999878</Z></Point><Point xsi:type='typens:PointN'><X>729665.05000000075</X><Y>230091.65374999866</Y><Z>1449.8624999999884</Z></Point><Point xsi:type='typens:PointN'><X>729666.1487499997</X><Y>230100.02499999851</Y><Z>1452.1549999999843</Z></Point><Point xsi:type='typens:PointN'><X>729670.27625000104</X><Y>230104.78750000149</Y><Z>1451.3449999999866</Z></Point><Point xsi:type='typens:PointN'><X>729676.05124999955</X><Y>230107.94000000134</Y><Z>1448.446249999979</Z></Point><Point xsi:type='typens:PointN'><X>729682.3512500003</X><Y>230112.73999999836</Y><Z>1448.6987499999814</Z></Point><Point xsi:type='typens:PointN'><X>729710.85000000149</X><Y>230117.23999999836</Y><Z>1444.5574999999808</Z></Point><Point xsi:type='typens:PointN'><X>729732.94999999925</X><Y>230124.94000000134</Y><Z>1444.0362499999901</Z></Point><Point xsi:type='typens:PointN'><X>729749.1799999997</X><Y>230124.46499999985</Y><Z>1443.1812499999796</Z></Point></PointArray></Path></PathArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903_LV03&quot;,GEOGCS[&quot;GCS_CH1903&quot;,DATUM[&quot;D_CH1903&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,600000.0],PARAMETER[&quot;False_Northing&quot;,200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,21781]],VERTCS[&quot;LN_1902&quot;,VDATUM[&quot;Landesnivellement_1902&quot;],PARAMETER[&quot;Vertical_Shift&quot;,0.0],PARAMETER[&quot;Direction&quot;,1.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,5728]]</WKT><XOrigin>-29386400</XOrigin><YOrigin>-33067900</YOrigin><XYScale>800</XYScale><ZOrigin>-100000</ZOrigin><ZScale>800.00000000000011</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.012500000000000001</XYTolerance><ZTolerance>0.012500000000000001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>21781</WKID><LatestWKID>21781</LatestWKID><VCSWKID>5728</VCSWKID><LatestVCSWKID>5728</LatestVCSWKID></SpatialReference></PolylineN>");
			Assert.IsTrue(((ITopologicalOperator2) reference).IsSimple);

			IGeometry compare =
				FromXmlString(
					@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.4'><HasID>false</HasID><HasZ>true</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>729284.67500000075</XMin><YMin>229911.11250000075</YMin><XMax>729749.1799999997</XMax><YMax>230126.7262500003</YMax><ZMin>1443.1812499999796</ZMin><ZMax>1497.4862499999872</ZMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>729284.67500000075</X><Y>229911.11250000075</Y><Z>1497.2787499999831</Z></Point><Point xsi:type='typens:PointN'><X>729294.04125000164</X><Y>229912.3825000003</Y><Z>1497.4862499999872</Z></Point><Point xsi:type='typens:PointN'><X>729303.08999999985</X><Y>229917.93874999881</Y><Z>1496.7937499999825</Z></Point><Point xsi:type='typens:PointN'><X>729320.39375000075</X><Y>229919.3674999997</Y><Z>1494.1849999999831</Z></Point><Point xsi:type='typens:PointN'><X>729328.80750000104</X><Y>229921.74875000119</Y><Z>1492.758749999979</Z></Point><Point xsi:type='typens:PointN'><X>729332.77625000104</X><Y>229927.62249999866</Y><Z>1491.8862499999814</Z></Point><Point xsi:type='typens:PointN'><X>729347.85750000179</X><Y>229934.28999999911</Y><Z>1490.164999999979</Z></Point><Point xsi:type='typens:PointN'><X>729371.51125000045</X><Y>229932.38500000164</Y><Z>1486.8112499999843</Z></Point><Point xsi:type='typens:PointN'><X>729386.11625000089</X><Y>229936.98874999955</Y><Z>1486.7437499999796</Z></Point><Point xsi:type='typens:PointN'><X>729408.65875000134</X><Y>229938.73624999821</Y><Z>1486.5649999999878</Z></Point><Point xsi:type='typens:PointN'><X>729420.40625</X><Y>229937.30750000104</Y><Z>1484.8149999999878</Z></Point><Point xsi:type='typens:PointN'><X>729430.72500000149</X><Y>229937.46624999866</Y><Z>1483.0149999999849</Z></Point><Point xsi:type='typens:PointN'><X>729441.83749999851</X><Y>229938.73624999821</Y><Z>1480.7412499999919</Z></Point><Point xsi:type='typens:PointN'><X>729456.28375000134</X><Y>229943.81625000015</Y><Z>1480.9062499999854</Z></Point><Point xsi:type='typens:PointN'><X>729473.1099999994</X><Y>229943.49875000119</Y><Z>1477.3124999999854</Z></Point><Point xsi:type='typens:PointN'><X>729482.4537499994</X><Y>229945.84124999866</Y><Z>1476.6362499999814</Z></Point><Point xsi:type='typens:PointN'><X>729495.15374999866</X><Y>229947.84124999866</Y><Z>1477.5287499999831</Z></Point><Point xsi:type='typens:PointN'><X>729508.15374999866</X><Y>229954.34124999866</Y><Z>1476.1924999999901</Z></Point><Point xsi:type='typens:PointN'><X>729518.6724999994</X><Y>229961.12000000104</Y><Z>1477.6324999999924</Z></Point><Point xsi:type='typens:PointN'><X>729533.59499999881</X><Y>229960.4849999994</Y><Z>1476.946249999979</Z></Point><Point xsi:type='typens:PointN'><X>729549.31125000119</X><Y>229958.89750000089</Y><Z>1476.4074999999866</Z></Point><Point xsi:type='typens:PointN'><X>729570.90125000104</X><Y>229958.10375000164</Y><Z>1476.5287499999831</Z></Point><Point xsi:type='typens:PointN'><X>729578.6799999997</X><Y>229951.91250000149</Y><Z>1475.0324999999866</Z></Point><Point xsi:type='typens:PointN'><X>729585.66499999911</X><Y>229951.75375000015</Y><Z>1473.9274999999907</Z></Point><Point xsi:type='typens:PointN'><X>729593.44375000149</X><Y>229954.13500000164</Y><Z>1472.5812499999884</Z></Point><Point xsi:type='typens:PointN'><X>729597.41250000149</X><Y>229960.00874999911</Y><Z>1472.6937499999913</Z></Point><Point xsi:type='typens:PointN'><X>729607.4137500003</X><Y>229972.55000000075</Y><Z>1469.758749999979</Z></Point><Point xsi:type='typens:PointN'><X>729615.50874999911</X><Y>229987.15500000119</Y><Z>1467.1699999999837</Z></Point><Point xsi:type='typens:PointN'><X>729620.74749999866</X><Y>229992.55249999836</Y><Z>1465.8649999999907</Z></Point><Point xsi:type='typens:PointN'><X>729622.15249999985</X><Y>229997.33999999985</Y><Z>1463.7162499999831</Z></Point><Point xsi:type='typens:PointN'><X>729623.85249999911</X><Y>230000.23999999836</Y><Z>1461.8587499999849</Z></Point><Point xsi:type='typens:PointN'><X>729621.05249999836</X><Y>230018.53999999911</Y><Z>1457.352499999979</Z></Point><Point xsi:type='typens:PointN'><X>729620.85249999911</X><Y>230027.03999999911</Y><Z>1456.539999999979</Z></Point><Point xsi:type='typens:PointN'><X>729623.96499999985</X><Y>230033.22125000134</Y><Z>1457.6474999999919</Z></Point><Point xsi:type='typens:PointN'><X>729625.39624999836</X><Y>230037.98874999955</Y><Z>1457.3199999999924</Z></Point><Point xsi:type='typens:PointN'><X>729627.47500000149</X><Y>230049.17500000075</Y><Z>1456.8274999999849</Z></Point><Point xsi:type='typens:PointN'><X>729628.23750000075</X><Y>230054.40125000104</Y><Z>1456.828749999986</Z></Point><Point xsi:type='typens:PointN'><X>729632.3287499994</X><Y>230061.27749999985</Y><Z>1456.172499999986</Z></Point><Point xsi:type='typens:PointN'><X>729635.09375</X><Y>230065.31125000119</Y><Z>1455.8449999999866</Z></Point><Point xsi:type='typens:PointN'><X>729639.6400000006</X><Y>230068.15249999985</Y><Z>1455.6812499999796</Z></Point><Point xsi:type='typens:PointN'><X>729645.40625</X><Y>230072.18625000119</Y><Z>1455.0249999999796</Z></Point><Point xsi:type='typens:PointN'><X>729649.6174999997</X><Y>230075.9450000003</Y><Z>1454.5324999999866</Z></Point><Point xsi:type='typens:PointN'><X>729655.37750000134</X><Y>230081.81125000119</Y><Z>1453.8762499999866</Z></Point><Point xsi:type='typens:PointN'><X>729662.6875</X><Y>230089.87875000015</Y><Z>1452.5649999999878</Z></Point><Point xsi:type='typens:PointN'><X>729665.1174999997</X><Y>230094.5549999997</Y><Z>1452.3999999999796</Z></Point><Point xsi:type='typens:PointN'><X>729667.10624999925</X><Y>230098.68124999851</Y><Z>1451.0887499999808</Z></Point><Point xsi:type='typens:PointN'><X>729670.42500000075</X><Y>230103.44750000164</Y><Z>1450.9237499999872</Z></Point><Point xsi:type='typens:PointN'><X>729675.07124999911</X><Y>230107.40500000119</Y><Z>1448.9362499999843</Z></Point><Point xsi:type='typens:PointN'><X>729676.05124999955</X><Y>230107.94000000134</Y><Z>1448.446249999979</Z></Point><Point xsi:type='typens:PointN'><X>729682.3512500003</X><Y>230112.73999999836</Y><Z>1448.6987499999814</Z></Point><Point xsi:type='typens:PointN'><X>729685.3912499994</X><Y>230114.99875000119</Y><Z>1449.7762499999808</Z></Point><Point xsi:type='typens:PointN'><X>729689.60624999925</X><Y>230117.0150000006</Y><Z>1450.1037499999802</Z></Point><Point xsi:type='typens:PointN'><X>729697.59625000134</X><Y>230120.13125000149</Y><Z>1449.7762499999808</Z></Point><Point xsi:type='typens:PointN'><X>729700.81374999881</X><Y>230121.23000000045</Y><Z>1449.4487499999814</Z></Point><Point xsi:type='typens:PointN'><X>729707.70499999821</X><Y>230120.40374999866</Y><Z>1448.3012499999895</Z></Point><Point xsi:type='typens:PointN'><X>729715.1487499997</X><Y>230120.12750000134</Y><Z>1447.6449999999895</Z></Point><Point xsi:type='typens:PointN'><X>729722.7037499994</X><Y>230120.12624999881</Y><Z>1446.6612499999901</Z></Point><Point xsi:type='typens:PointN'><X>729726.25375000015</X><Y>230121.95874999836</Y><Z>1445.6774999999907</Z></Point><Point xsi:type='typens:PointN'><X>729730.35375000164</X><Y>230125.44249999896</Y><Z>1445.0212499999907</Z></Point><Point xsi:type='typens:PointN'><X>729734.68374999985</X><Y>230126.7262500003</Y><Z>1444.2012499999837</Z></Point><Point xsi:type='typens:PointN'><X>729742.90749999881</X><Y>230125.8987499997</Y><Z>1443.5449999999837</Z></Point><Point xsi:type='typens:PointN'><X>729749.1799999997</X><Y>230124.46499999985</Y><Z>1443.1812499999796</Z></Point></PointArray></Path></PathArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903_LV03&quot;,GEOGCS[&quot;GCS_CH1903&quot;,DATUM[&quot;D_CH1903&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,600000.0],PARAMETER[&quot;False_Northing&quot;,200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,21781]],VERTCS[&quot;LN_1902&quot;,VDATUM[&quot;Landesnivellement_1902&quot;],PARAMETER[&quot;Vertical_Shift&quot;,0.0],PARAMETER[&quot;Direction&quot;,1.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,5728]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>800</XYScale><ZOrigin>-100000</ZOrigin><ZScale>800.00000000000011</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.012500000000000001</XYTolerance><ZTolerance>0.012500000000000001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>21781</WKID><LatestWKID>21781</LatestWKID><VCSWKID>5728</VCSWKID><LatestVCSWKID>5728</LatestVCSWKID></SpatialReference></PolylineN>");
			Assert.IsTrue(((ITopologicalOperator2) compare).IsSimple);

			IGeometry difference =
				((ITopologicalOperator2) compare).Difference(reference);
			Assert.IsTrue(((ITopologicalOperator2) difference).IsSimple);

			((ITopologicalOperator2) difference).IsKnownSimple_2 = false;

			int geometryCountPriorSimplify =
				((IGeometryCollection) difference).GeometryCount;

			((ITopologicalOperator2) difference).Simplify();

			int geometryCountAfterSimplify =
				((IGeometryCollection) difference).GeometryCount;

			Assert.AreEqual(2, geometryCountAfterSimplify);
			Assert.AreEqual(4, geometryCountPriorSimplify);
			Assert.AreEqual(geometryCountAfterSimplify, geometryCountPriorSimplify);
		}

		/// <summary>
		/// Repro case for the Simplify error in ArcGIS 10 (Simplify fails with "Unknown Error in Geometry Library" for some geometries)
		/// </summary>
		[Test]
		public void ReproTestSimplifyUnknownErrorInGeometrySystem()
		{
			//Succeeds in 10.2
			IPolyline g1 = CreatePolylineZ(5, 5, 1000, 15, 5, 1000);
			IPolygon g2 = CreatePolygon(0, 0, 10, 10, 100);

			IGeometry g2Boundary = ((ITopologicalOperator) g2).Boundary;

			foreach (IGeometry intersection in GetAllIntersections(g1, g2))
			{
				Simplify(intersection);

				IGeometry difference =
					((ITopologicalOperator) intersection).Difference(g2Boundary);

				// Workaround: get the first point of the point collection; this causes the following Simplify() to succeed
				// -> uncomment to try
				// GetFirstPointWorkaround(difference);

				// this fails with "unknown error in geometry system" on second intersection (non-empty polyline)
				// - only in ArcGIS 10 (no problem in 9.3.1)
				Simplify(difference);

				Console.WriteLine(@"Simplify() succeeded");
			}
		}

		[Test]
		public void ReproTestWeedAndGeneralizeWithTolerance0()
		{
			// TODO: report to ESRI Inc.
			// Fails in 10.2
			string xmlFile = TestData.GetDensifiedWorkUnitPerimeterPath();

			var densifiedPolyZ = (IPolygon) GeometryUtils.FromXmlFile(xmlFile);

			//
			// Weed 0.1: example for small tolerance - works correctly in 10.0 SP5
			//
			var weededSmallerTolerancePoly =
				(IPolygon) ((IClone) densifiedPolyZ).Clone();

			weededSmallerTolerancePoly.Weed(0.1);

			Assert.IsTrue(
				((IRelationalOperator) weededSmallerTolerancePoly).Equals(
					densifiedPolyZ),
				"Weed with 0.1 changed the geometry significantly (more than the tolerance)");

			//
			// Weed 0.0: the lower left corner of the densified rectangle is weeded out with tolerance 0:
			//
			var weeded0Poly = (IPolygon) ((IClone) densifiedPolyZ).Clone();

			weeded0Poly.Weed(0.0);

			Assert.IsTrue(((IRelationalOperator) weeded0Poly).Equals(densifiedPolyZ),
			              "Weed with 0.0 changed the geometry significantly (more than the tolerance)");

			// NOTE: as expected the same behaviour can be reproduced with Generalize instead of weed
		}

		[Test]
		public void CanGetIntersectionPointsForSpecificGeometry()
		{
			// reproduces missing intersection points in ArcGIS 10.0 (and probably 9.3.1 also) and 10.2
			// NIM093558
			// succeeds with 10.4.1

			string linePath = TestData.GetSelfTouchingPolylinePath();
			var multipartPolyline = (IPolyline) GeometryUtils.FromXmlFile(linePath);

			string polyPath = TestData.GetPolygonIntersectingSelfTouchingPolylinePath();
			var poly = (IPolygon) GeometryUtils.FromXmlFile(polyPath);

			IGeometry result = ((ITopologicalOperator) poly).Intersect(
				multipartPolyline, esriGeometryDimension.esriGeometry0Dimension);

			Assert.Greater(((IPointCollection) result).PointCount, 2);
		}

		[Test]
		public void Generalize3DChangesLineEndPoint()
		{
			// Succeeds in 10.2.2
			const string xmlPolyline =
				@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>true</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>2712534.4849999994</XMin><YMin>1264059.7950000018</YMin><XMax>2712536.9649999999</XMax><YMax>1264094.3225000016</YMax><ZMin>448.21624999999767</ZMin><ZMax>448.74874999999884</ZMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>2712534.4849999994</X><Y>1264094.3225000016</Y><Z>448.21624999999767</Z></Point><Point xsi:type='typens:PointN'><X>2712535.848749999</X><Y>1264075.2300000004</Y><Z>448.53500000000349</Z></Point><Point xsi:type='typens:PointN'><X>2712536.9074999988</X><Y>1264060.6774999984</Y><Z>448.73625000000175</Z></Point><Point xsi:type='typens:PointN'><X>2712536.9649999999</X><Y>1264059.7950000018</Y><Z>448.74874999999884</Z></Point></PointArray></Path></PathArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>140996569.55187955</XYScale><ZOrigin>-100000</ZOrigin><ZScale>800</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.012500000000000001</XYTolerance><ZTolerance>0.012500000000000001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID></SpatialReference></PolylineN>";

			var polyline = (IPolyline) FromXmlString(xmlPolyline);

			var clone = (IPolyline) ((IClone) polyline).Clone();

			((IPolycurve3D) polyline).Generalize3D(0.05);

			IPoint originalToPoint = clone.ToPoint;
			IPoint newToPoint = polyline.ToPoint;

			// the change in Z is just below the tolerance
			Assert.IsTrue(originalToPoint.Z == newToPoint.Z);
		}

		[Test]
		public void BufferTestRoundCap()
		{
			// This works fine with 10.0 and starts to fail with 10.1, 10.2.
			// It is fixed with 10.2.2
			// NIM097069

			// The symptoms are that even though esriBufferRound is specified, one end of the buffer is flat
			// with only 1 geometry in the bag, this happens with approx. 20-40% of all geometries. With 2 and more we have not yet seen this happening
			// see https://issuetracker02.eggits.net/browse/TOP-4459

			const string xmlPolyline =
				@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>true</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>2693598.5449569803</XMin><YMin>1298493.1542750292</YMin><XMax>2693661.5271756114</XMax><YMax>1298661.3944480843</YMax><ZMin>0</ZMin><ZMax>0</ZMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>2693643.4090031283</X><Y>1298661.3944480843</Y><Z>0</Z></Point><Point xsi:type='typens:PointN'><X>2693598.5449569803</X><Y>1298576.8429764977</Y><Z>0</Z></Point><Point xsi:type='typens:PointN'><X>2693661.5271756114</X><Y>1298493.1542750292</Y><Z>0</Z></Point></PointArray></Path></PathArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>140996569.55187955</XYScale><ZOrigin>-100000</ZOrigin><ZScale>800</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.012500000000000001</XYTolerance><ZTolerance>0.012500000000000001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID></SpatialReference></PolylineN>";
			IGeometry polyline = FromXmlString(xmlPolyline);

			IGeometryBag bag = CreateGeometryBag(polyline);

			// *** ROUND Buffer End! ***
			const esriBufferConstructionEndEnum bufferEnd =
				esriBufferConstructionEndEnum.esriBufferRound;

			const esriBufferConstructionSideEnum esriBufferSide =
				esriBufferConstructionSideEnum.esriBufferFull;

			const bool explodeBuffers = false;

			IBufferConstruction bufferConstruction = new BufferConstructionClass();
			var bufferProperties =
				(IBufferConstructionProperties) bufferConstruction;

			bufferProperties.ExplodeBuffers = explodeBuffers;

			bufferProperties.GenerateCurves = false;
			bufferProperties.DensifyDeviation = -1;

			bufferProperties.UnionOverlappingBuffers = true;
			bufferProperties.SideOption = esriBufferSide;
			bufferProperties.EndOption = bufferEnd;

			IDoubleArray distances = new DoubleArrayClass();
			distances.Add(11.75);

			IGeometryCollection outputCollection = new GeometryBagClass();

			bufferConstruction.ConstructBuffersByDistances2((IEnumGeometry) bag,
			                                                distances,
			                                                outputCollection);

			IGeometry firstPoly = outputCollection.get_Geometry(0);

			// when the bug occurs, the point count is ca. 32
			Assert.AreEqual(50, ((IPointCollection) firstPoly).PointCount,
			                "Unexpected buffer point count");
		}

		[Test]
		public void
			Repro_IRelationalOperator_Disjoint_ReturnsIncorrectResult_Multipoints()
		{
			// This started failing with 10.0
			// Succeeds in 10.2.2
			// multipoint: x / point: o
			//
			//   x     x
			//         
			//      o
			//
			//   x     x
			// 

			IList<IPoint> points = new List<IPoint>
			                       {
				                       GeometryFactory.CreatePoint(0, 0),
				                       GeometryFactory.CreatePoint(10, 0),
				                       GeometryFactory.CreatePoint(10, 10),
				                       GeometryFactory.CreatePoint(10, 0)
			                       };

			IMultipoint multipoint = GeometryFactory.CreateMultipoint(points);

			IPoint point = GeometryFactory.CreatePoint(5, 5);

			// contains work-around:
			Assert.IsTrue(GeometryUtils.Disjoint(multipoint, point));

			// fails:
			Assert.IsTrue(((IRelationalOperator) multipoint).Disjoint(point));
		}

		[Test]
		public void
			Repro_IHitTest_HitTest_DoesNotFindHitSegmentWithSearchPointCloseToVertex
			()
		{
			// TODO: report to Esri Inc.
			// Distance from point to line: 0.00093 (below resolution)
			// Distance from point to line endpoint 0.0128 (just above tolerance)

			// this is most likely due to some internal optimization in hit test that uses IRelationalOperator
			// (for which the tolerance is applied to x-difference and y-difference separately)
			const string xmlPolyline =
				@"<PolylineN xsi:type='typens:PolylineN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>true</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>2710684.3012499996</XMin><YMin>1262409.2987499982</YMin><XMax>2710701.2487500012</XMax><YMax>1262412.3150000013</YMax><ZMin>499.76374999999825</ZMin><ZMax>500.55250000000524</ZMax></Extent><PathArray xsi:type='typens:ArrayOfPath'><Path xsi:type='typens:Path'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>2710701.2487500012</X><Y>1262412.3150000013</Y><Z>499.76374999999825</Z></Point><Point xsi:type='typens:PointN'><X>2710688.3900000006</X><Y>1262409.2987499982</Y><Z>500.55250000000524</Z></Point><Point xsi:type='typens:PointN'><X>2710684.3012499996</X><Y>1262411.963750001</Y><Z>500.33125000000291</Z></Point></PointArray></Path></PathArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>140996569.55187955</XYScale><ZOrigin>-100000</ZOrigin><ZScale>800</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.012500000000000001</XYTolerance><ZTolerance>0.012500000000000001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID></SpatialReference></PolylineN>";

			IGeometry polyline = FromXmlString(xmlPolyline);
			IPoint point = new PointClass();

			point.SpatialReference = polyline.SpatialReference;

			// TODO: Point XY values?

			Essentials.Assertions.Assert.ArgumentNotNull(polyline, "geometry");
			Essentials.Assertions.Assert.ArgumentNotNull(point, "point");

			var hitTest = (IHitTest) polyline;

			IPoint hitPoint = new PointClass();
			double hitDistance = 0;
			int segmentIndex = -1;
			int partIdx = -1;
			var rightSide = false;

			bool found = hitTest.HitTest(
				point, 0.0125,
				esriGeometryHitPartType.esriGeometryPartBoundary, hitPoint,
				ref hitDistance, ref partIdx, ref segmentIndex, ref rightSide);

			Assert.IsTrue(found);
		}

		[Test]
		public void Repro_ITopologicalOperator3GetIsSimpleExDoesNotFind0LengthSegments()
		{
			// This test fails with 10.2.2
			// TODO: Report to esri inc. 
			const string xmlPoly =
				@"<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.1'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>2670706.1552000009</XMin><YMin>1260591.7961000018</YMin><XMax>2671267.0730000008</XMax><YMax>1261075.9846000017</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>2670864.9054999985</X><Y>1261075.9846000001</Y></Point><Point xsi:type='typens:PointN'><X>2671261.7813000008</X><Y>1261065.4012999982</Y></Point><Point xsi:type='typens:PointN'><X>2670825.2179000005</X><Y>1260591.7961000018</Y></Point><Point xsi:type='typens:PointN'><X>2670825.2179000005</X><Y>1260591.7961000018</Y></Point><Point xsi:type='typens:PointN'><X>2670706.1552000009</X><Y>1260853.7342000008</Y></Point><Point xsi:type='typens:PointN'><X>2670864.9054999985</X><Y>1261075.9846000001</Y></Point></PointArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>140996569.55187955</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID><LatestWKID>2056</LatestWKID></SpatialReference></PolygonN>";

			IGeometry poly = FromXmlString(xmlPoly);

			IPoint vertex2 = ((IPointCollection) poly).get_Point(2);
			IPoint vertex3 = ((IPointCollection) poly).get_Point(3);

			bool hasDuplicateVertex = ((IRelationalOperator) vertex2).Equals(vertex3);

			Assert.IsTrue(hasDuplicateVertex);

			var topologicalOperator3 = ((ITopologicalOperator3) poly);

			topologicalOperator3.IsKnownSimple_2 = false;

			// expected: short segment
			esriNonSimpleReasonEnum reason;
			Assert.False(topologicalOperator3.get_IsSimpleEx(out reason));
		}

		[Test]
		public void Repro_IRelationalOperatorDisjointIncorrectAfterOtherDisjoint()
		{
			// This test fails with 10.2.2
			// Bug BUG-000106314

			string problemPolylineFilePath =
				TestUtils.GetGeometryTestDataPath("incorrect_disjoint_problemLine.xml");
			string intersectingPointFilePath =
				TestUtils.GetGeometryTestDataPath("incorrect_disjoint_intersectingPoint.xml");
			string otherLineFilePath =
				TestUtils.GetGeometryTestDataPath("incorrect_disjoint_someOtherLine.xml");

			var problemPolyline =
				(IPolyline) GeometryUtils.FromXmlFile(problemPolylineFilePath);
			var intersectingPoint =
				(IPoint) GeometryUtils.FromXmlFile(intersectingPointFilePath);
			var otherPolyline = (IPolyline) GeometryUtils.FromXmlFile(otherLineFilePath);

			// This is correct (not disjoint):
			Assert.IsFalse(
				((IRelationalOperator) problemPolyline).Disjoint(intersectingPoint));

			// Some other disjoint operation...
			bool disjointFromOtherLine =
				((IRelationalOperator) problemPolyline).Disjoint(otherPolyline);

			// BUG: Same statement as above, now reporting true (disjoint).
			Assert.IsFalse(
				((IRelationalOperator) problemPolyline).Disjoint(intersectingPoint));

			// Interesting observation, may be useful for analysis (comment above assertion to execute this code):
			((ISpatialIndex) problemPolyline).AllowIndexing = false;

			// Now it's correct again...
			Assert.IsFalse(
				((IRelationalOperator) problemPolyline).Disjoint(intersectingPoint));
		}

		[Test]
		public void Repro_ITopologicalOperatorIntersectErrorXyClusterToleranceTooLarge()
		{
			// This test fails with 10.2.2
			// TODO: Report to esri inc. 

			string polyline1Path = TestUtils.GetGeometryTestDataPath("Intersect1.xml");
			string polyline2Path = TestUtils.GetGeometryTestDataPath("Intersect2.xml");

			var polyline1 = (IPolyline) GeometryUtils.FromXmlFile(polyline1Path);
			var polyline2 = (IPolyline) GeometryUtils.FromXmlFile(polyline2Path);

			((ITopologicalOperator) polyline1).Intersect(
				polyline2, esriGeometryDimension.esriGeometry1Dimension);

			// NOTE: Setting the minimum tolerance of the spatial reference works around this bug. 
			// However, the result is different from the (correct) result with the original tolerance!
		}

		[Test]
		public void
			Repro_BufferConstruction_UnionOverlappingBuffers_Crashes_With_EmptyGeometries()
		{
			// This test fails with 10.2.2, 10.3.1, 10.4.1
			IBufferConstruction bufferConstruction = new BufferConstructionClass();
			((IBufferConstructionProperties) bufferConstruction).UnionOverlappingBuffers =
				true;

			var bag = new GeometryBagClass();
			object missing = Type.Missing;
			bag.AddGeometry(new PolygonClass(), ref missing, ref missing);

			IGeometryCollection outputCollection = new GeometryBagClass();
			bufferConstruction.ConstructBuffers(bag, 1, outputCollection);
		}

		[Test]
		public void Repro_IRelationalOperatorDisjointReturnsWrongResult_Polylines()
		{
			// TODO: Report to Esri Inc.
			// Found on 10.4.1

			// Two polylines (and their envelopes) are disjoint when using absolute numbers
			// but are within distance < tolerance (TopologicalOperator intersection is not empty)

			string polyline1Path = TestUtils.GetGeometryTestDataPath("disjointissue_polyline1.xml");
			string polyline2Path = TestUtils.GetGeometryTestDataPath("disjointissue_polyline2.xml");

			var polyline1 = (IPolyline) GeometryUtils.FromXmlFile(polyline1Path);
			var polyline2 = (IPolyline) GeometryUtils.FromXmlFile(polyline2Path);

			bool disjointResult = ((IRelationalOperator) polyline1).Disjoint(polyline2);

			// Should be false, but is true:
			Assert.False(disjointResult);

			IGeometry intersection = ((ITopologicalOperator) polyline1).Intersect(
				polyline2, esriGeometryDimension.esriGeometry0Dimension);

			// This is correct: the intersection is not empty:
			Assert.False(intersection.IsEmpty);

			// Even their envelopes intersect:
			IEnvelope envelope1 = polyline1.Envelope;
			IEnvelope envelope2 = polyline2.Envelope;

			bool envelopeDisjoint = ((IRelationalOperator) envelope1).Disjoint(envelope2);

			Assert.False(envelopeDisjoint);
		}

		[Test]
		public void Repro_ITopologicalOperatorDifferenceResultLosesAwareness()
		{
			// TODO: Report to Esri Inc.
			// Found on 10.4.1

			// Difference of multipoint source loses
			// - Z-awareness
			// - M-awareness
			// - PointID-awareness
			// even though the source geometry was aware (observed at 10.4.1).

			// Z/M/ID Awareness of the other geometry should make no difference, ITopoOp.Difference takes awareness from the source
			IPolygon perimeter = CreatePolygon(2600000, 1200000, 2600100, 1200100, 0);

			// Z/M/ID aware multipoint (intersecting)
			IGeometry sourceMultipoint = new MultipointClass();
			var sourcePointCollection = (IPointCollection4) sourceMultipoint;

			((IZAware) sourceMultipoint).ZAware = true;
			((IMAware) sourceMultipoint).MAware = true;
			((IPointIDAware) sourceMultipoint).PointIDAware = true;

			var pointArray = new WKSPointZ[2];
			PutPoint(pointArray, 0, 2599999, 1299999, 600);
			PutPoint(pointArray, 1, 2600001, 1200001, 1400);

			GeometryUtils.SetWKSPointZs(sourcePointCollection, pointArray);

			((ITopologicalOperator) sourceMultipoint).Simplify();

			Assert.IsTrue(((IZAware) sourceMultipoint).ZAware);
			Assert.IsTrue(((IMAware) sourceMultipoint).MAware);
			Assert.IsTrue(((IPointIDAware) sourceMultipoint).PointIDAware);

			IGeometry difference =
				((ITopologicalOperator) sourceMultipoint).Difference(perimeter);

			// In this example the assigned Z value is still there. But can we trust it?
			// Is it ok to just re-set the awareness, or are there cases where the
			// actual coordinate values (Z, M, PointID) are lost?
			Assert.IsTrue(((IZAware) difference).ZAware);
			Assert.IsTrue(((IMAware) difference).MAware);
			Assert.IsTrue(((IPointIDAware) difference).PointIDAware);
		}

		[Test]
		public void
			Repro_ITopologicalOperator_Intersect_ReturnsEmptyResultForVerticalPolyline()
		{
			// Probably works as desgned? Input for Topological Operators must be simple.

			ISpatialReference spatialRef = null;

			var pointArray = new WKSPointZ[4];
			PutPoint(pointArray, 0, 2600000, 1200000, 500);
			PutPoint(pointArray, 1, 2600040, 1200040, 500);
			PutPoint(pointArray, 2, 2600020, 1200020, 600);
			PutPoint(pointArray, 3, 2600000, 1200000, 500);

			IPolyline polyline = new PolylineClass();
			polyline.SpatialReference = spatialRef;

			((IZAware) polyline).ZAware = true;

			GeometryUtils.SetWKSPointZs(polyline, pointArray);

			// Vertical polyline intersecting the above polyline in 2 points:
			pointArray = new WKSPointZ[2];
			PutPoint(pointArray, 0, 2600010, 1200010, 800);
			PutPoint(pointArray, 1, 2600010, 1200010, 400);

			IPolyline verticalPolyline = new PolylineClass();
			verticalPolyline.SpatialReference = spatialRef;

			((IZAware) verticalPolyline).ZAware = true;

			GeometryUtils.SetWKSPointZs(verticalPolyline, pointArray);

			// Correct: The polyline and the vertical polyline intersect
			Assert.IsFalse(((IRelationalOperator) polyline).Disjoint(verticalPolyline));

			IGeometry planarIntersection =
				((ITopologicalOperator) polyline).Intersect(
					verticalPolyline.FromPoint,
					esriGeometryDimension.esriGeometry0Dimension);

			// Correct: The polyline and the vertical polyline's from-point intersect
			Assert.IsFalse(planarIntersection.IsEmpty);

			planarIntersection =
				((ITopologicalOperator) polyline).Intersect(
					verticalPolyline, esriGeometryDimension.esriGeometry0Dimension);

			// Incorrect - should contain one point at least:
			Assert.IsFalse(planarIntersection.IsEmpty);
		}

		[Test]
		[Ignore("works as designed")]
		public void
			Repro_ITopologicalOperator6_IntersectEx_ReturnsWrongResultOrFailsForVerticalGeometry
			()
		{
			// Reported as Bug BUG-000106339, but rejected with the reason:
			// Topological operations are 2-D based. The two polylines involved are with Z values, and not simple in 2-D

			string closedPathPolylinePath =
				TestUtils.GetGeometryTestDataPath("verticalRingBoundarypolyline.xml");
			//locator.GetPath("verticalRingBoundarypolyline.xml");

			string verticalPolylinePath =
				TestUtils.GetGeometryTestDataPath("verticalPolyline.xml");
			//locator.GetPath("verticalPolyline.xml");

			var closedPathPolyline =
				(IPolyline) GeometryUtils.FromXmlFile(closedPathPolylinePath);
			var verticalPolyline =
				(IPolyline) GeometryUtils.FromXmlFile(verticalPolylinePath);

			// From-point of vertical polyline intersects in 3D,
			Assert.IsFalse(
				((IRelationalOperator3D) closedPathPolyline).Disjoint3D(
					verticalPolyline.FromPoint));

			// to-point of vertical polyline intersects in 3D,
			Assert.IsFalse(
				((IRelationalOperator3D) closedPathPolyline).Disjoint3D(
					verticalPolyline.ToPoint));

			// therefore there should be intersection points:
			var intersectionPointsNonPlanar = (IPointCollection)
				((ITopologicalOperator6) closedPathPolyline).IntersectEx(
					verticalPolyline, true, esriGeometryDimension.esriGeometry0Dimension);

			// This fails:
			// Expected: 2 intersection points. BUT: 0!
			Assert.AreEqual(2, intersectionPointsNonPlanar.PointCount);

			// Alternatively, a linear intersection equal to the vertical polyline could be returned.
			IGeometry linearNonPlanarIntersections =
				((ITopologicalOperator6) closedPathPolyline).IntersectEx(
					verticalPolyline, true, esriGeometryDimension.esriGeometry1Dimension);

			// However, this also fails (small snippets of the source-geometry are returned also)
			Assert.IsTrue(
				((IClone) linearNonPlanarIntersections).IsEqual(
					(IClone) verticalPolyline));

			// Let us convert the vertical polyline into a multipoint and try that:
			IPointCollection multipoint = new MultipointClass();
			multipoint.AddPointCollection((IPointCollection) verticalPolyline);

			// Throws OutOfMemoryException or AccessViolation:
			IGeometry intersectionPointsNonPlanarWithMultipoint =
				((ITopologicalOperator6) verticalPolyline).IntersectEx(
					(IGeometry) multipoint, true,
					esriGeometryDimension.esriGeometry0Dimension);
		}

		[Test]
		public void
			Repro_ITopologicalOperator_Boundary_IncorrectForSpecificMultipatch_Plus_IRelationalOperator_Wrong
			()
		{
			// Observed at 10.4.1
			// TODO: Report to Esri Inc. However, this is probably a feature (because the boundary might not be simple)

			string multipatchLocation =
				TestUtils.GetGeometryTestDataPath("MultipatchWithWrongBoundary.xml");
			var multipatch = (IMultiPatch) GeometryUtils.FromXmlFile(multipatchLocation);
			var geometryCollection = (IGeometryCollection) multipatch;

			Assert.AreEqual(1, geometryCollection.GeometryCount);

			var boundary = (IPolyline) ((ITopologicalOperator) multipatch).Boundary;

			// The boundary corresponds to the envelope rather than the actual boundary of the multipatch ring
			var onlyRing = (IRing) geometryCollection.Geometry[0];

			// The lenght of the ring should be equal the length of the boundary of the multipatch. 
			// But it is not:
			Assert.AreEqual(onlyRing.Length, boundary.Length);

			var correctFootprint =
				(IPolygon) GeometryUtils.GetHighLevelGeometry(onlyRing);
			GeometryUtils.Simplify(correctFootprint, true, true);

			Assert.AreEqual(correctFootprint.Length, boundary.Length);

			// Additinal note: IRelationalOperator.Touches also return the wrong result - most likely because the boundary is used internally
			IPolyline touchingLine = GeometryFactory.CreatePolyline(onlyRing);

			Assert.IsTrue(((IRelationalOperator) correctFootprint).Touches(touchingLine));

			Assert.IsTrue(((IRelationalOperator) multipatch).Touches(touchingLine));

			var buffered = (IPolygon) GeometryUtils.Buffer(correctFootprint, 0.1);

			Assert.IsTrue(((IRelationalOperator) multipatch).Disjoint(
				              GeometryFactory.CreatePolyline(buffered)));
		}

		[Test]
		//[HandleProcessCorruptedStateExceptions]
		public void Repro_IEnumCurve_Crash()
		{
			IPoint fromPoint = new PointClass();
			fromPoint.PutCoords(0, 0);
			IPoint toPoint = new PointClass();
			toPoint.PutCoords(0, 10);

			var curve = new PolylineClass();
			//curve.SpatialReference = CreateSpatialReference(0.001, 0.0001);
			object o = Type.Missing;
			curve.AddPoint(fromPoint, ref o, ref o);
			curve.AddPoint(toPoint, ref o, ref o);

			var segments = (ISegmentCollection) curve;

			IEnumCurve enumCurve = segments.EnumCurve;
			enumCurve.Reset();

			//working: -1, 0, 9, curve.Length - ((ISpatialReferenceTolerance) curve.SpatialReference).XYTolerance
			//not working: curve.Length, 11
			enumCurve.Next(11);

			// Crashes ArcObjects with:
			// System.AccessViolationException : Attempted to read or write protected memory.
			try
			{
				ISegment segment = enumCurve.Segment;
			}
			//uncomment [HandleProcessCorruptedStateExceptions] will throw AccessViolationException
			catch (AccessViolationException)
			{
				Console.WriteLine(@"AccessViolationException is thrown.");
			}

			Console.WriteLine(@"Repro_IEnumCurve_Crash did not crash.");
		}

		[Test]
		public void Repro_ReplaceSegmentCollectionInconsistentEnvelope()
		{
			// TODO: Report to ESRI Inc. (currently no issue)
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);

			IPolyline polyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200000, 400, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600010, 1200000, 405, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600020, 1200000, 410, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600030, 1200000, 420, double.NaN,
					                            lv95)));

			IPath replacement = GeometryFactory.CreatePath(
				GeometryFactory.CreatePoint(2600010, 1200000, 100, double.NaN, lv95),
				GeometryFactory.CreatePoint(2600015, 1200010, 500, double.NaN, lv95),
				GeometryFactory.CreatePoint(2600020, 1200000, 900, double.NaN, lv95));

			// NOTE: The Z value of the first point of the replacement is used, however, the 
			//       envelope of the resulting pathToReshape is not updated!

			var pathToReshape =
				(ISegmentCollection) ((IGeometryCollection) polyline).Geometry[0];

			pathToReshape.ReplaceSegmentCollection(
				1, 2, (ISegmentCollection) replacement);

			// This makes no difference:
			//pathToReshape.SegmentsChanged();

			Assert.True(
				polyline.Envelope.ZMin <= ((IPointCollection) polyline).Point[1].Z,
				"Point has smaller Z than the envelope's ZMin!");
		}

		[Test]
		public void Repro_ITopologicalOperator_WrongResultsWithSpatialIndex()
		{
			// Found in 10.4.1, reproduced in 10.6.1 (issue: TOP-5106)
			// Reported to ESRI Inc. 24.10.2018, logged as BUG-000117827

			// The following methods have been tested and all behave inconsistently with respect to the spatial index on the geometry:
			// - ITopologicalOperator.Intersect
			// - ITopologicalOperator.Difference

			string polygonPath = TestUtils.GetGeometryTestDataPath("spatialindex_issue_poly.xml");
			string multipointPath =
				TestUtils.GetGeometryTestDataPath("spatialindex_issue_multipoint.xml");

			IGeometry polygon = GeometryUtils.FromXmlFile(polygonPath);
			IGeometry multipoint = GeometryUtils.FromXmlFile(multipointPath);

			// Intersect:
			IGeometry intersection =
				((ITopologicalOperator) multipoint).Intersect(
					polygon, esriGeometryDimension.esriGeometry0Dimension);
			int intersectionPointCountWithoutIndexing =
				((IPointCollection) intersection).PointCount;

			// Difference:
			IGeometry difference =
				((ITopologicalOperator) multipoint).Difference(polygon);
			int diffPointCountWithOutIndexing =
				((IPointCollection) difference).PointCount;

			// Repeate the same operations with indexing:
			GeometryUtils.AllowIndexing(polygon);

			// Intersect:
			intersection =
				((ITopologicalOperator) multipoint).Intersect(
					polygon, esriGeometryDimension.esriGeometry0Dimension);
			int intersectionPointCountWithIndexing =
				((IPointCollection) intersection).PointCount;

			// Difference:
			difference = ((ITopologicalOperator) multipoint).Difference(polygon);
			int diffPointCountWithIndexing = ((IPointCollection) difference).PointCount;

			// Test the results:
			Assert.AreEqual(intersectionPointCountWithIndexing,
			                intersectionPointCountWithoutIndexing);

			Assert.AreEqual(diffPointCountWithIndexing, diffPointCountWithOutIndexing);
		}

		#region Geometry creation utils

		private static IGeometryBag CreateGeometryBag(IGeometry polyline)
		{
			IGeometryBag bag = new GeometryBagClass();
			bag.SpatialReference = polyline.SpatialReference;

			// get the (first) path of the polyline
			var path = (IPath) ((IGeometryCollection) polyline).get_Geometry(0);

			// get the high level geometry of the path
			IPolyline highLevelPath = new PolylineClass
			                          {
				                          SpatialReference = polyline.SpatialReference
			                          };

			((IZAware) highLevelPath).ZAware = true;

			((ISegmentCollection) highLevelPath).AddSegmentCollection(
				(ISegmentCollection) path);

			object missingRef = Type.Missing;
			((IGeometryCollection) bag).AddGeometry(highLevelPath, ref missingRef,
			                                        ref missingRef);
			return bag;
		}

		private static void AssertIsNotSimple(IPolyline path1Polyline,
		                                      IPolyline path2Polyline)
		{
			var path1 = (IPath) ((IGeometryCollection) path1Polyline).get_Geometry(0);
			var path2 = (IPath) ((IGeometryCollection) path2Polyline).get_Geometry(0);

			esriNonSimpleReasonEnum nonSimpleReason;
			bool isSimple = GetIsSimple(path1, path2, out nonSimpleReason);

			Console.WriteLine(nonSimpleReason);
			Assert.IsFalse(isSimple, "Polyline is expected to be non-simple");
		}

		private static void AssertIsNotSimple(IPolygon ring1Poly, IPolygon ring2Poly)
		{
			var ring1 = (IRing) ((IGeometryCollection) ring1Poly).get_Geometry(0);
			var ring2 = (IRing) ((IGeometryCollection) ring2Poly).get_Geometry(0);

			esriNonSimpleReasonEnum nonSimpleReason;
			bool isSimple = GetIsSimple(ring1, ring2, out nonSimpleReason);

			Console.WriteLine(nonSimpleReason);
			Assert.IsFalse(isSimple, "Polygon is expected to be non-simple");
		}

		private static bool GetIsSimple(IPath path1,
		                                IPath path2,
		                                out esriNonSimpleReasonEnum nonSimpleReason)
		{
			var path1Clone = (IPath) ((IClone) path1).Clone();
			var path2Clone = (IPath) ((IClone) path2).Clone();

			var polyline = new PolylineClass();
			var rings = (IGeometryCollection) polyline;

			object missing = Type.Missing;
			rings.AddGeometry(path1Clone, ref missing, ref missing);
			rings.AddGeometry(path2Clone, ref missing, ref missing);

			return ((ITopologicalOperator3) polyline).get_IsSimpleEx(out nonSimpleReason);
		}

		private static bool GetIsSimple(IRing ring1,
		                                IRing ring2,
		                                out esriNonSimpleReasonEnum nonSimpleReason)
		{
			IPolygon polygon = CreatePolygon(ring1, ring2);

			return ((ITopologicalOperator3) polygon).get_IsSimpleEx(out nonSimpleReason);
		}

		private static IPolygon CreatePolygon(IRing ring1, IRing ring2)
		{
			var ring1Clone = (IRing) ((IClone) ring1).Clone();
			var ring2Clone = (IRing) ((IClone) ring2).Clone();

			var polygon = new PolygonClass();
			var rings = (IGeometryCollection) polygon;

			object missing = Type.Missing;
			rings.AddGeometry(ring1Clone, ref missing, ref missing);
			rings.AddGeometry(ring2Clone, ref missing, ref missing);

			return polygon;
		}

		private static void ReproProjectIssues(IPolygon lv03Polygon,
		                                       IPolygon lv95FineltraPolygon)
		{
			Console.WriteLine(@"lv03 polygon width: {0}", lv03Polygon.Envelope.Width);
			Console.WriteLine(@"lv03 polygon height: {0}", lv03Polygon.Envelope.Height);

			Console.WriteLine(@"lv03 polygon:");
			Console.WriteLine(GeometryUtils.ToString(lv03Polygon));

			Console.WriteLine(
				@"lv95 polygon transformed using official swisstopo algorithm (FINELTRA):");
			Console.WriteLine(GeometryUtils.ToString(lv95FineltraPolygon));

			ISpatialReference lv95 = lv95FineltraPolygon.SpatialReference;

			var lv95ProjectPolygon = (IPolygon) ((IClone) lv03Polygon).Clone();

			// create NTv2 transformation lv03 -> lv95
			// same thing happens with default transformation
			IGeoTransformation geoTransformation =
				SpatialReferenceUtils.CreateGeoTransformation(15486);

			((IGeometry5) lv95ProjectPolygon).ProjectEx(
				lv95,
				esriTransformDirection.esriTransformForward,
				geoTransformation, false, 0, 0);

			Console.WriteLine(@"lv95 polygon transformed using ProjectEx() (NTv2)");
			Console.WriteLine(GeometryUtils.ToString(lv95ProjectPolygon));

			Console.WriteLine(@"lv95 (ProjectEx()) polygon width: {0}",
			                  lv95ProjectPolygon.Envelope.Width);
			Console.WriteLine(@"lv95 (ProjectEx()) polygon height: {0}",
			                  lv95ProjectPolygon.Envelope.Height);

			const double tolerance = 0.05;
			// large polygon may have larger difference due to NTv2
			Assert.AreEqual(lv03Polygon.Envelope.Width,
			                lv95ProjectPolygon.Envelope.Width, tolerance,
			                "Unexpected envelope width!");
			Assert.AreEqual(lv03Polygon.Envelope.Height,
			                lv95ProjectPolygon.Envelope.Height,
			                tolerance, "Unexpected envelope height!");
		}

		private static void CalculateDifference([NotNull] string xml1,
		                                        [NotNull] string xml2)
		{
			var polyline1 = (IPolyline) FromXmlString(xml1);
			var polyline2 = (IPolyline) FromXmlString(xml2);

			Console.WriteLine(@"polyline1");
			Console.WriteLine(GeometryUtils.ToString(polyline1));
			Console.WriteLine(@"polyline2");
			Console.WriteLine(GeometryUtils.ToString(polyline2));

			IGeometry result = ((ITopologicalOperator) polyline1).Difference(polyline2);

			Console.WriteLine(@"result");
			Console.WriteLine(GeometryUtils.ToString(result));
		}

		// ReSharper disable UnusedMember.Local
		private static void GetFirstPointWorkaround(IGeometry geometry)
			// ReSharper restore UnusedMember.Local
		{
			var points = geometry as IPointCollection;

			if (points != null && points.PointCount > 0)
			{
				points.get_Point(0);
			}
		}

		private static IEnumerable<IGeometry> GetAllIntersections(IGeometry g1,
		                                                          IGeometry g2)
		{
			IGeometry intersections =
				((ITopologicalOperator2) g1).IntersectMultidimension(g2);

			if (intersections.IsEmpty)
			{
				yield break;
			}

			var bag = intersections as IGeometryBag;
			if (bag != null)
			{
				var collection = (IGeometryCollection) bag;
				for (var i = 0; i < collection.GeometryCount; i++)
				{
					IGeometry geometry = collection.get_Geometry(i);

					yield return geometry;
				}
			}
			else if (intersections is IMultipoint)
			{
				yield return intersections;
			}
			else
			{
				throw new InvalidOperationException(
					"Multipoint or geometry bag expected");
			}
		}

		private static void Simplify(IGeometry geometry)
		{
			var topoOp2 = geometry as ITopologicalOperator2;
			if (topoOp2 != null)
			{
				topoOp2.IsKnownSimple_2 = false;
			}

			var topoOp = geometry as ITopologicalOperator;
			if (topoOp != null)
			{
				topoOp.Simplify();
			}
		}

		private static IGeometry FromXmlString(string xmlGeometryString)
		{
			IXMLSerializer xmlSerializer = new XMLSerializerClass();

			try
			{
				return
					(IGeometry)
					xmlSerializer.LoadFromString(xmlGeometryString, null, null);
			}
			finally
			{
				Marshal.ReleaseComObject(xmlSerializer);
			}
		}

		private static IEnvelope CreateEnvelope(
			double xmin, double ymin,
			double xmax, double ymax,
			ISpatialReference spatialReference)
		{
			// spatialReference may 
			var envelope = new EnvelopeClass
			               {
				               XMin = xmin,
				               YMin = ymin,
				               XMax = xmax,
				               YMax = ymax
			               };

			if (spatialReference != null)
			{
				envelope.SpatialReference = spatialReference;
			}

			return envelope;
		}

		private static IPolygon CreatePolygon(double xmin, double ymin,
		                                      double xmax, double ymax,
		                                      double constantZ)
		{
			IEnvelope envelope = CreateEnvelope(xmin, ymin, xmax, ymax, null);
			IPolygon polygon = CreatePolygon(envelope);

			((IZAware) polygon).ZAware = true;
			((IZ) polygon).SetConstantZ(constantZ);

			return polygon;
		}

		private static IPolygon CreatePolygon(IEnvelope envelope)
		{
			IPolygon polygon = new PolygonClass();

			SetRectangle(polygon, envelope);

			return polygon;
		}

		private static void SetRectangle(IPolygon polygon, IEnvelope envelope)
		{
			polygon.SpatialReference = envelope.SpatialReference;

			if (((IZAware) envelope).ZAware)
			{
				((IZAware) polygon).ZAware = true;
			}

			if (envelope.IsEmpty)
			{
				polygon.SetEmpty();
			}
			else
			{
				((ISegmentCollection) polygon).SetRectangle(envelope);
				((ITopologicalOperator) polygon).Simplify();
			}
		}

		/// <summary>
		/// Test helper method to put a point into a z-aware wks point array
		/// </summary>
		/// <param name="points"></param>
		/// <param name="index"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		private static void PutPoint(WKSPointZ[] points, int index,
		                             double x, double y, double z)
		{
			points[index].X = x;
			points[index].Y = y;
			points[index].Z = z;
		}

		/// <summary>
		/// Test helper method to put a point into a wks point array
		/// </summary>
		/// <param name="points"></param>
		/// <param name="index"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		private static void PutPoint(WKSPoint[] points, int index,
		                             double x, double y)
		{
			points[index].X = x;
			points[index].Y = y;
		}

		/// <summary>
		/// Test helper method to create z-aware polyline based on 2 vertices
		/// </summary>
		/// <param name="x1"></param>
		/// <param name="y1"></param>
		/// <param name="z1"></param>
		/// <param name="x2"></param>
		/// <param name="y2"></param>
		/// <param name="z2"></param>
		/// <returns></returns>
		private static IPolyline CreatePolylineZ(double x1, double y1, double z1,
		                                         double x2, double y2, double z2)
		{
			IPolyline result = new PolylineClass();
			((IZAware) result).ZAware = true;

			var pointCollection = (IPointCollection4) result;

			var pointArray = new WKSPointZ[2];
			PutPoint(pointArray, 0, x1, y1, z1);
			PutPoint(pointArray, 1, x2, y2, z2);

			GeometryUtils.SetWKSPointZs(pointCollection, pointArray);

			((ITopologicalOperator) result).Simplify();

			return result;
		}

		/// <summary>
		/// Test helper method to create z-aware polyline based on 3 vertices
		/// </summary>
		/// <param name="x1"></param>
		/// <param name="y1"></param>
		/// <param name="z1"></param>
		/// <param name="x2"></param>
		/// <param name="y2"></param>
		/// <param name="z2"></param>
		/// <param name="x3"></param>
		/// <param name="y3"></param>
		/// <param name="z3"></param>
		/// <returns></returns>
		private static IPolyline CreatePolylineZ(double x1, double y1, double z1,
		                                         double x2, double y2, double z2,
		                                         double x3, double y3, double z3)
		{
			IPolyline result = new PolylineClass();
			((IZAware) result).ZAware = true;

			var pointCollection = (IPointCollection4) result;

			var pointArray = new WKSPointZ[3];
			PutPoint(pointArray, 0, x1, y1, z1);
			PutPoint(pointArray, 1, x2, y2, z2);
			PutPoint(pointArray, 2, x3, y3, z3);

			GeometryUtils.SetWKSPointZs(pointCollection, pointArray);

			((ITopologicalOperator) result).Simplify();

			return result;
		}

		/// <summary>
		/// Test helper method to create polyline based on 2 vertices
		/// </summary>
		/// <param name="x1"></param>
		/// <param name="y1"></param>
		/// <param name="x2"></param>
		/// <param name="y2"></param>
		/// <returns></returns>
		private static IPolyline CreatePolyline(double x1, double y1,
		                                        double x2, double y2)
		{
			IPolyline result = new PolylineClass();

			var pointCollection = (IPointCollection4) result;

			var pointArray = new WKSPoint[2];
			PutPoint(pointArray, 0, x1, y1);
			PutPoint(pointArray, 1, x2, y2);

			GeometryUtils.SetWKSPoints(pointCollection, pointArray);

			((ITopologicalOperator) result).Simplify();

			return result;
		}

		/// <summary>
		/// Test helper method to create polyline based on 3 vertices
		/// </summary>
		/// <param name="x1"></param>
		/// <param name="y1"></param>
		/// <param name="x2"></param>
		/// <param name="y2"></param>
		/// <param name="x3"></param>
		/// <param name="y3"></param>
		/// <returns></returns>
		private static IPolyline CreatePolyline(double x1, double y1,
		                                        double x2, double y2,
		                                        double x3, double y3)
		{
			IPolyline result = new PolylineClass();

			var pointCollection = (IPointCollection4) result;

			var pointArray = new WKSPoint[3];
			PutPoint(pointArray, 0, x1, y1);
			PutPoint(pointArray, 1, x2, y2);
			PutPoint(pointArray, 2, x3, y3);

			GeometryUtils.SetWKSPoints(pointCollection, pointArray);

			((ITopologicalOperator) result).Simplify();

			return result;
		}

		/// <summary>
		/// Test helper method to create polyline based on 3 vertices
		/// </summary>
		/// <param name="x1">The x1.</param>
		/// <param name="y1">The y1.</param>
		/// <param name="x2">The x2.</param>
		/// <param name="y2">The y2.</param>
		/// <param name="x3">The x3.</param>
		/// <param name="y3">The y3.</param>
		/// <param name="x4">The x4.</param>
		/// <param name="y4">The y4.</param>
		/// <returns></returns>
		private static IPolyline CreatePolyline(double x1, double y1,
		                                        double x2, double y2,
		                                        double x3, double y3,
		                                        double x4, double y4)
		{
			IPolyline result = new PolylineClass();

			var pointCollection = (IPointCollection4) result;

			var pointArray = new WKSPoint[4];
			PutPoint(pointArray, 0, x1, y1);
			PutPoint(pointArray, 1, x2, y2);
			PutPoint(pointArray, 2, x3, y3);
			PutPoint(pointArray, 3, x4, y4);

			GeometryUtils.SetWKSPoints(pointCollection, pointArray);

			((ITopologicalOperator) result).Simplify();

			return result;
		}

		private static IGeometryBridge _geometryBridge;

		/// <summary>
		/// Gets the geometry bridge (singleton)
		/// </summary>
		/// <value>The geometry bridge.</value>
		private static IGeometryBridge GeometryBridge
		{
			get
			{
				if (_geometryBridge == null)
				{
					Type geometryEnvType =
						Type.GetTypeFromProgID("esriGeometry.GeometryEnvironment");
					_geometryBridge =
						(IGeometryBridge) Activator.CreateInstance(geometryEnvType);
				}

				return _geometryBridge;
			}
		}

		#endregion
	}
}
