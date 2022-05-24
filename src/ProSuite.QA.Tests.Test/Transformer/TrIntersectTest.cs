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
using System.Collections.Generic;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.IssueFilters;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrIntersectTest
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
		public void CanIntersect()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrIntersect");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline);
			IFeatureClass polyFc =
				CreateFeatureClass(ws, "polyFc", esriGeometryType.esriGeometryPolygon);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
										   .LineTo(70, 50).ClosePolygon();
				f.Store();
			}

			TrIntersect tr = new TrIntersect(ReadOnlyTableFactory.Create(lineFc),
											 ReadOnlyTableFactory.Create(polyFc));
			{
				QaMaxLength test = new QaMaxLength(tr.GetTransformed(), 25);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				QaMaxLength test = new QaMaxLength(tr.GetTransformed(), 10);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}

		}
		[Test]
		public void CanAccessAttributes()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrIntersect");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr") });
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 13;
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 8;
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
										   .LineTo(70, 50).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 6;
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}

			TrIntersect tr = new TrIntersect(ReadOnlyTableFactory.Create(lineFc),
											 ReadOnlyTableFactory.Create(polyFc));
			{
				QaMaxLength test = new QaMaxLength(tr.GetTransformed(), 15);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(4, runner.Errors.Count);
			}
			{
				QaMaxLength test = new QaMaxLength(tr.GetTransformed(), 15);
				IFilterEditTest ft = test;
				ft.SetIssueFilters(
					"filter", new List<IIssueFilter> { new IfInvolvedRows("t0.Nr = 12 OR t1.Nr = 6"){Name = "filter"} });
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}

		}

		private IFeatureClass CreateFeatureClass(IFeatureWorkspace ws, string name,
												 esriGeometryType geometryType,
												 IList<IField> customFields = null)
		{
			List<IField> fields = new List<IField>();
			fields.Add(FieldUtils.CreateOIDField());
			if (customFields != null)
			{
				fields.AddRange(customFields);
			}
			fields.Add(FieldUtils.CreateShapeField(
						   "Shape", geometryType,
						   SpatialReferenceUtils.CreateSpatialReference
						   ((int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
							true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name,
				FieldUtils.CreateFields(fields));
			return fc;
		}

	}
}
