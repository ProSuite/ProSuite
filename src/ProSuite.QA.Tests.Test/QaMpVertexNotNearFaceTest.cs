using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMpVertexNotNearFaceTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_testWs =
				TestWorkspaceUtils.CreateInMemoryWorkspace("QaMpVertexNotNearFaceTest");
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanTestVertexNotNearFace()
		{
			IFeatureClass fc = CreateFeatureClass(_testWs, "CanTestVertexNotNearFace");

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 0)
			            .Add(0, 10, 10)
			            .Add(10, 10, 10)
			            .Add(10, 0, 0);
			IFeature row1 = fc.CreateFeature();
			row1.Shape = construction.MultiPatch;
			row1.Store();

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(2, 0, 5)
			            .Add(2, 7, 5)
			            .Add(8, 7, 5)
			            .Add(8, 0, 5);
			IFeature row2 = fc.CreateFeature();
			row2.Shape = construction.MultiPatch;
			row2.Store();

			((IWorkspaceEdit) _testWs).StopEditing(true);

			var test = new QaMpVertexNotNearFace(fc, new[] {fc}, 1, 0.2)
			           {
				           VerifyWithinFeature = false,
				           MinimumSlopeDegrees = 30
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void CanTestVertexNotNearFaceAboveHorizontal()
		{
			IFeatureClass fc =
				CreateFeatureClass(_testWs, "CanTestVertexNotNearFaceHorizontal");

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 10)
			            .Add(0, 10, 10)
			            .Add(10, 10, 10)
			            .Add(10, 0, 10);
			IFeature row1 = fc.CreateFeature();
			row1.Shape = construction.MultiPatch;
			row1.Store();

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 5, 10.1)
			            .Add(15, 5, 15.1)
			            .Add(15, 15, 20.1)
			            .Add(5, 15, 15.1);
			IFeature row2 = fc.CreateFeature();
			row2.Shape = construction.MultiPatch;
			row2.Store();

			((IWorkspaceEdit) _testWs).StopEditing(true);

			var test = new QaMpVertexNotNearFace(fc, new[] {fc}, 0.2, 0)
			           {
				           VerifyWithinFeature = false,
				           MinimumSlopeDegrees = 0
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestVertexNotNearFaceAboveHorizontalCcw()
		{
			IFeatureClass fc =
				CreateFeatureClass(_testWs, "CanTestVertexNotNearFaceHorizontalCcw");

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 10)
			            .Add(10, 0, 10)
			            .Add(10, 10, 10)
			            .Add(1, 10, 10);
			IFeature row1 = fc.CreateFeature();
			row1.Shape = construction.MultiPatch;
			row1.Store();

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 5, 10.1)
			            .Add(15, 5, 15.1)
			            .Add(15, 15, 20.1)
			            .Add(5, 15, 15.1);
			IFeature row2 = fc.CreateFeature();
			row2.Shape = construction.MultiPatch;
			row2.Store();

			((IWorkspaceEdit) _testWs).StopEditing(true);

			var test = new QaMpVertexNotNearFace(fc, new[] {fc}, 0.2, 0)
			           {
				           VerifyWithinFeature = false,
				           MinimumSlopeDegrees = 0
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestVertexNotNearFaceBelowHorizontal()
		{
			IFeatureClass fc =
				CreateFeatureClass(_testWs, "CanTestVertexNotNearFaceBelowHorizontal");

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 10)
			            .Add(0, 10, 10)
			            .Add(10, 10, 10)
			            .Add(10, 0, 10);
			IFeature row1 = fc.CreateFeature();
			row1.Shape = construction.MultiPatch;
			row1.Store();

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 5, 9.9)
			            .Add(15, 5, 14.9)
			            .Add(15, 15, 19.9)
			            .Add(5, 15, 14.9);
			IFeature row2 = fc.CreateFeature();
			row2.Shape = construction.MultiPatch;
			row2.Store();

			((IWorkspaceEdit) _testWs).StopEditing(true);

			var test = new QaMpVertexNotNearFace(fc, new[] {fc}, 0, 0.2)
			           {
				           VerifyWithinFeature = true,
				           MinimumSlopeDegrees = 0
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanTestVertexNotNearFaceBelowHorizontalCcw()
		{
			IFeatureClass fc =
				CreateFeatureClass(_testWs, "CanTestVertexNotNearFaceBelowHorizontalCcw");

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 10)
			            .Add(10, 0, 10)
			            .Add(10, 10, 10)
			            .Add(1, 10, 10);
			IFeature row1 = fc.CreateFeature();
			row1.Shape = construction.MultiPatch;
			row1.Store();

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 5, 9.9)
			            .Add(15, 5, 14.9)
			            .Add(15, 15, 19.9)
			            .Add(5, 15, 14.9);
			IFeature row2 = fc.CreateFeature();
			row2.Shape = construction.MultiPatch;
			row2.Store();

			((IWorkspaceEdit) _testWs).StopEditing(true);

			var test = new QaMpVertexNotNearFace(fc, new[] {fc}, 0, 0.2)
			           {
				           VerifyWithinFeature = true,
				           MinimumSlopeDegrees = 0
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[NotNull]
		private static IFeatureClass CreateFeatureClass(
			IFeatureWorkspace ws, string fcName)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryMultiPatch,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, true, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, fcName, fields, null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);
			return fc;
		}

		[Test]
		[Ignore("requires connection to TOPGIST")]
		public void TestTopgisTMultiPatches()
		{
			IWorkspace workspace = TestDataUtils.OpenTopgisTlm();

			IFeatureClass gebaeude =
				((IFeatureWorkspace) workspace).OpenFeatureClass(
					"TOPGIS_TLM.TLM_GEBAEUDE");

			var test = new QaMpVertexNotNearFace(
				gebaeude, new[] {gebaeude}, 1, 0.2);
			test.VerifyWithinFeature = true;
			test.PointCoincidence = 0.1;
			test.EdgeCoincidence = 0.1;
			test.IgnoreNonCoplanarFaces = true;
			test.MinimumSlopeDegrees = 15;

			var runner = new QaContainerTestRunner(10000, test);
			runner.LogErrors = false;

			IFeatureWorkspace ws =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("QaMpVertexNotNearFaceTest");
			var logger = new SimpleErrorWorkspace(ws);
			runner.TestContainer.QaError += logger.TestContainer_QaError;

			runner.Execute();

			Console.WriteLine(runner.Errors.Count);
		}
	}
}
