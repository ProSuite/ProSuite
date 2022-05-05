using System;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaPseudoNodesTest
	{
		private const string _nrFieldName = "Nummer";

		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanTestPseudoNodes()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("CanTestPseudoNodes");
			IFeatureClass fc = CreateLineClass(ws);

			IFeature row1 = fc.CreateFeature();
			row1.Shape = CurveConstruction.StartLine(0, 0).LineTo(1, 1).Curve;
			row1.set_Value(fc.Fields.FindField(_nrFieldName), 1);
			row1.Store();

			IFeature row2 = fc.CreateFeature();
			row2.Shape = CurveConstruction.StartLine(1, 1).LineTo(2, 2).Curve;
			row2.set_Value(fc.Fields.FindField(_nrFieldName), 1);
			row2.Store();

			var test = new QaPseudoNodes(fc, new string[] { });

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestLoopEndPoints()
		{
			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateInMemoryWorkspace("CanTestLoopEndPoints");
			IFeatureClass fc = CreateLineClass(ws);

			IFeature row1 = fc.CreateFeature();
			row1.Shape =
				CurveConstruction.StartLine(0, 0).LineTo(1, 1).LineTo(1, 0).LineTo(0, 0).Curve;
			row1.set_Value(fc.Fields.FindField(_nrFieldName), 1);
			row1.Store();

			var test = new QaPseudoNodes(fc, new string[] { });

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);

			test.IgnoreLoopEndpoints = true;
			runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[NotNull]
		private static IFeatureClass CreateLineClass([NotNull] IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateIntegerField(_nrFieldName));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			Thread.Sleep(10);
			IFeatureClass featureClass =
				DatasetUtils.CreateSimpleFeatureClass(
					ws, "line_" + Environment.TickCount, fields, null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			return featureClass;
		}
	}
}
