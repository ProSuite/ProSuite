
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaInteriorIntersectsSelfTest
	{
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
		public void CanRun()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("intersect");

			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				(int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000,
			                                  0.0001, 0.001);

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline, sref, 1000));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestNoGaps", fields);


			{
				IFeature row = fc.CreateFeature();
				row.Shape =
					CurveConstruction.StartLine(100, 100)
					                 .LineTo(200, 200)
					                 .Curve;
				row.Store();
			}

			{
				IFeature row = fc.CreateFeature();
				row.Shape =
					CurveConstruction.StartLine(200, 100)
					                 .LineTo(100, 200)
					                 .Curve;
				row.Store();
			}

			QaInteriorIntersectsSelf test =
				new QaInteriorIntersectsSelf(ReadOnlyTableFactory.Create(fc));

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();
		}
	}

}
