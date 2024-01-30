using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaNoGapsTest
	{
		private IFeatureWorkspace _testWs;
		private bool _failed;
		private const bool _writeErrors = false;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("TestNoGaps");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[SetUp]
		public void SetUp()
		{
			_failed = false;
		}

		[TearDown]
		public void TearDown()
		{
			_failed = false;
		}

		[Test]
		public void TestNoGaps()
		{
			ISpatialReference sref = CreateSpatialReference();
			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.0001, 0.001);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon, sref, 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(_testWs, "TestNoGaps", fields);

			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .LineTo(150, 100)
				                 .LineTo(150, 150)
				                 .LineTo(120, 100)
				                 .ClosePolygon();
			row1.Store();

			IFeature row2 = fc.CreateFeature();
			row2.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(200, 100)
				                 .LineTo(200, 0)
				                 .LineTo(150, 0)
				                 .LineTo(145, 10)
				                 .LineTo(140, 0)
				                 .LineTo(100, 0)
				                 .ClosePolygon();
			row2.Store();

			IEnvelope largeEnv = new EnvelopeClass { SpatialReference = sref };
			largeEnv.PutCoords(-100, 1, 300, 250);

			for (int subdivisions = 0; subdivisions < 4; subdivisions++)
			{
				RunTest(fc, largeEnv, -1, -1, 1, 100, subdivisions);

				RunTest(fc, largeEnv, -1, 500, 0, 100, subdivisions);

				RunTest(fc, largeEnv, -1, 1000, 1, 100, subdivisions);

				RunTest(fc, largeEnv, 30, -1, 0, 100, subdivisions);

				RunTest(fc, largeEnv, 20, -1, 1, 100, subdivisions);

				RunTest(fc, largeEnv, 20, 1000, 1, 100, subdivisions);
			}

			for (double subTileSize = 0;
			     subTileSize < 400;
			     subTileSize = subTileSize + 50)
			{
				RunTest(fc, largeEnv, -1, -1, 1, 100, subTileSize);

				RunTest(fc, largeEnv, -1, 500, 0, 100, subTileSize);

				RunTest(fc, largeEnv, -1, 1000, 1, 100, subTileSize);

				RunTest(fc, largeEnv, 30, -1, 0, 100, subTileSize);

				RunTest(fc, largeEnv, 20, -1, 1, 100, subTileSize);

				RunTest(fc, largeEnv, 20, 1000, 1, 100, subTileSize);
			}

			Assert.IsFalse(_failed, "Unexpected error count");
		}

		[Test]
		public void TestNoArtefacts()
		{
			ISpatialReference sref = CreateSpatialReference();
			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.0001, 0.001);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon, sref, 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(_testWs, "TestNoArtefacts",
				                                      fields);

			/*
			 * ----------
			 * |        |
			 * |  ----  |
			 * |  |  |  |
			 * |  |  |  |
			 * ----  |  |
			 *       |  |
			 *       |  |
			 *       ----
			 */
			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartPoly(0, 200)
				                 .LineTo(100, 200)
				                 .LineTo(100, 0)
				                 .LineTo(80, 0)
				                 .LineTo(80, 120)
				                 .LineTo(20, 120)
				                 .LineTo(20, 80)
				                 .LineTo(0, 80)
				                 .LineTo(0, 100)
				                 .ClosePolygon();
			row1.Store();

			IEnvelope largeEnv = new EnvelopeClass { SpatialReference = sref };
			largeEnv.PutCoords(0, 0, 100, 200);

			const double smallTileSize = 50;
			const double largeTileSize = 100;

			RunTest(fc, largeEnv, -1, 3000, 0, smallTileSize, 0); // limited maxArea
			RunTest(fc, largeEnv, -1, -1, 0, smallTileSize, 0); // unlimited maxArea

			RunTest(fc, largeEnv, -1, -1, 0, largeTileSize, 0); // unlimited maxArea
			RunTest(fc, largeEnv, -1, 1000000, 0, largeTileSize, 0);
			// maxArea > tile area

			for (int subdivisions = 0; subdivisions < 4; subdivisions++)
			{
				RunTest(fc, largeEnv, -1, -1, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, -1, 500, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, -1, 1000, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, 30, -1, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, 20, -1, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, 20, 1000, 0, largeTileSize, subdivisions);
			}

			for (double subTileSize = 0;
			     subTileSize < 200;
			     subTileSize = subTileSize + 50)
			{
				RunTest(fc, largeEnv, -1, -1, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, -1, 500, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, -1, 1000, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, 30, -1, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, 20, -1, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, 20, 1000, 0, largeTileSize, subTileSize);
			}

			Assert.IsFalse(_failed, "Unexpected error count");
		}

		[Test]
		public void TestNoArtefacts2()
		{
			ISpatialReference sref = CreateSpatialReference();
			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.0001, 0.001);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon, sref, 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(_testWs, "TestNoArtefacts2",
				                                      fields);

			/*
			 * ------------
			 * |          |
			 * |  ------  |
			 * |  |    |  |
			 * |  |    ----
			 * |  |
			 * |  |
			 * ----  
			 * 
			 */
			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartPoly(0, 200)
				                 .LineTo(110, 200)
				                 .LineTo(110, 80)
				                 .LineTo(90, 80)
				                 .LineTo(90, 120)
				                 .LineTo(20, 120)
				                 .LineTo(20, 0)
				                 .LineTo(0, 0)
				                 .LineTo(0, 100)
				                 .ClosePolygon();
			row1.Store();

			IEnvelope largeEnv = new EnvelopeClass { SpatialReference = sref };
			largeEnv.PutCoords(0, 0, 200, 200);

			const double smallTileSize = 50;
			const double largeTileSize = 100;

			RunTest(fc, largeEnv, -1, -1, 0, smallTileSize, 0); // unlimited maxArea

			// limited maxArea
			RunTest(fc, largeEnv, -1, 8000, 0, smallTileSize, 0);

			RunTest(fc, largeEnv, -1, -1, 0, largeTileSize, 0); // unlimited maxArea

			// limited maxArea, such that the gap in the first tile (tilesize = 100) is classified as "small"
			RunTest(fc, largeEnv, -1, 8000, 0, largeTileSize, 0);

			for (int subdivisions = 0; subdivisions < 4; subdivisions++)
			{
				RunTest(fc, largeEnv, -1, -1, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, -1, 4000, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, -1, 8000, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, -1, 100000, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, 30, -1, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, 20, -1, 0, largeTileSize, subdivisions);

				RunTest(fc, largeEnv, 20, 8000, 0, largeTileSize, subdivisions);
			}

			for (double subTileSize = 0;
			     subTileSize < 200;
			     subTileSize = subTileSize + 50)
			{
				RunTest(fc, largeEnv, -1, -1, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, -1, 4000, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, -1, 8000, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, -1, 100000, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, 30, -1, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, 20, -1, 0, largeTileSize, subTileSize);

				RunTest(fc, largeEnv, 20, 8000, 0, largeTileSize, subTileSize);
			}

			Assert.IsFalse(_failed, "Unexpected error count");
		}

		[Test]
		public void TestNoArtefacts_DueToClipEnvelopeOffset()
		{
			ISpatialReference sref = CreateSpatialReference();
			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.01, 0.1);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon, sref, 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(_testWs,
				                                      "TestNoArtefacts_DueToClipEnvelopeOffset",
				                                      fields);

			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartPoly(90, 0)
				                 .LineTo(99.85, 50) // does not touch tile envelope
				                 .LineTo(90, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 0)
				                 .LineTo(90, 0)
				                 .ClosePolygon();
			row1.Store();

			IEnvelope testEnvelope = new EnvelopeClass { SpatialReference = sref };
			testEnvelope.PutCoords(0, 0, 200, 200);

			const double tileSize = 100;

			RunTest(fc, testEnvelope, -1, -1, 0, tileSize, 0); // unlimited maxArea

			// limited maxArea
			RunTest(fc, testEnvelope, -1, 8000, 0, tileSize, 0);

			Assert.IsFalse(_failed, "Unexpected error count");
		}

		[Test]
		public void TestNoArtefacts_DueToClipEnvelopeOffset_TileCorner()
		{
			ISpatialReference sref = CreateSpatialReference();
			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.01, 0.1);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon, sref, 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(_testWs,
				                                      "TestNoArtefacts_DueToClipEnvelopeOffset_TileCorner",
				                                      fields);

			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartPoly(200, 90)
				                 .LineTo(99.85, 99.85) // does not touch tile envelope
				                 .LineTo(90, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 90)
				                 .ClosePolygon();
			row1.Store();

			IEnvelope testEnvelope = new EnvelopeClass { SpatialReference = sref };
			testEnvelope.PutCoords(0, 0, 200, 200);

			const double tileSize = 100;

			RunTest(fc, testEnvelope, -1, -1, 0, tileSize, 0); // unlimited maxArea

			// limited maxArea
			RunTest(fc, testEnvelope, -1, 8000, 0, tileSize, 0);

			Assert.IsFalse(_failed, "Unexpected error count");
		}

		[Test]
		public void TestNoArtefacts_DueToClipEnvelopeOffset_TileCorner2()
		{
			ISpatialReference sref = CreateSpatialReference();
			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.01, 0.1);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon, sref, 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(_testWs,
				                                      "TestNoArtefacts_DueToClipEnvelopeOffset_TileCorner2",
				                                      fields);

			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartPoly(200, 90)
				                 .LineTo(100.15, 100.15) // gap overlaps tile envelope
				                 .LineTo(90, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 90)
				                 .ClosePolygon();
			row1.Store();

			IEnvelope testEnvelope = new EnvelopeClass { SpatialReference = sref };
			testEnvelope.PutCoords(0, 0, 200, 200);

			const double tileSize = 100;

			RunTest(fc, testEnvelope, -1, -1, 0, tileSize, 0); // unlimited maxArea

			// limited maxArea
			RunTest(fc, testEnvelope, -1, 8000, 0, tileSize, 0);

			Assert.IsFalse(_failed, "Unexpected error count");
		}

		[Test]
		public void TestNoArtefacts_DueToClipEnvelopeOffset_TileCorner3()
		{
			ISpatialReference sref = CreateSpatialReference();
			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.01, 0.1);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon, sref, 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(_testWs,
				                                      "TestNoArtefacts_DueToClipEnvelopeOffset_2",
				                                      fields);

			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartPoly(90, 0)
				                 .LineTo(100, 50) // touches tile envelope
				                 .LineTo(90, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 0)
				                 .LineTo(90, 0)
				                 .ClosePolygon();
			row1.Store();

			IEnvelope testEnvelope = new EnvelopeClass { SpatialReference = sref };
			testEnvelope.PutCoords(0, 0, 200, 200);

			const double tileSize = 100;

			RunTest(fc, testEnvelope, -1, -1, 0, tileSize, 0); // unlimited maxArea

			// limited maxArea
			RunTest(fc, testEnvelope, -1, 8000, 0, tileSize, 0);

			Assert.IsFalse(_failed, "Unexpected error count");
		}

		[Test]
		public void TestNoGapsBorder()
		{
			ISpatialReference sref = CreateSpatialReference();

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                sref, 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(_testWs, "TestNoGapsBorder",
				                                      fields);

			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .LineTo(150, 100)
				                 .LineTo(150, 150)
				                 .LineTo(120, 100)
				                 .ClosePolygon();
			row1.Store();

			IFeature row2 = fc.CreateFeature();
			row2.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(200, 100)
				                 .LineTo(200, 0)
				                 .LineTo(150, 0)
				                 .LineTo(145, 10)
				                 .LineTo(140, 0)
				                 .LineTo(100, 0)
				                 .ClosePolygon();
			row2.Store();

			// left border
			IFeature row3 = fc.CreateFeature();
			row3.Shape = CurveConstruction.StartPoly(-110, 150)
			                              .LineTo(-90, 160)
			                              .LineTo(-110, 170)
			                              .LineTo(-60, 160)
			                              .ClosePolygon();
			row3.Store();

			// top border
			IFeature row4 = fc.CreateFeature();
			row4.Shape = CurveConstruction.StartPoly(50, 260)
			                              .LineTo(60, 240)
			                              .LineTo(70, 260)
			                              .LineTo(60, 230)
			                              .ClosePolygon();
			row4.Store();

			// right border
			IFeature row5 = fc.CreateFeature();
			row5.Shape = CurveConstruction.StartPoly(310, 150)
			                              .LineTo(260, 160)
			                              .LineTo(310, 170)
			                              .LineTo(290, 160)
			                              .ClosePolygon();
			row5.Store();

			// bottom border
			IFeature row6 = fc.CreateFeature();
			row6.Shape = CurveConstruction.StartPoly(50, -10)
			                              .LineTo(60, 40)
			                              .LineTo(70, -10)
			                              .LineTo(60, 10)
			                              .ClosePolygon();
			row6.Store();

			// between
			IFeature row7 = fc.CreateFeature();
			row7.Shape = CurveConstruction.StartPoly(50, 46)
			                              .LineTo(60, 66)
			                              .LineTo(70, 46)
			                              .LineTo(60, 56)
			                              .ClosePolygon();
			row7.Store();

			IEnvelope largeEnv = new EnvelopeClass();
			largeEnv.PutCoords(-100, 1, 300, 250);

			for (int subdivisions = 0; subdivisions < 1; subdivisions++)
			{
				RunTest(fc, largeEnv, -1, -1, 1, 100, subdivisions);

				IList<IGeometry> errorGeometries = RunTest(fc, largeEnv, -1, -1, 1,
				                                           32.87,
				                                           subdivisions);

				IGeometry geometry = errorGeometries[0];
				Assert.IsNotNull(geometry);
				double area = ((IArea) geometry).Area;
				Assert.IsTrue(Math.Abs(area - 750) < 0.001);
			}

			for (double subTileSize = 0;
			     subTileSize < 400;
			     subTileSize = subTileSize + 50)
			{
				RunTest(fc, largeEnv, -1, -1, 1, 100, subTileSize);

				IList<IGeometry> errorGeometries = RunTest(fc, largeEnv, -1, -1, 1,
				                                           32.87,
				                                           subTileSize, false);

				IGeometry geometry = errorGeometries[0];
				Assert.IsNotNull(geometry);
				double area = ((IArea) geometry).Area;
				Assert.IsTrue(Math.Abs(area - 750) < 0.001);
			}

			Assert.IsFalse(_failed, "Unexpected error count");
		}

		[Test]
		public void ReproClipExceptionTest()
		{
			// from failed test:
			// var polygon = (IPolygon)GeometryUtils.FromXmlString("<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>32793643.734899998</XMin><YMin>5441054.0731000006</YMin><XMax>32795422.098299999</XMax><YMax>5442041.6487000007</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>32795174.235300001</X><Y>5441850.8463000003</Y></Point><Point xsi:type='typens:PointN'><X>32795182.5264</X><Y>5441834.0930000003</Y></Point><Point xsi:type='typens:PointN'><X>32795199.3693</X><Y>5441841.5446000006</Y></Point><Point xsi:type='typens:PointN'><X>32795258.216899998</X><Y>5441892.9964000005</Y></Point><Point xsi:type='typens:PointN'><X>32795281.654100001</X><Y>5441899.9757000003</Y></Point><Point xsi:type='typens:PointN'><X>32795319.254799999</X><Y>5441830.0125999991</Y></Point><Point xsi:type='typens:PointN'><X>32795212.3915</X><Y>5441772.5091999993</Y></Point><Point xsi:type='typens:PointN'><X>32795219.925999999</X><Y>5441740.1579999998</Y></Point><Point xsi:type='typens:PointN'><X>32795251.5636</X><Y>5441741.8465</Y></Point><Point xsi:type='typens:PointN'><X>32795341.8717</X><Y>5441679.6561999992</Y></Point><Point xsi:type='typens:PointN'><X>32795363.9593</X><Y>5441707.9497999996</Y></Point><Point xsi:type='typens:PointN'><X>32795406.203200001</X><Y>5441702.9059999995</Y></Point><Point xsi:type='typens:PointN'><X>32795422.098299999</X><Y>5441688.1972000003</Y></Point><Point xsi:type='typens:PointN'><X>32795388.330499999</X><Y>5441632.1653000005</Y></Point><Point xsi:type='typens:PointN'><X>32795339.213300001</X><Y>5441652.4890000001</Y></Point><Point xsi:type='typens:PointN'><X>32795320.734700002</X><Y>5441625.0457000006</Y></Point><Point xsi:type='typens:PointN'><X>32795196.657400001</X><Y>5441615.4031000007</Y></Point><Point xsi:type='typens:PointN'><X>32795033.660700001</X><Y>5441666.2963999994</Y></Point><Point xsi:type='typens:PointN'><X>32795032.5825</X><Y>5441646.0008000005</Y></Point><Point xsi:type='typens:PointN'><X>32795097.631900001</X><Y>5441591.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32795212.388999999</X><Y>5441524.9702000003</Y></Point><Point xsi:type='typens:PointN'><X>32795138.566399999</X><Y>5441427.3739999998</Y></Point><Point xsi:type='typens:PointN'><X>32795065.968000002</X><Y>5441383.8164000008</Y></Point><Point xsi:type='typens:PointN'><X>32794873.847599998</X><Y>5441473.9607999995</Y></Point><Point xsi:type='typens:PointN'><X>32794841.837899998</X><Y>5441464.3869000003</Y></Point><Point xsi:type='typens:PointN'><X>32794838.430300001</X><Y>5441424.9773999993</Y></Point><Point xsi:type='typens:PointN'><X>32794830.0297</X><Y>5441373.5668000001</Y></Point><Point xsi:type='typens:PointN'><X>32794938.816300001</X><Y>5441309.3344000001</Y></Point><Point xsi:type='typens:PointN'><X>32794919.522799999</X><Y>5441141.7095999997</Y></Point><Point xsi:type='typens:PointN'><X>32794766.7423</X><Y>5441167.0229000002</Y></Point><Point xsi:type='typens:PointN'><X>32794733.4329</X><Y>5441054.0731000006</Y></Point><Point xsi:type='typens:PointN'><X>32794663.666000001</X><Y>5441068.1989999991</Y></Point><Point xsi:type='typens:PointN'><X>32794714.966600001</X><Y>5441221.2898999993</Y></Point><Point xsi:type='typens:PointN'><X>32794672.982099999</X><Y>5441222.3857000005</Y></Point><Point xsi:type='typens:PointN'><X>32794571.566100001</X><Y>5441206.0079999994</Y></Point><Point xsi:type='typens:PointN'><X>32794511.238899998</X><Y>5441201.2249999996</Y></Point><Point xsi:type='typens:PointN'><X>32794433.4505</X><Y>5441216.3864999991</Y></Point><Point xsi:type='typens:PointN'><X>32794274.653499998</X><Y>5441312.1027000006</Y></Point><Point xsi:type='typens:PointN'><X>32794235.395</X><Y>5441298.7367000002</Y></Point><Point xsi:type='typens:PointN'><X>32794212.237</X><Y>5441290.4397</Y></Point><Point xsi:type='typens:PointN'><X>32794230.379900001</X><Y>5441245.1228</Y></Point><Point xsi:type='typens:PointN'><X>32794201.688999999</X><Y>5441222.4119000006</Y></Point><Point xsi:type='typens:PointN'><X>32794122.3902</X><Y>5441306.5242999997</Y></Point><Point xsi:type='typens:PointN'><X>32794067.205400001</X><Y>5441232.5801999997</Y></Point><Point xsi:type='typens:PointN'><X>32794010.414700001</X><Y>5441287.9238000009</Y></Point><Point xsi:type='typens:PointN'><X>32794042.59</X><Y>5441343.3375000004</Y></Point><Point xsi:type='typens:PointN'><X>32794056.6899</X><Y>5441374.7445</Y></Point><Point xsi:type='typens:PointN'><X>32794008.651900001</X><Y>5441390.5569000002</Y></Point><Point xsi:type='typens:PointN'><X>32793903.277400002</X><Y>5441429.4452</Y></Point><Point xsi:type='typens:PointN'><X>32793827.061999999</X><Y>5441423.1009999998</Y></Point><Point xsi:type='typens:PointN'><X>32793765.724399999</X><Y>5441416.7119999994</Y></Point><Point xsi:type='typens:PointN'><X>32793741.764600001</X><Y>5441441.1714999992</Y></Point><Point xsi:type='typens:PointN'><X>32793726.771400001</X><Y>5441439.1162</Y></Point><Point xsi:type='typens:PointN'><X>32793727.339200001</X><Y>5441416.5571999997</Y></Point><Point xsi:type='typens:PointN'><X>32793743.148499999</X><Y>5441397.3149999995</Y></Point><Point xsi:type='typens:PointN'><X>32793694.3785</X><Y>5441388.8638000004</Y></Point><Point xsi:type='typens:PointN'><X>32793671.623999998</X><Y>5441410.7687999997</Y></Point><Point xsi:type='typens:PointN'><X>32793643.734899998</X><Y>5441411.2943999991</Y></Point><Point xsi:type='typens:PointN'><X>32793664.0374</X><Y>5441460.0019000005</Y></Point><Point xsi:type='typens:PointN'><X>32793712.9045</X><Y>5441448.2647999991</Y></Point><Point xsi:type='typens:PointN'><X>32793802.362599999</X><Y>5441474.0327000003</Y></Point><Point xsi:type='typens:PointN'><X>32793893.398699999</X><Y>5441460.8483000007</Y></Point><Point xsi:type='typens:PointN'><X>32793911.490899999</X><Y>5441491.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32793842.899099998</X><Y>5441545.7807999998</Y></Point><Point xsi:type='typens:PointN'><X>32793883.445</X><Y>5441570.8693000004</Y></Point><Point xsi:type='typens:PointN'><X>32793916.962899998</X><Y>5441539.2399000004</Y></Point><Point xsi:type='typens:PointN'><X>32793930.925999999</X><Y>5441522.909</Y></Point><Point xsi:type='typens:PointN'><X>32793942.5953</X><Y>5441540.1004000008</Y></Point><Point xsi:type='typens:PointN'><X>32793987.820299998</X><Y>5441513.3838</Y></Point><Point xsi:type='typens:PointN'><X>32794007.344499998</X><Y>5441534.5032000002</Y></Point><Point xsi:type='typens:PointN'><X>32794051.809100002</X><Y>5441503.2223000005</Y></Point><Point xsi:type='typens:PointN'><X>32794030.462499999</X><Y>5441478.4956</Y></Point><Point xsi:type='typens:PointN'><X>32794081.765299998</X><Y>5441460.6392999999</Y></Point><Point xsi:type='typens:PointN'><X>32794125.6472</X><Y>5441430.8323999997</Y></Point><Point xsi:type='typens:PointN'><X>32794165.025399998</X><Y>5441409.8354000002</Y></Point><Point xsi:type='typens:PointN'><X>32794181.6032</X><Y>5441428.2355000004</Y></Point><Point xsi:type='typens:PointN'><X>32794206.094900001</X><Y>5441458.5461999997</Y></Point><Point xsi:type='typens:PointN'><X>32794185.984000001</X><Y>5441479.7317999993</Y></Point><Point xsi:type='typens:PointN'><X>32794182.932700001</X><Y>5441503.6409000009</Y></Point><Point xsi:type='typens:PointN'><X>32794193.656500001</X><Y>5441524.8278999999</Y></Point><Point xsi:type='typens:PointN'><X>32794231.297899999</X><Y>5441508.2523999996</Y></Point><Point xsi:type='typens:PointN'><X>32794239.458999999</X><Y>5441525.7037000004</Y></Point><Point xsi:type='typens:PointN'><X>32794244.9421</X><Y>5441554.6692999993</Y></Point><Point xsi:type='typens:PointN'><X>32794226.599600002</X><Y>5441562.5756999999</Y></Point><Point xsi:type='typens:PointN'><X>32794119.367600001</X><Y>5441604.8714000005</Y></Point><Point xsi:type='typens:PointN'><X>32794171.8814</X><Y>5441691.6239999998</Y></Point><Point xsi:type='typens:PointN'><X>32794248.446600001</X><Y>5441667.2337999996</Y></Point><Point xsi:type='typens:PointN'><X>32794427.070299998</X><Y>5441685.9543999992</Y></Point><Point xsi:type='typens:PointN'><X>32794548.1261</X><Y>5441655.0434000008</Y></Point><Point xsi:type='typens:PointN'><X>32794568.2544</X><Y>5441638.6411000006</Y></Point><Point xsi:type='typens:PointN'><X>32794550.533</X><Y>5441602.1360999998</Y></Point><Point xsi:type='typens:PointN'><X>32794611.829800002</X><Y>5441595.7828000002</Y></Point><Point xsi:type='typens:PointN'><X>32794624.5854</X><Y>5441611.5179999992</Y></Point><Point xsi:type='typens:PointN'><X>32794648.952799998</X><Y>5441607.8008999992</Y></Point><Point xsi:type='typens:PointN'><X>32794676.268300001</X><Y>5441633.8512999993</Y></Point><Point xsi:type='typens:PointN'><X>32794700.6818</X><Y>5441629.1338999998</Y></Point><Point xsi:type='typens:PointN'><X>32794720.659400001</X><Y>5441632.2041999996</Y></Point><Point xsi:type='typens:PointN'><X>32794694.076400001</X><Y>5441688.3705000002</Y></Point><Point xsi:type='typens:PointN'><X>32794681.592700001</X><Y>5441713.4598999992</Y></Point><Point xsi:type='typens:PointN'><X>32794642.5559</X><Y>5441718.4124999996</Y></Point><Point xsi:type='typens:PointN'><X>32794638.275699999</X><Y>5441741.0192000009</Y></Point><Point xsi:type='typens:PointN'><X>32794582.310199998</X><Y>5441762.3999000005</Y></Point><Point xsi:type='typens:PointN'><X>32794559.363600001</X><Y>5441772.0436000004</Y></Point><Point xsi:type='typens:PointN'><X>32794580.201299999</X><Y>5441811.4894999992</Y></Point><Point xsi:type='typens:PointN'><X>32794581.477400001</X><Y>5441844.1878999993</Y></Point><Point xsi:type='typens:PointN'><X>32794547.189100001</X><Y>5441863.4134999998</Y></Point><Point xsi:type='typens:PointN'><X>32794564.789499998</X><Y>5441883.4630999994</Y></Point><Point xsi:type='typens:PointN'><X>32794625.989</X><Y>5441874.2074999996</Y></Point><Point xsi:type='typens:PointN'><X>32794644.362099998</X><Y>5441885.3830999993</Y></Point><Point xsi:type='typens:PointN'><X>32794649.1558</X><Y>5441896.9945</Y></Point><Point xsi:type='typens:PointN'><X>32794666.908300001</X><Y>5441902.4982999992</Y></Point><Point xsi:type='typens:PointN'><X>32794667.924599998</X><Y>5441884.9332999997</Y></Point><Point xsi:type='typens:PointN'><X>32794678.877900001</X><Y>5441868.7668999992</Y></Point><Point xsi:type='typens:PointN'><X>32794666.000500001</X><Y>5441826.6018000003</Y></Point><Point xsi:type='typens:PointN'><X>32794684.857999999</X><Y>5441784.4222999997</Y></Point><Point xsi:type='typens:PointN'><X>32794687.1884</X><Y>5441756.7638000008</Y></Point><Point xsi:type='typens:PointN'><X>32794723.055100001</X><Y>5441776.1153999995</Y></Point><Point xsi:type='typens:PointN'><X>32794738.9606</X><Y>5441754.5741000008</Y></Point><Point xsi:type='typens:PointN'><X>32794737.793700002</X><Y>5441716.9556000009</Y></Point><Point xsi:type='typens:PointN'><X>32794701.966800001</X><Y>5441701.7962999996</Y></Point><Point xsi:type='typens:PointN'><X>32794721.878699999</X><Y>5441658.2577</Y></Point><Point xsi:type='typens:PointN'><X>32794777.269000001</X><Y>5441704.6049000006</Y></Point><Point xsi:type='typens:PointN'><X>32794802.555199999</X><Y>5441757.3052999992</Y></Point><Point xsi:type='typens:PointN'><X>32794788.6241</X><Y>5441842.9425000008</Y></Point><Point xsi:type='typens:PointN'><X>32794790.782099999</X><Y>5441875.4204999991</Y></Point><Point xsi:type='typens:PointN'><X>32794803.455800001</X><Y>5441892.6676000003</Y></Point><Point xsi:type='typens:PointN'><X>32794775.923999999</X><Y>5441902.7346000001</Y></Point><Point xsi:type='typens:PointN'><X>32794799.538699999</X><Y>5441957.5080999993</Y></Point><Point xsi:type='typens:PointN'><X>32794833.919</X><Y>5442010.7605000008</Y></Point><Point xsi:type='typens:PointN'><X>32794895.6996</X><Y>5442020.1275999993</Y></Point><Point xsi:type='typens:PointN'><X>32794941.989399999</X><Y>5441986.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32794922.974799998</X><Y>5441927.5124999993</Y></Point><Point xsi:type='typens:PointN'><X>32794960.9023</X><Y>5441916.5629999992</Y></Point><Point xsi:type='typens:PointN'><X>32794971.965</X><Y>5441871.1712999996</Y></Point><Point xsi:type='typens:PointN'><X>32794915.513700001</X><Y>5441851.7902000006</Y></Point><Point xsi:type='typens:PointN'><X>32794908.172399998</X><Y>5441804.7263999991</Y></Point><Point xsi:type='typens:PointN'><X>32794954.2808</X><Y>5441817.8276000004</Y></Point><Point xsi:type='typens:PointN'><X>32795005.642999999</X><Y>5441840.2140999995</Y></Point><Point xsi:type='typens:PointN'><X>32795013.368500002</X><Y>5441901.6191000007</Y></Point><Point xsi:type='typens:PointN'><X>32795021.5407</X><Y>5441953.4080999997</Y></Point><Point xsi:type='typens:PointN'><X>32795056.630599998</X><Y>5442041.6487000007</Y></Point><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32795227.480599999</X><Y>5441899.5470000003</Y></Point><Point xsi:type='typens:PointN'><X>32795174.235300001</X><Y>5441850.8463000003</Y></Point></PointArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;ETRS_1989_UTM_Zone_32N_8stellen&quot;,GEOGCS[&quot;GCS_ETRS_1989&quot;,DATUM[&quot;D_ETRS_1989&quot;,SPHEROID[&quot;GRS_1980&quot;,6378137.0,298.257222101]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Transverse_Mercator&quot;],PARAMETER[&quot;False_Easting&quot;,32500000.0],PARAMETER[&quot;False_Northing&quot;,0.0],PARAMETER[&quot;Central_Meridian&quot;,9.0],PARAMETER[&quot;Scale_Factor&quot;,0.9996],PARAMETER[&quot;Latitude_Of_Origin&quot;,0.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;ESRI&quot;,102329]]</WKT><XOrigin>26879100</XOrigin><YOrigin>-9998100</YOrigin><XYScale>10000</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>102329</WKID></SpatialReference></PolygonN>");

			// feature (original)
			// var polygon = (IPolygon)GeometryUtils.FromXmlString("<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>32793643.734899998</XMin><YMin>5441054.0731000006</YMin><XMax>32795422.098299999</XMax><YMax>5442041.6487000007</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32795227.480599999</X><Y>5441899.5470000003</Y></Point><Point xsi:type='typens:PointN'><X>32795174.235300001</X><Y>5441850.8463000003</Y></Point><Point xsi:type='typens:PointN'><X>32795182.5264</X><Y>5441834.0930000003</Y></Point><Point xsi:type='typens:PointN'><X>32795199.3693</X><Y>5441841.5446000006</Y></Point><Point xsi:type='typens:PointN'><X>32795258.216899998</X><Y>5441892.9964000005</Y></Point><Point xsi:type='typens:PointN'><X>32795281.654100001</X><Y>5441899.9757000003</Y></Point><Point xsi:type='typens:PointN'><X>32795319.254799999</X><Y>5441830.0125999991</Y></Point><Point xsi:type='typens:PointN'><X>32795212.3915</X><Y>5441772.5091999993</Y></Point><Point xsi:type='typens:PointN'><X>32795219.925999999</X><Y>5441740.1579999998</Y></Point><Point xsi:type='typens:PointN'><X>32795251.5636</X><Y>5441741.8465</Y></Point><Point xsi:type='typens:PointN'><X>32795341.8717</X><Y>5441679.6561999992</Y></Point><Point xsi:type='typens:PointN'><X>32795363.9593</X><Y>5441707.9497999996</Y></Point><Point xsi:type='typens:PointN'><X>32795406.203200001</X><Y>5441702.9059999995</Y></Point><Point xsi:type='typens:PointN'><X>32795422.098299999</X><Y>5441688.1972000003</Y></Point><Point xsi:type='typens:PointN'><X>32795388.330499999</X><Y>5441632.1653000005</Y></Point><Point xsi:type='typens:PointN'><X>32795339.213300001</X><Y>5441652.4890000001</Y></Point><Point xsi:type='typens:PointN'><X>32795320.734700002</X><Y>5441625.0457000006</Y></Point><Point xsi:type='typens:PointN'><X>32795196.657400001</X><Y>5441615.4031000007</Y></Point><Point xsi:type='typens:PointN'><X>32795033.660700001</X><Y>5441666.2963999994</Y></Point><Point xsi:type='typens:PointN'><X>32795032.5825</X><Y>5441646.0008000005</Y></Point><Point xsi:type='typens:PointN'><X>32795097.631900001</X><Y>5441591.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32795212.388999999</X><Y>5441524.9702000003</Y></Point><Point xsi:type='typens:PointN'><X>32795138.566399999</X><Y>5441427.3739999998</Y></Point><Point xsi:type='typens:PointN'><X>32795065.968000002</X><Y>5441383.8164000008</Y></Point><Point xsi:type='typens:PointN'><X>32794873.847599998</X><Y>5441473.9607999995</Y></Point><Point xsi:type='typens:PointN'><X>32794841.837899998</X><Y>5441464.3869000003</Y></Point><Point xsi:type='typens:PointN'><X>32794838.430300001</X><Y>5441424.9773999993</Y></Point><Point xsi:type='typens:PointN'><X>32794830.0297</X><Y>5441373.5668000001</Y></Point><Point xsi:type='typens:PointN'><X>32794938.816300001</X><Y>5441309.3344000001</Y></Point><Point xsi:type='typens:PointN'><X>32794919.522799999</X><Y>5441141.7095999997</Y></Point><Point xsi:type='typens:PointN'><X>32794766.7423</X><Y>5441167.0229000002</Y></Point><Point xsi:type='typens:PointN'><X>32794733.4329</X><Y>5441054.0731000006</Y></Point><Point xsi:type='typens:PointN'><X>32794663.666000001</X><Y>5441068.1989999991</Y></Point><Point xsi:type='typens:PointN'><X>32794714.966600001</X><Y>5441221.2898999993</Y></Point><Point xsi:type='typens:PointN'><X>32794672.982099999</X><Y>5441222.3857000005</Y></Point><Point xsi:type='typens:PointN'><X>32794571.566100001</X><Y>5441206.0079999994</Y></Point><Point xsi:type='typens:PointN'><X>32794511.238899998</X><Y>5441201.2249999996</Y></Point><Point xsi:type='typens:PointN'><X>32794433.4505</X><Y>5441216.3864999991</Y></Point><Point xsi:type='typens:PointN'><X>32794274.653499998</X><Y>5441312.1027000006</Y></Point><Point xsi:type='typens:PointN'><X>32794235.395</X><Y>5441298.7367000002</Y></Point><Point xsi:type='typens:PointN'><X>32794212.237</X><Y>5441290.4397</Y></Point><Point xsi:type='typens:PointN'><X>32794230.379900001</X><Y>5441245.1228</Y></Point><Point xsi:type='typens:PointN'><X>32794201.688999999</X><Y>5441222.4119000006</Y></Point><Point xsi:type='typens:PointN'><X>32794122.3902</X><Y>5441306.5242999997</Y></Point><Point xsi:type='typens:PointN'><X>32794067.205400001</X><Y>5441232.5801999997</Y></Point><Point xsi:type='typens:PointN'><X>32794010.414700001</X><Y>5441287.9238000009</Y></Point><Point xsi:type='typens:PointN'><X>32794042.59</X><Y>5441343.3375000004</Y></Point><Point xsi:type='typens:PointN'><X>32794056.6899</X><Y>5441374.7445</Y></Point><Point xsi:type='typens:PointN'><X>32794008.651900001</X><Y>5441390.5569000002</Y></Point><Point xsi:type='typens:PointN'><X>32793903.277400002</X><Y>5441429.4452</Y></Point><Point xsi:type='typens:PointN'><X>32793827.061999999</X><Y>5441423.1009999998</Y></Point><Point xsi:type='typens:PointN'><X>32793765.724399999</X><Y>5441416.7119999994</Y></Point><Point xsi:type='typens:PointN'><X>32793741.764600001</X><Y>5441441.1714999992</Y></Point><Point xsi:type='typens:PointN'><X>32793726.771400001</X><Y>5441439.1162</Y></Point><Point xsi:type='typens:PointN'><X>32793727.339200001</X><Y>5441416.5571999997</Y></Point><Point xsi:type='typens:PointN'><X>32793743.148499999</X><Y>5441397.3149999995</Y></Point><Point xsi:type='typens:PointN'><X>32793694.3785</X><Y>5441388.8638000004</Y></Point><Point xsi:type='typens:PointN'><X>32793671.623999998</X><Y>5441410.7687999997</Y></Point><Point xsi:type='typens:PointN'><X>32793643.734899998</X><Y>5441411.2943999991</Y></Point><Point xsi:type='typens:PointN'><X>32793664.0374</X><Y>5441460.0019000005</Y></Point><Point xsi:type='typens:PointN'><X>32793712.9045</X><Y>5441448.2647999991</Y></Point><Point xsi:type='typens:PointN'><X>32793802.362599999</X><Y>5441474.0327000003</Y></Point><Point xsi:type='typens:PointN'><X>32793893.398699999</X><Y>5441460.8483000007</Y></Point><Point xsi:type='typens:PointN'><X>32793911.490899999</X><Y>5441491.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32793842.899099998</X><Y>5441545.7807999998</Y></Point><Point xsi:type='typens:PointN'><X>32793883.445</X><Y>5441570.8693000004</Y></Point><Point xsi:type='typens:PointN'><X>32793916.962899998</X><Y>5441539.2399000004</Y></Point><Point xsi:type='typens:PointN'><X>32793930.925999999</X><Y>5441522.909</Y></Point><Point xsi:type='typens:PointN'><X>32793942.5953</X><Y>5441540.1004000008</Y></Point><Point xsi:type='typens:PointN'><X>32793987.820299998</X><Y>5441513.3838</Y></Point><Point xsi:type='typens:PointN'><X>32794007.344499998</X><Y>5441534.5032000002</Y></Point><Point xsi:type='typens:PointN'><X>32794051.809100002</X><Y>5441503.2223000005</Y></Point><Point xsi:type='typens:PointN'><X>32794030.462499999</X><Y>5441478.4956</Y></Point><Point xsi:type='typens:PointN'><X>32794081.765299998</X><Y>5441460.6392999999</Y></Point><Point xsi:type='typens:PointN'><X>32794125.6472</X><Y>5441430.8323999997</Y></Point><Point xsi:type='typens:PointN'><X>32794165.025399998</X><Y>5441409.8354000002</Y></Point><Point xsi:type='typens:PointN'><X>32794181.6032</X><Y>5441428.2355000004</Y></Point><Point xsi:type='typens:PointN'><X>32794206.094900001</X><Y>5441458.5461999997</Y></Point><Point xsi:type='typens:PointN'><X>32794185.984000001</X><Y>5441479.7317999993</Y></Point><Point xsi:type='typens:PointN'><X>32794182.932700001</X><Y>5441503.6409000009</Y></Point><Point xsi:type='typens:PointN'><X>32794193.656500001</X><Y>5441524.8278999999</Y></Point><Point xsi:type='typens:PointN'><X>32794231.297899999</X><Y>5441508.2523999996</Y></Point><Point xsi:type='typens:PointN'><X>32794239.458999999</X><Y>5441525.7037000004</Y></Point><Point xsi:type='typens:PointN'><X>32794244.9421</X><Y>5441554.6692999993</Y></Point><Point xsi:type='typens:PointN'><X>32794226.599600002</X><Y>5441562.5756999999</Y></Point><Point xsi:type='typens:PointN'><X>32794119.367600001</X><Y>5441604.8714000005</Y></Point><Point xsi:type='typens:PointN'><X>32794171.8814</X><Y>5441691.6239999998</Y></Point><Point xsi:type='typens:PointN'><X>32794248.446600001</X><Y>5441667.2337999996</Y></Point><Point xsi:type='typens:PointN'><X>32794427.070299998</X><Y>5441685.9543999992</Y></Point><Point xsi:type='typens:PointN'><X>32794548.1261</X><Y>5441655.0434000008</Y></Point><Point xsi:type='typens:PointN'><X>32794568.2544</X><Y>5441638.6411000006</Y></Point><Point xsi:type='typens:PointN'><X>32794550.533</X><Y>5441602.1360999998</Y></Point><Point xsi:type='typens:PointN'><X>32794611.829800002</X><Y>5441595.7828000002</Y></Point><Point xsi:type='typens:PointN'><X>32794624.5854</X><Y>5441611.5179999992</Y></Point><Point xsi:type='typens:PointN'><X>32794648.952799998</X><Y>5441607.8008999992</Y></Point><Point xsi:type='typens:PointN'><X>32794676.268300001</X><Y>5441633.8512999993</Y></Point><Point xsi:type='typens:PointN'><X>32794700.6818</X><Y>5441629.1338999998</Y></Point><Point xsi:type='typens:PointN'><X>32794720.659400001</X><Y>5441632.2041999996</Y></Point><Point xsi:type='typens:PointN'><X>32794694.076400001</X><Y>5441688.3705000002</Y></Point><Point xsi:type='typens:PointN'><X>32794681.592700001</X><Y>5441713.4598999992</Y></Point><Point xsi:type='typens:PointN'><X>32794642.5559</X><Y>5441718.4124999996</Y></Point><Point xsi:type='typens:PointN'><X>32794638.275699999</X><Y>5441741.0192000009</Y></Point><Point xsi:type='typens:PointN'><X>32794582.310199998</X><Y>5441762.3999000005</Y></Point><Point xsi:type='typens:PointN'><X>32794559.363600001</X><Y>5441772.0436000004</Y></Point><Point xsi:type='typens:PointN'><X>32794580.201299999</X><Y>5441811.4894999992</Y></Point><Point xsi:type='typens:PointN'><X>32794581.477400001</X><Y>5441844.1878999993</Y></Point><Point xsi:type='typens:PointN'><X>32794547.189100001</X><Y>5441863.4134999998</Y></Point><Point xsi:type='typens:PointN'><X>32794564.789499998</X><Y>5441883.4630999994</Y></Point><Point xsi:type='typens:PointN'><X>32794625.989</X><Y>5441874.2074999996</Y></Point><Point xsi:type='typens:PointN'><X>32794644.362099998</X><Y>5441885.3830999993</Y></Point><Point xsi:type='typens:PointN'><X>32794649.1558</X><Y>5441896.9945</Y></Point><Point xsi:type='typens:PointN'><X>32794666.908300001</X><Y>5441902.4982999992</Y></Point><Point xsi:type='typens:PointN'><X>32794667.924599998</X><Y>5441884.9332999997</Y></Point><Point xsi:type='typens:PointN'><X>32794678.877900001</X><Y>5441868.7668999992</Y></Point><Point xsi:type='typens:PointN'><X>32794666.000500001</X><Y>5441826.6018000003</Y></Point><Point xsi:type='typens:PointN'><X>32794684.857999999</X><Y>5441784.4222999997</Y></Point><Point xsi:type='typens:PointN'><X>32794687.1884</X><Y>5441756.7638000008</Y></Point><Point xsi:type='typens:PointN'><X>32794723.055100001</X><Y>5441776.1153999995</Y></Point><Point xsi:type='typens:PointN'><X>32794738.9606</X><Y>5441754.5741000008</Y></Point><Point xsi:type='typens:PointN'><X>32794737.793700002</X><Y>5441716.9556000009</Y></Point><Point xsi:type='typens:PointN'><X>32794701.966800001</X><Y>5441701.7962999996</Y></Point><Point xsi:type='typens:PointN'><X>32794721.878699999</X><Y>5441658.2577</Y></Point><Point xsi:type='typens:PointN'><X>32794777.269000001</X><Y>5441704.6049000006</Y></Point><Point xsi:type='typens:PointN'><X>32794802.555199999</X><Y>5441757.3052999992</Y></Point><Point xsi:type='typens:PointN'><X>32794788.6241</X><Y>5441842.9425000008</Y></Point><Point xsi:type='typens:PointN'><X>32794790.782099999</X><Y>5441875.4204999991</Y></Point><Point xsi:type='typens:PointN'><X>32794803.455800001</X><Y>5441892.6676000003</Y></Point><Point xsi:type='typens:PointN'><X>32794775.923999999</X><Y>5441902.7346000001</Y></Point><Point xsi:type='typens:PointN'><X>32794799.538699999</X><Y>5441957.5080999993</Y></Point><Point xsi:type='typens:PointN'><X>32794833.919</X><Y>5442010.7605000008</Y></Point><Point xsi:type='typens:PointN'><X>32794895.6996</X><Y>5442020.1275999993</Y></Point><Point xsi:type='typens:PointN'><X>32794941.989399999</X><Y>5441986.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32794922.974799998</X><Y>5441927.5124999993</Y></Point><Point xsi:type='typens:PointN'><X>32794960.9023</X><Y>5441916.5629999992</Y></Point><Point xsi:type='typens:PointN'><X>32794971.965</X><Y>5441871.1712999996</Y></Point><Point xsi:type='typens:PointN'><X>32794915.513700001</X><Y>5441851.7902000006</Y></Point><Point xsi:type='typens:PointN'><X>32794908.172399998</X><Y>5441804.7263999991</Y></Point><Point xsi:type='typens:PointN'><X>32794954.2808</X><Y>5441817.8276000004</Y></Point><Point xsi:type='typens:PointN'><X>32795005.642999999</X><Y>5441840.2140999995</Y></Point><Point xsi:type='typens:PointN'><X>32795013.368500002</X><Y>5441901.6191000007</Y></Point><Point xsi:type='typens:PointN'><X>32795021.5407</X><Y>5441953.4080999997</Y></Point><Point xsi:type='typens:PointN'><X>32795056.630599998</X><Y>5442041.6487000007</Y></Point><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point></PointArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;ETRS_1989_UTM_Zone_32N_8stellen&quot;,GEOGCS[&quot;GCS_ETRS_1989&quot;,DATUM[&quot;D_ETRS_1989&quot;,SPHEROID[&quot;GRS_1980&quot;,6378137.0,298.257222101]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Transverse_Mercator&quot;],PARAMETER[&quot;False_Easting&quot;,32500000.0],PARAMETER[&quot;False_Northing&quot;,0.0],PARAMETER[&quot;Central_Meridian&quot;,9.0],PARAMETER[&quot;Scale_Factor&quot;,0.9996],PARAMETER[&quot;Latitude_Of_Origin&quot;,0.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;ESRI&quot;,102329]]</WKT><XOrigin>26879100</XOrigin><YOrigin>-9998100</YOrigin><XYScale>450445547.3910538</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>102329</WKID></SpatialReference></PolygonN>");

			// feature (one vertex moved)
			var polygon =
				(IPolygon)
				GeometryUtils.FromXmlString(
					"<PolygonN xsi:type='typens:PolygonN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><HasID>false</HasID><HasZ>false</HasZ><HasM>false</HasM><Extent xsi:type='typens:EnvelopeN'><XMin>32793643.734899998</XMin><YMin>5441054.0731000006</YMin><XMax>32795422.098299999</XMax><YMax>5442041.6487000007</YMax></Extent><RingArray xsi:type='typens:ArrayOfRing'><Ring xsi:type='typens:Ring'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32795227.480599999</X><Y>5441899.5470000003</Y></Point><Point xsi:type='typens:PointN'><X>32795174.235300001</X><Y>5441850.8463000003</Y></Point><Point xsi:type='typens:PointN'><X>32795182.5264</X><Y>5441834.0930000003</Y></Point><Point xsi:type='typens:PointN'><X>32795199.3693</X><Y>5441841.5446000006</Y></Point><Point xsi:type='typens:PointN'><X>32795258.216899998</X><Y>5441892.9964000005</Y></Point><Point xsi:type='typens:PointN'><X>32795281.654100001</X><Y>5441899.9757000003</Y></Point><Point xsi:type='typens:PointN'><X>32795319.254799999</X><Y>5441830.0125999991</Y></Point><Point xsi:type='typens:PointN'><X>32795212.3915</X><Y>5441772.5091999993</Y></Point><Point xsi:type='typens:PointN'><X>32795219.925999999</X><Y>5441740.1579999998</Y></Point><Point xsi:type='typens:PointN'><X>32795251.5636</X><Y>5441741.8465</Y></Point><Point xsi:type='typens:PointN'><X>32795341.8717</X><Y>5441679.6561999992</Y></Point><Point xsi:type='typens:PointN'><X>32795363.9593</X><Y>5441707.9497999996</Y></Point><Point xsi:type='typens:PointN'><X>32795406.203200001</X><Y>5441702.9059999995</Y></Point><Point xsi:type='typens:PointN'><X>32795422.098299999</X><Y>5441688.1972000003</Y></Point><Point xsi:type='typens:PointN'><X>32795388.330499999</X><Y>5441632.1653000005</Y></Point><Point xsi:type='typens:PointN'><X>32795339.213300001</X><Y>5441652.4890000001</Y></Point><Point xsi:type='typens:PointN'><X>32795320.734700002</X><Y>5441625.0457000006</Y></Point><Point xsi:type='typens:PointN'><X>32795196.657400001</X><Y>5441615.4031000007</Y></Point><Point xsi:type='typens:PointN'><X>32795033.660700001</X><Y>5441666.2963999994</Y></Point><Point xsi:type='typens:PointN'><X>32795032.5825</X><Y>5441646.0008000005</Y></Point><Point xsi:type='typens:PointN'><X>32795097.631900001</X><Y>5441591.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32795212.388999999</X><Y>5441524.9702000003</Y></Point><Point xsi:type='typens:PointN'><X>32795138.566399999</X><Y>5441427.3739999998</Y></Point><Point xsi:type='typens:PointN'><X>32795065.968000002</X><Y>5441383.8164000008</Y></Point><Point xsi:type='typens:PointN'><X>32794873.847599998</X><Y>5441473.9607999995</Y></Point><Point xsi:type='typens:PointN'><X>32794841.837899998</X><Y>5441464.3869000003</Y></Point><Point xsi:type='typens:PointN'><X>32794838.430300001</X><Y>5441424.9773999993</Y></Point><Point xsi:type='typens:PointN'><X>32794830.0297</X><Y>5441373.5668000001</Y></Point><Point xsi:type='typens:PointN'><X>32794938.816300001</X><Y>5441309.3344000001</Y></Point><Point xsi:type='typens:PointN'><X>32794919.522799999</X><Y>5441141.7095999997</Y></Point><Point xsi:type='typens:PointN'><X>32794766.7423</X><Y>5441167.0229000002</Y></Point><Point xsi:type='typens:PointN'><X>32794733.4329</X><Y>5441054.0731000006</Y></Point><Point xsi:type='typens:PointN'><X>32794663.666000001</X><Y>5441068.1989999991</Y></Point><Point xsi:type='typens:PointN'><X>32794714.966600001</X><Y>5441221.2898999993</Y></Point><Point xsi:type='typens:PointN'><X>32794672.982099999</X><Y>5441222.3857000005</Y></Point><Point xsi:type='typens:PointN'><X>32794571.566100001</X><Y>5441206.0079999994</Y></Point><Point xsi:type='typens:PointN'><X>32794511.238899998</X><Y>5441201.2249999996</Y></Point><Point xsi:type='typens:PointN'><X>32794433.4505</X><Y>5441216.3864999991</Y></Point><Point xsi:type='typens:PointN'><X>32794274.653499998</X><Y>5441312.1027000006</Y></Point><Point xsi:type='typens:PointN'><X>32794235.395</X><Y>5441298.7367000002</Y></Point><Point xsi:type='typens:PointN'><X>32794212.237</X><Y>5441290.4397</Y></Point><Point xsi:type='typens:PointN'><X>32794230.379900001</X><Y>5441245.1228</Y></Point><Point xsi:type='typens:PointN'><X>32794201.688999999</X><Y>5441222.4119000006</Y></Point><Point xsi:type='typens:PointN'><X>32794122.3902</X><Y>5441306.5242999997</Y></Point><Point xsi:type='typens:PointN'><X>32794067.205400001</X><Y>5441232.5801999997</Y></Point><Point xsi:type='typens:PointN'><X>32794010.414700001</X><Y>5441287.9238000009</Y></Point><Point xsi:type='typens:PointN'><X>32794042.59</X><Y>5441343.3375000004</Y></Point><Point xsi:type='typens:PointN'><X>32794056.6899</X><Y>5441374.7445</Y></Point><Point xsi:type='typens:PointN'><X>32794008.651900001</X><Y>5441390.5569000002</Y></Point><Point xsi:type='typens:PointN'><X>32793903.277400002</X><Y>5441429.4452</Y></Point><Point xsi:type='typens:PointN'><X>32793827.061999999</X><Y>5441423.1009999998</Y></Point><Point xsi:type='typens:PointN'><X>32793765.724399999</X><Y>5441416.7119999994</Y></Point><Point xsi:type='typens:PointN'><X>32793741.764600001</X><Y>5441441.1714999992</Y></Point><Point xsi:type='typens:PointN'><X>32793726.771400001</X><Y>5441439.1162</Y></Point><Point xsi:type='typens:PointN'><X>32793727.339200001</X><Y>5441416.5571999997</Y></Point><Point xsi:type='typens:PointN'><X>32793743.148499999</X><Y>5441397.3149999995</Y></Point><Point xsi:type='typens:PointN'><X>32793694.3785</X><Y>5441388.8638000004</Y></Point><Point xsi:type='typens:PointN'><X>32793671.623999998</X><Y>5441410.7687999997</Y></Point><Point xsi:type='typens:PointN'><X>32793643.734899998</X><Y>5441411.2943999991</Y></Point><Point xsi:type='typens:PointN'><X>32793664.0374</X><Y>5441460.0019000005</Y></Point><Point xsi:type='typens:PointN'><X>32793712.9045</X><Y>5441448.2647999991</Y></Point><Point xsi:type='typens:PointN'><X>32793802.362599999</X><Y>5441474.0327000003</Y></Point><Point xsi:type='typens:PointN'><X>32793893.398699999</X><Y>5441460.8483000007</Y></Point><Point xsi:type='typens:PointN'><X>32793911.490899999</X><Y>5441491.2857000008</Y></Point><Point xsi:type='typens:PointN'><X>32793842.899099998</X><Y>5441545.7807999998</Y></Point><Point xsi:type='typens:PointN'><X>32793883.445</X><Y>5441570.8693000004</Y></Point><Point xsi:type='typens:PointN'><X>32793916.962899998</X><Y>5441539.2399000004</Y></Point><Point xsi:type='typens:PointN'><X>32793930.925999999</X><Y>5441522.909</Y></Point><Point xsi:type='typens:PointN'><X>32793942.5953</X><Y>5441540.1004000008</Y></Point><Point xsi:type='typens:PointN'><X>32793987.820299998</X><Y>5441513.3838</Y></Point><Point xsi:type='typens:PointN'><X>32794007.344499998</X><Y>5441534.5032000002</Y></Point><Point xsi:type='typens:PointN'><X>32794051.809100002</X><Y>5441503.2223000005</Y></Point><Point xsi:type='typens:PointN'><X>32794030.462499999</X><Y>5441478.4956</Y></Point><Point xsi:type='typens:PointN'><X>32794081.765299998</X><Y>5441460.6392999999</Y></Point><Point xsi:type='typens:PointN'><X>32794125.644000001</X><Y>5441430.8341000006</Y></Point><Point xsi:type='typens:PointN'><X>32794165.025399998</X><Y>5441409.8354000002</Y></Point><Point xsi:type='typens:PointN'><X>32794181.6032</X><Y>5441428.2355000004</Y></Point><Point xsi:type='typens:PointN'><X>32794206.094900001</X><Y>5441458.5461999997</Y></Point><Point xsi:type='typens:PointN'><X>32794185.984000001</X><Y>5441479.7317999993</Y></Point><Point xsi:type='typens:PointN'><X>32794182.932700001</X><Y>5441503.6409000009</Y></Point><Point xsi:type='typens:PointN'><X>32794193.656500001</X><Y>5441524.8278999999</Y></Point><Point xsi:type='typens:PointN'><X>32794231.297899999</X><Y>5441508.2523999996</Y></Point><Point xsi:type='typens:PointN'><X>32794239.458999999</X><Y>5441525.7037000004</Y></Point><Point xsi:type='typens:PointN'><X>32794244.9421</X><Y>5441554.6692999993</Y></Point><Point xsi:type='typens:PointN'><X>32794226.599600002</X><Y>5441562.5756999999</Y></Point><Point xsi:type='typens:PointN'><X>32794119.367600001</X><Y>5441604.8714000005</Y></Point><Point xsi:type='typens:PointN'><X>32794171.8814</X><Y>5441691.6239999998</Y></Point><Point xsi:type='typens:PointN'><X>32794248.446600001</X><Y>5441667.2337999996</Y></Point><Point xsi:type='typens:PointN'><X>32794427.070299998</X><Y>5441685.9543999992</Y></Point><Point xsi:type='typens:PointN'><X>32794548.1261</X><Y>5441655.0434000008</Y></Point><Point xsi:type='typens:PointN'><X>32794568.2544</X><Y>5441638.6411000006</Y></Point><Point xsi:type='typens:PointN'><X>32794550.533</X><Y>5441602.1360999998</Y></Point><Point xsi:type='typens:PointN'><X>32794611.829800002</X><Y>5441595.7828000002</Y></Point><Point xsi:type='typens:PointN'><X>32794624.5854</X><Y>5441611.5179999992</Y></Point><Point xsi:type='typens:PointN'><X>32794648.952799998</X><Y>5441607.8008999992</Y></Point><Point xsi:type='typens:PointN'><X>32794676.268300001</X><Y>5441633.8512999993</Y></Point><Point xsi:type='typens:PointN'><X>32794700.6818</X><Y>5441629.1338999998</Y></Point><Point xsi:type='typens:PointN'><X>32794720.659400001</X><Y>5441632.2041999996</Y></Point><Point xsi:type='typens:PointN'><X>32794694.076400001</X><Y>5441688.3705000002</Y></Point><Point xsi:type='typens:PointN'><X>32794681.592700001</X><Y>5441713.4598999992</Y></Point><Point xsi:type='typens:PointN'><X>32794642.5559</X><Y>5441718.4124999996</Y></Point><Point xsi:type='typens:PointN'><X>32794638.275699999</X><Y>5441741.0192000009</Y></Point><Point xsi:type='typens:PointN'><X>32794582.310199998</X><Y>5441762.3999000005</Y></Point><Point xsi:type='typens:PointN'><X>32794559.363600001</X><Y>5441772.0436000004</Y></Point><Point xsi:type='typens:PointN'><X>32794580.201299999</X><Y>5441811.4894999992</Y></Point><Point xsi:type='typens:PointN'><X>32794581.477400001</X><Y>5441844.1878999993</Y></Point><Point xsi:type='typens:PointN'><X>32794547.189100001</X><Y>5441863.4134999998</Y></Point><Point xsi:type='typens:PointN'><X>32794564.789499998</X><Y>5441883.4630999994</Y></Point><Point xsi:type='typens:PointN'><X>32794625.989</X><Y>5441874.2074999996</Y></Point><Point xsi:type='typens:PointN'><X>32794644.362099998</X><Y>5441885.3830999993</Y></Point><Point xsi:type='typens:PointN'><X>32794649.1558</X><Y>5441896.9945</Y></Point><Point xsi:type='typens:PointN'><X>32794666.908300001</X><Y>5441902.4982999992</Y></Point><Point xsi:type='typens:PointN'><X>32794667.924599998</X><Y>5441884.9332999997</Y></Point><Point xsi:type='typens:PointN'><X>32794678.877900001</X><Y>5441868.7668999992</Y></Point><Point xsi:type='typens:PointN'><X>32794666.000500001</X><Y>5441826.6018000003</Y></Point><Point xsi:type='typens:PointN'><X>32794684.857999999</X><Y>5441784.4222999997</Y></Point><Point xsi:type='typens:PointN'><X>32794687.1884</X><Y>5441756.7638000008</Y></Point><Point xsi:type='typens:PointN'><X>32794723.055100001</X><Y>5441776.1153999995</Y></Point><Point xsi:type='typens:PointN'><X>32794738.9606</X><Y>5441754.5741000008</Y></Point><Point xsi:type='typens:PointN'><X>32794737.793700002</X><Y>5441716.9556000009</Y></Point><Point xsi:type='typens:PointN'><X>32794701.966800001</X><Y>5441701.7962999996</Y></Point><Point xsi:type='typens:PointN'><X>32794721.878699999</X><Y>5441658.2577</Y></Point><Point xsi:type='typens:PointN'><X>32794777.269000001</X><Y>5441704.6049000006</Y></Point><Point xsi:type='typens:PointN'><X>32794802.555199999</X><Y>5441757.3052999992</Y></Point><Point xsi:type='typens:PointN'><X>32794788.6241</X><Y>5441842.9425000008</Y></Point><Point xsi:type='typens:PointN'><X>32794790.782099999</X><Y>5441875.4204999991</Y></Point><Point xsi:type='typens:PointN'><X>32794803.455800001</X><Y>5441892.6676000003</Y></Point><Point xsi:type='typens:PointN'><X>32794775.923999999</X><Y>5441902.7346000001</Y></Point><Point xsi:type='typens:PointN'><X>32794799.538699999</X><Y>5441957.5080999993</Y></Point><Point xsi:type='typens:PointN'><X>32794833.919</X><Y>5442010.7605000008</Y></Point><Point xsi:type='typens:PointN'><X>32794895.6996</X><Y>5442020.1275999993</Y></Point><Point xsi:type='typens:PointN'><X>32794941.989399999</X><Y>5441986.6664000005</Y></Point><Point xsi:type='typens:PointN'><X>32794922.974799998</X><Y>5441927.5124999993</Y></Point><Point xsi:type='typens:PointN'><X>32794960.9023</X><Y>5441916.5629999992</Y></Point><Point xsi:type='typens:PointN'><X>32794971.965</X><Y>5441871.1712999996</Y></Point><Point xsi:type='typens:PointN'><X>32794915.513700001</X><Y>5441851.7902000006</Y></Point><Point xsi:type='typens:PointN'><X>32794908.172399998</X><Y>5441804.7263999991</Y></Point><Point xsi:type='typens:PointN'><X>32794954.2808</X><Y>5441817.8276000004</Y></Point><Point xsi:type='typens:PointN'><X>32795005.642999999</X><Y>5441840.2140999995</Y></Point><Point xsi:type='typens:PointN'><X>32795013.368500002</X><Y>5441901.6191000007</Y></Point><Point xsi:type='typens:PointN'><X>32795021.5407</X><Y>5441953.4080999997</Y></Point><Point xsi:type='typens:PointN'><X>32795056.630599998</X><Y>5442041.6487000007</Y></Point><Point xsi:type='typens:PointN'><X>32795253.121100001</X><Y>5442014.6664000005</Y></Point></PointArray></Ring></RingArray><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;ETRS_1989_UTM_Zone_32N_8stellen&quot;,GEOGCS[&quot;GCS_ETRS_1989&quot;,DATUM[&quot;D_ETRS_1989&quot;,SPHEROID[&quot;GRS_1980&quot;,6378137.0,298.257222101]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Transverse_Mercator&quot;],PARAMETER[&quot;False_Easting&quot;,32500000.0],PARAMETER[&quot;False_Northing&quot;,0.0],PARAMETER[&quot;Central_Meridian&quot;,9.0],PARAMETER[&quot;Scale_Factor&quot;,0.9996],PARAMETER[&quot;Latitude_Of_Origin&quot;,0.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;ESRI&quot;,102329]]</WKT><XOrigin>26879100</XOrigin><YOrigin>-9998100</YOrigin><XYScale>450445547.3910538</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>102329</WKID></SpatialReference></PolygonN>");

			var clipEnvelope =
				(IEnvelope)
				GeometryUtils.FromXmlString(
					"<EnvelopeN xsi:type='typens:EnvelopeN' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.0'><XMin>32769125.6472</XMin><YMin>5433612.645299999</YMin><XMax>32794125.648199998</XMax><YMax>5458612.6462999992</YMax><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;ETRS_1989_UTM_Zone_32N_8stellen&quot;,GEOGCS[&quot;GCS_ETRS_1989&quot;,DATUM[&quot;D_ETRS_1989&quot;,SPHEROID[&quot;GRS_1980&quot;,6378137.0,298.257222101]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Transverse_Mercator&quot;],PARAMETER[&quot;False_Easting&quot;,32500000.0],PARAMETER[&quot;False_Northing&quot;,0.0],PARAMETER[&quot;Central_Meridian&quot;,9.0],PARAMETER[&quot;Scale_Factor&quot;,0.9996],PARAMETER[&quot;Latitude_Of_Origin&quot;,0.0],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;ESRI&quot;,102329]]</WKT><XOrigin>26879100</XOrigin><YOrigin>-9998100</YOrigin><XYScale>10000</XYScale><ZOrigin>-100000</ZOrigin><ZScale>10000</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.001</XYTolerance><ZTolerance>0.001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>102329</WKID></SpatialReference></EnvelopeN>");

			var intersection =
				(IPolygon) ((ITopologicalOperator) polygon).Intersect(
					clipEnvelope, esriGeometryDimension.esriGeometry2Dimension);

			((ITopologicalOperator) polygon).Clip(clipEnvelope);

			Assert.IsTrue(GeometryUtils.AreEqualInXY(intersection, polygon));
		}

		[Test]
		public void TestTolerance()
		{
			ISpatialReference sref = CreateSpatialReference();
			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.01, 0.01);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon, sref, 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(_testWs, "TestTolerance", fields);

			{
				IFeature row = fc.CreateFeature();

				row.Shape = CurveConstruction.StartPoly(100, 100)
				                             .LineTo(100, 200)
				                             .LineTo(200, 200)
				                             .LineTo(200, 100)
				                             .LineTo(190, 100)
				                             .LineTo(150, 100.01)
				                             .LineTo(140, 100)
				                             .LineTo(100, 100)
				                             .ClosePolygon();
				row.Store();
			}

			{
				IFeature row = fc.CreateFeature();
				row.Shape = CurveConstruction.StartPoly(100, 100)
				                             .LineTo(140, 100)
				                             .LineTo(150, 100.01)
				                             .LineTo(189.99, 100)
				                             .LineTo(200, 100)
				                             .LineTo(200, 0)
				                             .LineTo(100, 0)
				                             .LineTo(100, 100)
				                             .ClosePolygon();
				row.Store();
			}

			IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
			{
				QaNoGaps test = new QaNoGaps(roFc, -1, -1, 0, findGapsBelowTolerance: false);

				var runner = new QaContainerTestRunner(10000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}

			{
				QaNoGaps test = new QaNoGaps(roFc, -1, -1, 0, findGapsBelowTolerance: true);

				var runner = new QaContainerTestRunner(10000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void LearnToleranceBehavior()
		{
			ISpatialReference sref = CreateSpatialReference();

			IPolygon p0 = CurveConstruction.StartPoly(100, 100).LineTo(100, 200)
			                               .LineTo(200, 200)
			                               .LineTo(200, 100)
			                               .LineTo(150, 101) // Snap candidate
			                               .LineTo(100, 100)
			                               .ClosePolygon();

			IPolygon p1 = CurveConstruction.StartPoly(100, 100)
			                               .LineTo(200, 100)
			                               .LineTo(200, 0)
			                               .LineTo(100, 0)
			                               .LineTo(100, 100)
			                               .ClosePolygon();

			// TODO never use these braces just to avoid giving unique variable names or (better) creating proper methods

			{
				IPolygon c0 = GeometryFactory.Clone(p0);
				ISpatialReference sr0 = Clone(sref);
				((ISpatialReferenceTolerance) sr0).XYTolerance = 2;
				// Snap candidate snaps
				c0.SpatialReference = sr0;

				IPolygon c1 = GeometryFactory.Clone(p1);
				ISpatialReference sr1 = Clone(sref);
				c1.SpatialReference = sr1;

				var union = (IPolygon) GeometryUtils.Union(c0, c1);
				int ringCount = ((IGeometryCollection) union).GeometryCount;

				Assert.AreEqual(1, ringCount);
				Assert.AreEqual(GeometryUtils.GetXyTolerance(union), 2);
			}
			{
				IPolygon c0 = GeometryFactory.Clone(p0);
				ISpatialReference sr0 = Clone(sref);
				c0.SpatialReference = sr0;

				IPolygon c1 = GeometryFactory.Clone(p1);
				ISpatialReference sr1 = Clone(sref);
				((ISpatialReferenceTolerance) sr1).XYTolerance = 2;
				// Snap candidate snaps
				c1.SpatialReference = sr1;

				var union = (IPolygon) GeometryUtils.Union(c0, c1);
				int ringCount = ((IGeometryCollection) union).GeometryCount;

				Assert.AreEqual(2, ringCount);
				Assert.AreEqual(GeometryUtils.GetXyTolerance(union),
				                GeometryUtils.GetXyTolerance(c0));
			}

			{
				IPolygon c0 = GeometryFactory.Clone(p0);
				ISpatialReference sr0 = Clone(sref);
				((ISpatialReferenceTolerance) sr0).XYTolerance = 0.1;
				c0.SpatialReference = sr0;

				IPolygon c1 = GeometryFactory.Clone(p1);
				ISpatialReference sr1 = Clone(sref);
				((ISpatialReferenceTolerance) sr1).XYTolerance = 0.5;
				c1.SpatialReference = sr1;

				ISpatialReference srU = Clone(sref);
				((ISpatialReferenceTolerance) srU).XYTolerance = 2;

				IGeometryBag bag = ConstructBag(srU, c0, c1);

				Assert.AreEqual(2, GeometryUtils.GetXyTolerance(bag));

				Assert.AreEqual(2, GeometryUtils.GetXyTolerance(c0));
				Assert.AreEqual(2, GeometryUtils.GetXyTolerance(c1));
			}

			{
				IPolygon c0 = GeometryFactory.Clone(p0);
				ISpatialReference sr0 = Clone(sref);
				((ISpatialReferenceTolerance) sr0).XYTolerance = 2;
				// Snap candidate snaps
				c0.SpatialReference = sr0;

				IPolygon c1 = GeometryFactory.Clone(p1);
				ISpatialReference sr1 = Clone(sref);
				((ISpatialReferenceTolerance) sr1).XYTolerance = 1;
				// Snap candidate snaps
				c1.SpatialReference = sr1;

				ISpatialReference srU = Clone(sref);
				((ISpatialReferenceTolerance) srU).XYTolerance = 0.1;
				// Snap candidate does not snap
				IPolygon union = ConstructUnion(srU, c0, c1);
				int ringCount = ((IGeometryCollection) union).GeometryCount;

				Assert.AreEqual(2, ringCount);
				Assert.AreEqual(0.1, GeometryUtils.GetXyTolerance(union));

				Assert.AreEqual(0.1, GeometryUtils.GetXyTolerance(c0));
				Assert.AreEqual(0.1, GeometryUtils.GetXyTolerance(c1));
			}
		}

		[NotNull]
		private IList<IGeometry> RunTest([NotNull] IFeatureClass featureClass,
		                                 [NotNull] IEnvelope envelope,
		                                 double sliverLimit,
		                                 double maxArea,
		                                 int expectedErrorCount,
		                                 double tileSize,
		                                 int tileSubdivisions)
		{
			Console.WriteLine();
			Console.WriteLine(
				@"tile subdivisions: {0}; maxArea: {1}; sliverLimit: {2}; tileSize: {3}",
				tileSubdivisions, maxArea, sliverLimit, tileSize);
			Console.WriteLine(@"expected error count: {0}", expectedErrorCount);

			string envelopeText = GeometryUtils.Format(envelope);

			// suppress obsolete warning
#pragma warning disable 612,618
			var test = new QaNoGaps(
				ReadOnlyTableFactory.Create(featureClass), sliverLimit, maxArea,
				tileSubdivisions);
#pragma warning restore 612,618

			Console.WriteLine(@"- simple test runner, no envelope:");
			var runner = new QaTestRunner(test);
			runner.Execute();
			CheckResult(expectedErrorCount, runner);

			runner.Errors.Clear();

			Console.WriteLine(@"- simple test runner, with envelope {0}", envelopeText);
			runner.Execute(envelope);
			CheckResult(expectedErrorCount, runner);

			// run in container without envelope
			Console.WriteLine(@"- container test runner, no envelope:");
			var containerRunner1 = new QaContainerTestRunner(tileSize, test)
			                       {
				                       KeepGeometry = true
			                       };
			containerRunner1.Execute();
			CheckResult(expectedErrorCount, containerRunner1);

			// run in container with large envelope
			Console.WriteLine(@"- container test runner, with envelope {0}",
			                  envelopeText);
			var containerRunner2 = new QaContainerTestRunner(tileSize, test)
			                       {
				                       KeepGeometry = true
			                       };
			containerRunner2.Execute(envelope);
			CheckResult(expectedErrorCount, containerRunner2);

			return containerRunner2.ErrorGeometries;
		}

		private void RunTest([NotNull] IFeatureClass featureClass,
		                     [NotNull] IEnvelope envelope,
		                     double sliverLimit,
		                     double maxArea,
		                     int expectedErrorCount,
		                     double tileSize,
		                     double subTileSize)
		{
			Console.WriteLine();
			Console.WriteLine(
				@"subtile size: {0}; maxArea: {1}; sliverLimit: {2}; tileSize: {3}",
				subTileSize, maxArea, sliverLimit, tileSize);

			RunTest(featureClass, envelope,
			        sliverLimit, maxArea,
			        expectedErrorCount,
			        tileSize, subTileSize,
			        true);

			RunTest(featureClass, envelope,
			        sliverLimit, maxArea,
			        expectedErrorCount,
			        tileSize, subTileSize,
			        false);
		}

		[NotNull]
		private IList<IGeometry> RunTest([NotNull] IFeatureClass featureClass,
		                                 [NotNull] IEnvelope envelope,
		                                 double sliverLimit,
		                                 double maxArea,
		                                 int expectedErrorCount,
		                                 double tileSize,
		                                 double subTileSize,
		                                 bool findGapsBelowTolerance)
		{
			Console.WriteLine(@"find gaps below tolerance: {0}",
			                  findGapsBelowTolerance);
			Console.WriteLine(@"expected error count: {0}", expectedErrorCount);

			string envelopeText = GeometryUtils.Format(envelope);

			var test = new QaNoGaps(
				ReadOnlyTableFactory.Create(featureClass), sliverLimit, maxArea, subTileSize,
				findGapsBelowTolerance);

			Console.WriteLine(@"- simple test runner, no envelope");
			var runner = new QaTestRunner(test);
			runner.Execute();
			CheckResult(expectedErrorCount, runner);

			Console.WriteLine(@"- simple test runner, with envelope {0}", envelopeText);
			runner.Errors.Clear();
			runner.Execute(envelope);
			CheckResult(expectedErrorCount, runner);

			// run in container without envelope
			Console.WriteLine(@"- container test runner, no envelope");
			var containerRunner1 = new QaContainerTestRunner(tileSize, test)
			                       {
				                       KeepGeometry = true
			                       };
			containerRunner1.Execute();
			CheckResult(expectedErrorCount, containerRunner1);

			// run in container with large envelope
			Console.WriteLine(@"- container test runner, with envelope {0}",
			                  envelopeText);
			var containerRunner2 = new QaContainerTestRunner(tileSize, test)
			                       {
				                       KeepGeometry = true
			                       };
			containerRunner2.Execute(envelope);
			CheckResult(expectedErrorCount, containerRunner2);

			return containerRunner2.ErrorGeometries;
		}

		private void CheckResult(int expectedErrorCount, QaTestRunnerBase testRunner)
		{
			if (expectedErrorCount == testRunner.Errors.Count)
			{
				return;
			}

			Console.WriteLine(@"** Unexpected error count: {0} (expected: {1})",
			                  testRunner.Errors.Count, expectedErrorCount);
			_failed = true;

			// ReSharper disable ConditionIsAlwaysTrueOrFalse
			if (_writeErrors)
				// ReSharper restore ConditionIsAlwaysTrueOrFalse
#pragma warning disable 162
				// ReSharper disable HeuristicUnreachableCode
			{
				WriteErrors(testRunner.ErrorGeometries);
			}
			// ReSharper restore HeuristicUnreachableCode
#pragma warning restore 162
		}

		private static void WriteErrors(
			[NotNull] IEnumerable<IGeometry> errorGeometries)
		{
			int i = 0;
			foreach (IGeometry errorGeometry in errorGeometries)
			{
				i++;
				Console.WriteLine(@"error geometry {0}", i);
				Console.WriteLine(GeometryUtils.ToString(errorGeometry));
			}
		}

		[NotNull]
		private static IGeometryBag ConstructBag(
			[NotNull] ISpatialReference unionSr, params IGeometry[] geometries)
		{
			IGeometryBag bag = new GeometryBagClass
			                   {
				                   SpatialReference = unionSr
			                   };

			foreach (IGeometry geometry in geometries)
			{
				object missing = Type.Missing;
				((IGeometryCollection) bag).AddGeometry(geometry, ref missing,
				                                        ref missing);
			}

			return bag;
		}

		[NotNull]
		private static IPolygon ConstructUnion(
			ISpatialReference unionSr, params IPolygon[] geometries)
		{
			IPolygon leftHandGeometry = new PolygonClass();

			IGeometryBag bag = new GeometryBagClass
			                   {
				                   SpatialReference = unionSr
			                   };

			foreach (IPolygon geometry in geometries)
			{
				object missing = Type.Missing;
				((IGeometryCollection) bag).AddGeometry(geometry, ref missing,
				                                        ref missing);
			}

			var topoOp = (ITopologicalOperator) leftHandGeometry;
			topoOp.ConstructUnion((IEnumGeometry) bag);

			return leftHandGeometry;
		}

		private static ISpatialReference CreateSpatialReference()
		{
			return SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);
		}

		[NotNull]
		private static T Clone<T>([NotNull] T prototype)
		{
			return (T) ((IClone) prototype).Clone();
		}
	}
}
