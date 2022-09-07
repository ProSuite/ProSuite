using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.IssueFilters;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;

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
		public void CanIntersectLines()
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
		public void CanIntersectPoints()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrIntersect");

			IFeatureClass pointFc =
				CreateFeatureClass(ws, "pointFc", esriGeometryType.esriGeometryPoint,
				                   new[] {FieldUtils.CreateIntegerField("IntVal")});
			IFeatureClass polyFc =
				CreateFeatureClass(ws, "polyFc", esriGeometryType.esriGeometryPolygon);

			{
				IFeature f = pointFc.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(55, 65);
				f.Value[1] = 5;
				f.Store();
			}
			{
				IFeature f = pointFc.CreateFeature();
				f.Shape = GeometryFactory.CreatePoint(20, 20);
				f.Value[1] = 5;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}

			TrIntersect tr = new TrIntersect(ReadOnlyTableFactory.Create(pointFc),
			                                 ReadOnlyTableFactory.Create(polyFc));
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "IntVal < 2");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "IntVal < 8");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
		}

		[Test]
		public void CanIntersectMultiPoints()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrIntersect");

			IFeatureClass pointFc =
				CreateFeatureClass(ws, "pointFc", esriGeometryType.esriGeometryMultipoint,
				                   new[] {FieldUtils.CreateIntegerField("IntVal")});
			IFeatureClass polyFc =
				CreateFeatureClass(ws, "polyFc", esriGeometryType.esriGeometryPolygon);

			{
				IFeature f = pointFc.CreateFeature();
				f.Shape = GeometryFactory.CreateMultipoint(GeometryFactory.CreatePoint(55, 65),
				                                           GeometryFactory.CreatePoint(20, 20));
				f.Value[1] = 5;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}

			TrIntersect tr = new TrIntersect(ReadOnlyTableFactory.Create(pointFc),
			                                 ReadOnlyTableFactory.Create(polyFc));
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "PartIntersected < 0.3");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "PartIntersected < 0.8");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
		}

		[Test]
		public void CanIntersectPolygons()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrIntersect");

			IFeatureClass pFc =
				CreateFeatureClass(ws, "pointFc", esriGeometryType.esriGeometryPolygon,
				                   new[] {FieldUtils.CreateIntegerField("IntVal")});
			IFeatureClass polyFc =
				CreateFeatureClass(ws, "polyFc", esriGeometryType.esriGeometryPolygon);

			{
				IFeature f = pFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(40, 40).LineTo(40, 60).LineTo(60, 60)
				                           .LineTo(60, 40).ClosePolygon();
				f.Value[1] = 5;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}

			TrIntersect tr = new TrIntersect(ReadOnlyTableFactory.Create(pFc),
			                                 ReadOnlyTableFactory.Create(polyFc));
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "PartIntersected < 0.2");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(tr.GetTransformed(), "PartIntersected < 0.26");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}
		}

		[Test]
		public void CanAccessAttributes()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrIntersect");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] {FieldUtils.CreateIntegerField("Nr")});
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] {FieldUtils.CreateIntegerField("Nr")});
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
				TransformedFeatureClass transformedClass = tr.GetTransformed();
				WriteFieldNames(transformedClass);
				QaMaxLength test = new QaMaxLength(transformedClass, 15);
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(4, runner.Errors.Count);
			}
			{
				TransformedFeatureClass transformedClass = tr.GetTransformed();
				WriteFieldNames(transformedClass);
				QaMaxLength test = new QaMaxLength(transformedClass, 15);
				IFilterEditTest ft = test;
				ft.SetIssueFilters(
					"filter",
					new List<IIssueFilter>
					{new IfInvolvedRows("Nr = 12 OR polyFc_Nr = 6") {Name = "filter"}});
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		private static void WriteFieldNames(GdbTable targetTable)
		{
			for (int i = 0; i < targetTable.Fields.FieldCount; i++)
			{
				IField field = targetTable.Fields.Field[i];

				Console.WriteLine(field.Name);
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
				           ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				            true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name,
				FieldUtils.CreateFields(fields));
			return fc;
		}
	}
}
