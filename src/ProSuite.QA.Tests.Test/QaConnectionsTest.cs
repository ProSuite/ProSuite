using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.GeoDb;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaConnectionsTest
	{
		private IFeatureWorkspace _testWs;
		private int _errorCount;
		private IGeometry _lastErrorGeom;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("QaConnectionsTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void TestConnections()
		{
			TestConnections(_testWs);
		}

		private void TestConnections(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateIntegerField("LineTyp", "LineTyp"));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestConnections", fields,
				                                      null);
			IList<ITableSchemaDef> tbls = new[] { ReadOnlyTableFactory.Create(fc) };

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				row.set_Value(1, 1);
				row.Shape =
					CurveConstruction.StartLine(100, 100).LineTo(200, 200).Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				row.set_Value(1, 2);
				row.Shape =
					CurveConstruction.StartLine(100, 100.1).LineTo(100, 200).Curve;
				row.Store();
			}

			IList<QaConnectionRule> rules = new[]
			                                {
				                                new QaConnectionRule(tbls, new[] { "LineTyp = 1" }),
				                                new QaConnectionRule(tbls, new[] { "LineTyp = 2" })
			                                };
			var test = new QaConnections(new[] { ReadOnlyTableFactory.Create(fc) }, rules, 0);
			test.QaError += Test_QaError;
			_errorCount = 0;
			test.Execute();
			Assert.AreEqual(0, _errorCount);
			test.QaError -= Test_QaError;

			var container = new TestContainer();
			container.AddTest(test);
			container.QaError += Test_QaError;
			_errorCount = 0;
			container.Execute();
			Assert.AreEqual(0, _errorCount);
		}

		[Test]
		public void TestMultipart()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("Testmultipart");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateIntegerField("LineTyp", "LineTyp"));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestConnections", fields,
				                                      null);
			IList<ITableSchemaDef> tbls = new[] { ReadOnlyTableFactory.Create(fc) };

			{
				IFeature row = fc.CreateFeature();
				row.set_Value(1, 1);
				row.Shape =
					CurveConstruction.StartLine(1, 1).LineTo(2, 2).MoveTo(3, 2).LineTo(4, 2).Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				row.set_Value(1, 2);
				row.Shape =
					CurveConstruction.StartLine(1, 5).LineTo(2, 2).Curve;
				row.Store();
			}

			IList<QaConnectionRule> rules = new List<QaConnectionRule>
			                                {
				                                new QaConnectionRule(tbls, new[] { "LineTyp = 1" }),
				                                new QaConnectionRule(tbls, new[] { "LineTyp = 2" })
			                                };
			var test = new QaConnections(new[] { ReadOnlyTableFactory.Create(fc) }, rules, 0);

			test.UseMultiParts = false;
			var runner = new QaContainerTestRunner(1000, test);
			Assert.AreEqual(0, runner.Execute());

			test.UseMultiParts = true;
			runner = new QaContainerTestRunner(1000, test);
			Assert.AreEqual(1, runner.Execute());
		}

		[Test]
		public void TestWithTolerance()
		{
			TestWithTolerance(_testWs);
		}

		private void TestWithTolerance(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateIntegerField("LineTyp", "LineTyp"));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestWithTolerance", fields,
				                                      null);
			IList<ITableSchemaDef> tbls = new[] { ReadOnlyTableFactory.Create(fc) };

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc.CreateFeature();
				row.set_Value(1, 1);
				row.Shape =
					CurveConstruction.StartLine(100, 99.93).LineTo(200, 200).Curve;
				row.Store();
			}
			{
				IFeature row = fc.CreateFeature();
				row.set_Value(1, 2);
				row.Shape =
					CurveConstruction.StartLine(100, 99.95).LineTo(100, 200).Curve;
				row.Store();
			}

			IList<QaConnectionRule> rules = new[]
			                                {
				                                new QaConnectionRule(tbls, new[] { "LineTyp = 1" }),
				                                new QaConnectionRule(tbls, new[] { "LineTyp = 2" })
			                                };
			var test = new QaConnections(new[] { ReadOnlyTableFactory.Create(fc) }, rules, 0.2);
			test.QaError += Test_QaError;
			_errorCount = 0;
			test.Execute();
			Assert.AreEqual(1, _errorCount);
			test.QaError -= Test_QaError;

			var container = new TestContainer();
			container.AddTest(test);
			container.TileSize = 100;
			IEnvelope box = new EnvelopeClass();
			container.QaError += Test_QaError;

			_errorCount = 0;
			container.Execute();
			Assert.AreEqual(1, _errorCount);

			_errorCount = 0;
			box.PutCoords(0, 0, 300, 300);
			container.Execute(box);
			Assert.AreEqual(1, _errorCount);

			_errorCount = 0;
			box.PutCoords(1, 0, 300, 300);
			container.Execute(box);
			Assert.AreEqual(1, _errorCount);
		}

		private void Test_QaError(object sender, QaErrorEventArgs e)
		{
			_lastErrorGeom = e.QaError.Geometry;
			_errorCount++;
		}
	}
}
