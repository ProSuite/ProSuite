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
	public class TrProjectTest
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
		public void CanUseTrProject()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			ISpatialReference srWgs84 = SpatialReferenceUtils.CreateSpatialReference(
				(int)esriSRGeoCSType.esriSRGeoCS_WGS1984, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fcLv95 = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));

			IFeatureClass fcWgs84 = DatasetUtils.CreateSimpleFeatureClass(ws, "wgs84",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srWgs84, 1000)));

			{
				IFeature f = fcLv95.CreateFeature();
				f.Shape = CurveConstruction.StartLine(2600000, 1200000)
				                           .LineTo(2601000, 1201000).Curve;
				f.Store();
			}
			{
				IPolycurve l = CurveConstruction.StartLine(2600000, 1201000)
				                                .LineTo(2601000, 1200000).Curve;
				l.SpatialReference = srLv95;
				l.Project(srWgs84);

				IFeature f = fcWgs84.CreateFeature();
				f.Shape = l;
				f.Store();
			}

			IReadOnlyFeatureClass roWgs84 = ReadOnlyTableFactory.Create(fcWgs84);
			TrProject tr = new TrProject(roWgs84, idLv95);
			QaInteriorIntersectsOther test =
				new QaInteriorIntersectsOther(tr.GetTransformed(),
				                              ReadOnlyTableFactory.Create(fcLv95));

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}
	}
}
