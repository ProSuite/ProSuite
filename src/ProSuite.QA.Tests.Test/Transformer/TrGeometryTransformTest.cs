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
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrGeomToPoints");

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

		[Test]
		public void MultilineToLine()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrMultiline");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(70, 70)
				                           .MoveTo(10, 0).LineTo(20, 5).Curve;
				f.Store();
			}

			TrMultilineToLine tr = new TrMultilineToLine(lineFc);
			QaMinLength test = new QaMinLength(tr.GetTransformed(), 100);
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
			test.SetConstraint(0, $"{TrMultilineToLine.AttrPartIndex} = 0");
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void MultipolyToPoly()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrMultipoly");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolygon);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape =
					CurveConstruction
						.StartPoly(0, 0).LineTo(30, 0).LineTo(30, 30).LineTo(0, 30).LineTo(0, 0)
						.MoveTo(10, 10).LineTo(10, 20).LineTo(20, 20).LineTo(20, 10).LineTo(10, 10)
						.MoveTo(22, 22).LineTo(22, 24).LineTo(24, 24).LineTo(24, 22).LineTo(22, 22)
						.MoveTo(13, 13).LineTo(17, 13).LineTo(17, 17).LineTo(13, 17).LineTo(13, 13)
						.MoveTo(40, 40).LineTo(50, 40).LineTo(50, 50).LineTo(40, 50).LineTo(40, 40)
						.ClosePolygon();
				f.Store();
			}

			TrMultipolygonToPolygon tr = new TrMultipolygonToPolygon(lineFc);
			QaMinArea test = new QaMinArea(tr.GetTransformed(), 850);
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(3, runner.Errors.Count);
			}
			{
				tr.TransformedParts = TrMultipolygonToPolygon.PolygonPart.OuterRings;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
			{
				tr.TransformedParts = TrMultipolygonToPolygon.PolygonPart.InnerRings;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
			{
				tr.TransformedParts = TrMultipolygonToPolygon.PolygonPart.AllRings;
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(4, runner.Errors.Count);
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
