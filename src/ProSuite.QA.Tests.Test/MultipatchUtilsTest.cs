using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class MultipatchUtilsTest
	{
		private IMultiPatch _largeData;
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_largeData = GetMultiPatch(50000);

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("TestMultipatchUtils");
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanGetMultiPatchRings()
		{
			List<List<WKSPointVA>> rings = MultiPatchUtils.GetRings(_largeData);
			Assert.IsNotNull(rings);
		}

		[Test]
		public void TestPerformanceIPointsCollection5()
		{
			List<WKSPointVA> pnts = GetFromIPointCollection5(_largeData);
			Assert.IsNotNull(pnts);
		}

		[Test]
		public void CanCreateIndexGeometry()
		{
			// TODO: Test umwandeln in CanGetMultiPatchProxy
			_largeData.SpatialReference = SpatialReferenceUtils.CreateSpatialReference
			(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);
			var geom = new IndexedPolycurve((IPointCollection4) _largeData);

			foreach (SegmentProxy segment in geom.GetSegments())
			{
				Assert.IsTrue(segment.IsLinear);
			}
		}

		[Test]
		public void CanCreateIndexGeometryWithTriangles()
		{
			// TODO: Test umwandeln in CanGetMultiPatchProxyWithTriangles
			var construction = new MultiPatchConstruction();

			construction.StartRing(5, 4, 0).Add(-5, 4, 0).Add(-5, -4, 0).Add(5, -4, 0)
			            .StartFan(5, 4, 1).Add(-5, 4, 1).Add(-5, -4, 1).Add(5, -4, 1)
			            .StartTris(5, 4, 2).Add(-5, 4, 2).Add(-5, -4, 2)
			            .StartStrip(5, 4, 3).Add(-5, 4, 3).Add(-5, -4, 3).Add(5, -4, 3);

			IMultiPatch mp = construction.MultiPatch;

			((IGeometry) mp).SpatialReference = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);

			var geom = new IndexedPolycurve((IPointCollection4) mp);

			foreach (SegmentProxy segment in geom.GetSegments())
			{
				Assert.IsTrue(segment.IsLinear);
			}
		}

		[Test]
		public void CanGetInnerPartIndexes()
		{
			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 0).Add(-5, 4, 0).Add(-5, -4, 0).Add(5, -4, 0)
			            .StartFan(5, 4, 1).Add(-5, 4, 1).Add(-5, -4, 1).Add(5, -4, 1)
			            .StartTris(5, 4, 2).Add(-5, 4, 2).Add(-5, -4, 2)
			            .StartInnerRing(4, 3, 0).Add(-4, 3, 0).Add(-4, -3, 0).Add(4, -3, 0)
			            .StartStrip(5, 4, 3).Add(-5, 4, 3).Add(-5, -4, 3).Add(5, -4, 3);

			IMultiPatch multiPatch = construction.MultiPatch;

			((IGeometry) multiPatch).SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);

			IIndexedMultiPatch indexedMultiPatch =
				QaGeometryUtils.CreateIndexedMultiPatch(multiPatch);

			List<int> innerPartIndexes =
				MultiPatchUtils.GetInnerRingPartIndexes(indexedMultiPatch, 0);
			Assert.AreEqual(0, innerPartIndexes.Count);
		}

		[Test]
		public void CanGetInnerPartIndexesOuterRingInnerRing()
		{
			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 0).Add(-5, 4, 0).Add(-5, -4, 0).Add(5, -4, 0)
			            .StartInnerRing(4, 3, 0).Add(-4, 3, 0).Add(-4, -3, 0).Add(4, -3, 0);

			IMultiPatch multiPatch = construction.MultiPatch;
			((IGeometry) multiPatch).SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);

			IIndexedMultiPatch indexedMultiPatch =
				QaGeometryUtils.CreateIndexedMultiPatch(multiPatch);

			List<int> innerPartIndexes =
				MultiPatchUtils.GetInnerRingPartIndexes(indexedMultiPatch, 0);
			Assert.AreEqual(1, innerPartIndexes.Count);
			Assert.AreEqual(1, innerPartIndexes[0]);
		}

		[Test]
		public void CanGetInnerPartIndexesOuterRingRing()
		{
			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 0).Add(-5, 4, 0).Add(-5, -4, 0).Add(5, -4, 0)
			            .StartRing(4, 3, 0).Add(-4, 3, 0).Add(-4, -3, 0).Add(4, -3, 0);

			IMultiPatch multiPatch = construction.MultiPatch;
			((IGeometry) multiPatch).SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);

			IIndexedMultiPatch indexedMultiPatch =
				QaGeometryUtils.CreateIndexedMultiPatch(multiPatch);

			List<int> innerPartIndexes =
				MultiPatchUtils.GetInnerRingPartIndexes(indexedMultiPatch, 0);
			Assert.AreEqual(0, innerPartIndexes.Count);
		}

		[Test]
		public void CanGetInnerPartIndexesOuterRingFirstRing()
		{
			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 0).Add(-5, 4, 0).Add(-5, -4, 0).Add(5, -4, 0)
			            .StartFirstRing(4, 3, 0).Add(-4, 3, 0).Add(-4, -3, 0).Add(4, -3, 0);

			IMultiPatch multiPatch = construction.MultiPatch;
			((IGeometry) multiPatch).SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);

			IIndexedMultiPatch indexedMultiPatch =
				QaGeometryUtils.CreateIndexedMultiPatch(multiPatch);

			List<int> innerPartIndexes =
				MultiPatchUtils.GetInnerRingPartIndexes(indexedMultiPatch, 0);
			Assert.AreEqual(0, innerPartIndexes.Count);
			innerPartIndexes =
				MultiPatchUtils.GetInnerRingPartIndexes(indexedMultiPatch, 1);
			Assert.AreEqual(0, innerPartIndexes.Count);
		}

		[Test]
		public void CanGetInnerPartIndexesFirstRingRing()
		{
			var construction = new MultiPatchConstruction();

			construction.StartFirstRing(5, 4, 0).Add(-5, 4, 0).Add(-5, -4, 0).Add(5, -4, 0)
			            .StartRing(4, 3, 0).Add(-4, 3, 0).Add(-4, -3, 0).Add(4, -3, 0);

			IMultiPatch multiPatch = construction.MultiPatch;
			((IGeometry) multiPatch).SpatialReference =
				SpatialReferenceUtils.CreateSpatialReference(
					(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95);

			IIndexedMultiPatch indexedMultiPatch =
				QaGeometryUtils.CreateIndexedMultiPatch(multiPatch);

			List<int> innerPartIndexes =
				MultiPatchUtils.GetInnerRingPartIndexes(indexedMultiPatch, 0);
			Assert.AreEqual(1, innerPartIndexes.Count);
			Assert.AreEqual(1, innerPartIndexes[0]);
		}

		[Test]
		public void TestMultipartLines()
		{
			TestMultipartLines(_testWs);
		}

		private static void TestMultipartLines(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestMultipartLines", fields,
				                                      null);
			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			// Crossing parts
			IFeature row1 = fc.CreateFeature();
			row1.Shape = CurveConstruction.StartLine(600000, 200000, 500)
			                              .LineTo(600010, 200010, 505)
			                              .MoveTo(600010, 200000, 600)
			                              .LineTo(600000, 200010, 602)
			                              .Curve;

			row1.Store();
			IFeature copy = fc.CreateFeature();
			copy.Shape = row1.ShapeCopy;
			((ITopologicalOperator) copy.Shape).Simplify();
			copy.Store();

			// 2D identical parts
			IFeature row2 = fc.CreateFeature();
			row2.Shape = CurveConstruction.StartLine(600000, 200000, 500)
			                              .LineTo(600010, 200010, 505)
			                              .MoveTo(600010, 200010, 600)
			                              .LineTo(600000, 200000, 602)
			                              .Curve;

			row2.Store();
			copy = fc.CreateFeature();
			copy.Shape = row1.ShapeCopy;
			((ITopologicalOperator) copy.Shape).Simplify();
			copy.Store();

			// identical parts
			IFeature row = fc.CreateFeature();
			row.Shape = CurveConstruction.StartLine(600000, 200000, 500)
			                             .LineTo(600010, 200010, 505)
			                             .MoveTo(600000, 200000, 500)
			                             .LineTo(600010, 200010, 505)
			                             .Curve;
			row.Store();
			copy = fc.CreateFeature();
			copy.Shape = row1.ShapeCopy;
			((ITopologicalOperator) copy.Shape).Simplify();
			copy.Store();
		}

		[Test]
		public void TestPerformanceEnumVertex()
		{
			List<WKSPointVA> pnts = GetFromEnumVertex(_largeData);
			Assert.IsNotNull(pnts);
		}

		[Test]
		[Ignore("requires connection to TOPGIST")]
		public void TestTopgisTMultiPatches()
		{
			IWorkspace workspace = TestDataUtils.OpenTopgisTlm();

			IFeatureClass gebaeude =
				((IFeatureWorkspace) workspace).OpenFeatureClass(
					"TOPGIS_TLM.TLM_GEBAEUDE");
			var tests = new ITest[]
			            {
				            //			            		new QaSegmentLength(gebaeude, 0.1),
				            //			            		new QaSliverPolygon(gebaeude, 50),
				            //			            		new QaCoplanarRings(gebaeude, 0, false),
				            new QaMpFootprintHoles(ReadOnlyTableFactory.Create(gebaeude),
				                                   InnerRingHandling.IgnoreInnerRings)
			            };
			var runner = new QaContainerTestRunner(10000, tests);
			runner.LogErrors = false;

			runner.Execute();

			Console.WriteLine(runner.Errors.Count);
		}

		[NotNull]
		private static IMultiPatch GetMultiPatch(int pointCount)
		{
			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 0);

			double dx = 10.0 / pointCount;
			for (int i = 1; i < pointCount; i++)
			{
				construction.Add(5 - i * dx, 4, 0);
			}

			construction.Add(-5, 4, 0).Add(-5, -4, 0).Add(5, -4, 0);

			return construction.MultiPatch;
		}

		[NotNull]
		private static List<WKSPointVA> GetFromEnumVertex([NotNull] IMultiPatch multiPatch)
		{
			var pts = (IPointCollection) multiPatch;

			int pointCount = pts.PointCount;
			var result = new List<WKSPointVA>(pointCount);
			IEnumVertex enumVertex = pts.EnumVertices;
			enumVertex.Reset();
			IPoint p = new PointClass();
			int part;
			int vertex;
			for (enumVertex.QueryNext(p, out part, out vertex);
			     vertex >= 0;
			     enumVertex.QueryNext(p, out part, out vertex))
			{
				var pnt = new WKSPointVA();
				double x;
				double y;
				p.QueryCoords(out x, out y);
				pnt.m_x = x;
				pnt.m_y = y;
				pnt.m_z = p.Z;
				pnt.m_m = p.M;
				pnt.m_id = p.ID;

				result.Add(pnt);
			}

			return result;
		}

		[NotNull]
		private static List<WKSPointVA> GetFromIPointCollection5(
			[NotNull] IMultiPatch multiPatch)
		{
			IPointCollection5 mps = new MultipointClass();
			mps.AddPointCollection((IPointCollection) multiPatch);

			int pointCount = mps.PointCount;
			var result = new List<WKSPointVA>(pointCount);
			for (int i = 0; i < pointCount; i++)
			{
				WKSPointVA wks;
				mps.QueryWKSPointVA(i, 1, out wks);
				result.Add(wks);
			}

			return result;
		}
	}
}
