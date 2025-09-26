using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.Test.Construction;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	[TestFixture]
	public class SpatialReferenceTests
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
		public void TestProjectExForEnvelope()
		{
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(2056);
			ISpatialReference srWgs84 = SpatialReferenceUtils.CreateSpatialReference(4326);

			IEnvelope envelopeLv95 =
				GeometryFactory.CreateEnvelope(2600000, 1200000, 2601000, 1201000, srLv95);

			IEnvelope result = SpatialReferenceUtils.ProjectEx(envelopeLv95, srWgs84);

			Assert.AreEqual(esriGeometryType.esriGeometryEnvelope, result.GeometryType);

			Assert.AreEqual(7.438632495, result.XMin, 0.000001);
			Assert.AreEqual(46.951082877, result.YMin, 0.000001);

			Assert.AreEqual(7.451770658, result.XMax, 0.000001);
			Assert.AreEqual(46.960077397, result.YMax, 0.000001);
		}

		[Test]
		public void HandleDifferentSpatialReferences()
		{
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(2056);
			ISpatialReference srWgs84 = SpatialReferenceUtils.CreateSpatialReference(4326);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateTestFgdbWorkspace("VerificationTest");
			IFeatureClass fcWgs84 = DatasetUtils.CreateSimpleFeatureClass(
				ws, "Wgs84",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolyline, srWgs84)
				));

			IFeatureClass fcWgs84_prj = DatasetUtils.CreateSimpleFeatureClass(
				ws, "Wgs84_prj",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolyline, srWgs84)
				));

			IFeatureClass fcLv95 = DatasetUtils.CreateSimpleFeatureClass(
				ws, "Lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolyline, srLv95)
				));

			IPolyline line = (IPolyline) CurveConstruction
			                             .StartLine(2600000, 1200000).LineTo(2601000, 1201000)
			                             .Curve;

			line.SpatialReference = (ISpatialReference) ((IClone) srLv95).Clone();

			{
				IFeature f = fcLv95.CreateFeature();
				f.Shape = line;
				f.Store();
			}

			{
				IFeature f = fcLv95.CreateFeature();

				IPolyline clone = SpatialReferenceUtils.ProjectEx(line, srLv95);

				f.Shape = clone;
				f.Store();
			}

			{
				IFeature f = fcLv95.CreateFeature();

				IPolyline clone = GeometryFactory.Clone(line);
				clone.SpatialReference = null;
				clone = SpatialReferenceUtils.ProjectEx(clone, srLv95);

				f.Shape = clone;
				f.Store();
			}

			{
				IFeature f = fcWgs84_prj.CreateFeature();

				IPolyline clone = SpatialReferenceUtils.ProjectEx(line, srWgs84);

				f.Shape = clone;
				f.Store();
			}

			{
				IFeature f = fcWgs84.CreateFeature();
				f.Shape = line;
				f.Store();

				SpatialReferenceUtils.AreEqual(line.SpatialReference, srLv95);
			}
		}
	}
}
