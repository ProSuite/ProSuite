using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaIntersectsOtherTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanIgnoreArea()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ignoreArea");

			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Objektart",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolyline,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass lineFc = DatasetUtils.CreateSimpleFeatureClass(ws, "lineFc", fields);

			fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Objektart",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass areaFc = DatasetUtils.CreateSimpleFeatureClass(ws, "areaFc", fields);

			fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Objektart",
			                                       esriFieldType.esriFieldTypeInteger));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPolygon,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000));

			IFeatureClass ignoreFc = DatasetUtils.CreateSimpleFeatureClass(ws, "ignoreFc", fields);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			Create(lineFc, 10, CurveConstruction.StartLine(0, 0)
			                                    .LineTo(10, 10)
			                                    .Curve);

			Create(areaFc, 10, CurveConstruction.StartPoly(1, 1)
			                                    .LineTo(2, 1)
			                                    .LineTo(2, 2)
			                                    .LineTo(1, 2)
			                                    .ClosePolygon());

			Create(areaFc, 10, CurveConstruction.StartPoly(6, 6)
			                                    .LineTo(7, 6)
			                                    .LineTo(7, 7)
			                                    .LineTo(6, 7)
			                                    .ClosePolygon());

			Create(ignoreFc, 0, CurveConstruction.StartPoly(1, 1)
			                                     .LineTo(2, 1)
			                                     .LineTo(2, 2)
			                                     .LineTo(1, 2)
			                                     .ClosePolygon());

			Create(ignoreFc, 10, CurveConstruction.StartPoly(6, 6)
			                                      .LineTo(7, 6)
			                                      .LineTo(7, 7)
			                                      .LineTo(6, 7)
			                                      .ClosePolygon());

			var test = new QaIntersectsOther(lineFc, areaFc);
			test.IgnoreArea = ignoreFc;
			test.SetConstraint(2, "objektart in (10)");
			{
				// Container test
				var runner = new QaContainerTestRunner(1000, test);
				int errorCount = runner.Execute();

				Assert.AreEqual(
					1, errorCount); // only line errors, point error is removed by test container!
			}
			{
				// simple test
				var runner = new QaTestRunner(test);
				int errorCount = runner.Execute();

				Assert.AreEqual(2, errorCount); // line AND point errors!
			}
		}

		private IFeature Create(IFeatureClass fc, int objekart, IGeometry geom)
		{
			IFeature f1 = fc.CreateFeature();
			f1.set_Value(1, objekart);
			f1.Shape = geom;
			f1.Store();

			return f1;
		}
	}
}