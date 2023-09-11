using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrGetNodesTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void SimpleGetNodesTest()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fcLv95 = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));


			{
				IFeature f = fcLv95.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(100, 100).Curve;
				f.Store();
			}
			IReadOnlyFeatureClass ro = ReadOnlyTableFactory.Create(fcLv95);
			TrGetNodes tr = new TrGetNodes(ro);
			QaConstraint test =
				new QaConstraint(tr.GetTransformed(), "ObjectId = 2");

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}


		[Test]
		public void CanAggregateAttributes()
		{
			int idLv95 = (int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fcNet = DatasetUtils.CreateSimpleFeatureClass(ws, "netFc",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));


			{
				IFeature f = fcNet.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(100, 100).Curve;
				f.Store();
			}
			{
				IFeature f = fcNet.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(30, 10).LineTo(100, 100).Curve;
				f.Store();
			}
			{
				IFeature f = fcNet.CreateFeature();
				f.Shape = CurveConstruction.StartLine(90, 10).LineTo(100, 100).Curve;
				f.Store();
			}
			IReadOnlyFeatureClass roNet = ReadOnlyTableFactory.Create(fcNet);
			TrGetNodes tr = new TrGetNodes(roNet);
			tr.Attributes = new List<string>
			                {
								"COUNT(OBJECTID) as joined_count"
			                };
			QaConstraint test =
				new QaConstraint(tr.GetTransformed(), "joined_count > 1");

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

	}
}
