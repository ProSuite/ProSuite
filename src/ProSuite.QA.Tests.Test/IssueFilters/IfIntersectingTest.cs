using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.IssueFilters;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using System.Collections.Generic;

namespace ProSuite.QA.Tests.Test.IssueFilters
{
	[TestFixture]
	public class IfIntersectingTest
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
		public void CanFilterLines()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("IfIntersectingTest");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(5, 5).LineTo(15, 5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(15, 15).LineTo(20, 12).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(8, 3).LineTo(3, 8).Curve;
				f.Store();
			}

			IFeatureClass polyFc =
				CreateFeatureClass(ws, "polyFc", esriGeometryType.esriGeometryPolygon);
			{
				IFeature f = polyFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(0, 0).LineTo(10, 0).LineTo(10, 10)
										   .LineTo(0, 10).ClosePolygon();
				f.Store();
			}


			QaConstraint test = new QaConstraint(ReadOnlyTableFactory.Create(lineFc), "1 = 0");
			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(3, runner.Errors.Count);
			}
			{
				IfIntersecting i = new IfIntersecting(ReadOnlyTableFactory.Create(polyFc));
				Container.IFilterEditTest filterTest = test;
				filterTest.SetIssueFilters(null, new Container.IIssueFilter[] { i });

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

			bool hasZ = geometryType == esriGeometryType.esriGeometryMultiPatch;
			fields.Add(FieldUtils.CreateShapeField(
						   "Shape", geometryType,
						   SpatialReferenceUtils.CreateSpatialReference
						   ((int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
							true), 1000, hasZ));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name,
				FieldUtils.CreateFields(fields));
			return fc;
		}

	}
}
