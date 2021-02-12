using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	[CLSCompliant(false)]
	public class QaFlowLogicTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;
		private const double _xyTolerance = 0.001;

		private IPoint _lastErrorPoint;
		private int _errorCount;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("TestFlowLogic");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestFlowDirExpression()
		{
			TestFlowDirExpression(_testWs);
		}

		private void TestFlowDirExpression(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("FlowDir",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass featureClass =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestFlowDirExpression", fields);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = featureClass.CreateFeature();
				row.set_Value(1, 5);
				row.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(100, 200),
						GeometryFactory.CreatePoint(110, 190));
				row.Store();
			}
			{
				IFeature row = featureClass.CreateFeature();
				row.set_Value(1, 10);
				row.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(120, 190),
						GeometryFactory.CreatePoint(110, 190));
				row.Store();
			}

			IPoint lastPoint;
			{
				IFeature row = featureClass.CreateFeature();
				row.set_Value(1, null);
				row.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(120, 190),
						GeometryFactory.CreatePoint(130, 190));
				row.Store();
				lastPoint = ((IPolyline) row.Shape).ToPoint;
			}

			var test = new QaFlowLogic(new[] {featureClass}, new[] {"FlowDir > 6"});
			test.QaError += Test_QaError;
			_errorCount = 0;
			test.Execute();
			Assert.AreEqual(1, _errorCount);
			Assert.AreEqual(lastPoint.X, _lastErrorPoint.X);
			Assert.AreEqual(lastPoint.Y, _lastErrorPoint.Y);
			test.QaError -= Test_QaError;

			var container = new TestContainer();
			container.AddTest(test);
			container.QaError += Test_QaError;
			_errorCount = 0;
			container.Execute();
			Assert.AreEqual(1, _errorCount);
			Assert.AreEqual(lastPoint.X, _lastErrorPoint.X);
			Assert.AreEqual(lastPoint.Y, _lastErrorPoint.Y);
		}

		[Test]
		public void TestFlowDirMultiExpression()
		{
			TestFlowDirMultiExpression(_testWs);
		}

		private void TestFlowDirMultiExpression(IFeatureWorkspace ws)
		{
			IFeatureClass fc1;
			{
				var fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateField("FlowDir",
				                                       esriFieldType.esriFieldTypeInteger));
				fields.AddField(FieldUtils.CreateShapeField(
					                "Shape", esriGeometryType.esriGeometryPolyline,
					                SpatialReferenceUtils.CreateSpatialReference
					                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
					                 true), 1000));

				fc1 = DatasetUtils.CreateSimpleFeatureClass(ws,
				                                            "TestFlowDirMultiExpression1",
				                                            fields);
			}
			IFeatureClass fc2;
			{
				var fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateField("FlowDir",
				                                       esriFieldType.esriFieldTypeInteger));
				fields.AddField(FieldUtils.CreateShapeField(
					                "Shape", esriGeometryType.esriGeometryPolyline,
					                SpatialReferenceUtils.CreateSpatialReference
					                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
					                 true), 1000));

				fc2 = DatasetUtils.CreateSimpleFeatureClass(ws,
				                                            "TestFlowDirMultiExpression2",
				                                            fields);
			}

			// make sure the tables are known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			{
				IFeature row = fc1.CreateFeature();
				row.set_Value(1, 5);
				row.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(100, 200),
						GeometryFactory.CreatePoint(110, 190));
				row.Store();
			}
			{
				IFeature row = fc2.CreateFeature();
				row.set_Value(1, 5);
				row.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(120, 190),
						GeometryFactory.CreatePoint(110, 190));
				row.Store();
			}
			{
				IFeature row = fc1.CreateFeature();
				row.set_Value(1, null);
				row.Shape =
					GeometryFactory.CreateLine(
						GeometryFactory.CreatePoint(120, 190),
						GeometryFactory.CreatePoint(130, 190));
				row.Store();
			}

			{
				var test = new QaFlowLogic(
					new[] {fc1, fc2},
					new[] {"FlowDir > 6", "FlowDir < 6"}
					// no feature fc1 will be inverted, feature of fc2 will be inverted
				);
				test.QaError += Test_QaError;
				_errorCount = 0;
				test.Execute();
				Assert.AreEqual(1, _errorCount);
				test.QaError -= Test_QaError;

				var container = new TestContainer();
				container.AddTest(test);
				_errorCount = 0;
				container.QaError += Test_QaError;
				container.Execute();
				Assert.AreEqual(1, _errorCount);
			}

			{
				var test = new QaFlowLogic(
					new[] {fc1, fc2},
					new[] {"FlowDir > 6"}
					// no feature will be inverted
				);
				test.QaError += Test_QaError;
				_errorCount = 0;
				test.Execute();
				Assert.AreEqual(3, _errorCount);
				test.QaError -= Test_QaError;

				var container = new TestContainer();
				container.AddTest(test);
				container.QaError += Test_QaError;
				_errorCount = 0;
				container.Execute();
				Assert.AreEqual(3, _errorCount);
			}
		}

		[Test]
		public void MultipartTest()
		{
			IFeatureWorkspace workspace =
				TestWorkspaceUtils.CreateInMemoryWorkspace("MultipartTest");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline, CreateLV95(),
				                1000));

			IFeatureClass linesFc = DatasetUtils.CreateSimpleFeatureClass(workspace, "Flow",
			                                                              fields);

			AddFeature(
				linesFc,
				CurveConstruction.StartLine(0, 0).LineTo(4, 0).MoveTo(6, 0).LineTo(6, 10).Curve);
			AddFeature(linesFc, CurveConstruction.StartLine(4, 0).LineTo(6, 0).Curve);

			AddFeature(linesFc, CurveConstruction.StartLine(0, 20).LineTo(4, 20).Curve);

			AddFeature(
				linesFc,
				CurveConstruction.StartLine(0, 30).LineTo(4, 30).MoveTo(0, 32).LineTo(4, 30)
				                 .MoveTo(4, 30).LineTo(8, 30).Curve);

			// expect counter-clockwise: 0 errors
			var runner = new QaContainerTestRunner(
				1000, new QaFlowLogic(linesFc));
			Assert.AreEqual(3, runner.Execute());
		}

		private void Test_QaError(object sender, QaErrorEventArgs e)
		{
			_lastErrorPoint = (IPoint) e.QaError.Geometry;
			_errorCount++;
		}

		[NotNull]
		private static IFeature AddFeature(
			[NotNull] IFeatureClass featureClass,
			[NotNull] IGeometry geometry)
		{
			IFeature feature = featureClass.CreateFeature();
			feature.Shape = geometry;

			feature.Store();

			return feature;
		}

		[NotNull]
		private static ISpatialReference CreateLV95()
		{
			ISpatialReference result = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetXYDomain(result, -10000, -10000, 10000, 10000,
			                                  0.0001, _xyTolerance);
			return result;
		}
	}
}
