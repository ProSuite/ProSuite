using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO;
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


			IPolyline line = (IPolyline)CurveConstruction
										 .StartLine(2600000, 1200000).LineTo(2601000, 1201000)
										 .Curve;

			line.SpatialReference = (ISpatialReference)((IClone)srLv95).Clone();

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

				IPolyline clone = SysUtils.Clone(line);
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
