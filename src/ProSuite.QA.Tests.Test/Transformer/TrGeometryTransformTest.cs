using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrGeometryTransformTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void GeometryToPoints()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrDissolve");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline);
			IFeatureClass pntFc =
				CreateFeatureClass(ws, "pntFc", esriGeometryType.esriGeometryPoint);
			IFeatureClass refFc =
				CreateFeatureClass(ws, "refFc", esriGeometryType.esriGeometryPoint);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).LineTo(70, 70).Curve;
				f.Store();
			}
			{
				IFeature f = pntFc.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(69, 69.5);
				f.Store();
			}
			{
				IFeature f = refFc.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(69, 70);
				f.Store();
			}

			{
				TrGeometryToPoints tr = new TrGeometryToPoints(lineFc, GeometryComponent.Vertices);
				QaPointNotNear test = new QaPointNotNear(tr.GetTransformed(), refFc, 2);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
			{
				TrGeometryToPoints tr =
					new TrGeometryToPoints(pntFc, GeometryComponent.EntireGeometry);
				QaPointNotNear test = new QaPointNotNear(tr.GetTransformed(), refFc, 2);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		private IFeatureClass CreateFeatureClass(IFeatureWorkspace ws, string name,
		                                         esriGeometryType geometryType)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", geometryType,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));
			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name, fields);
			return fc;
		}
	}
}
