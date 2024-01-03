using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.IssueFilters;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test.IssueFilters
{
	[TestFixture]
	public class IfNearTest
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
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("IfNearTest");

			IFeatureClass lineFc = TestWorkspaceUtils.CreateSimpleFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(1, 9).LineTo(10, 10).Curve;
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

			IFeatureClass polyFc = TestWorkspaceUtils.CreateSimpleFeatureClass(
				ws, "polyFc", esriGeometryType.esriGeometryPolygon);
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
				IfNear i = new IfNear(ReadOnlyTableFactory.Create(polyFc), 2);
				Container.IFilterEditTest filterTest = test;
				filterTest.SetIssueFilters(null, new Container.IIssueFilter[] { i });

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();

				Assert.AreEqual(2, runner.Errors.Count);
			}

		}

		[Test]
		public void CanFilterPoints()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("IfNearTest");

			IFeatureClass lineFc = TestWorkspaceUtils.CreateSimpleFeatureClass(
				ws, "pointFc", esriGeometryType.esriGeometryPoint);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = new PointClass { X = 9, Y = 9 };
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = new PointClass { X = 7.5, Y = 5 };
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = new PointClass { X = 13, Y = 13 };
				f.Store();
			}

			IFeatureClass polyFc = TestWorkspaceUtils.CreateSimpleFeatureClass(
				ws, "polyFc", esriGeometryType.esriGeometryPolygon);
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
				IfNear i = new IfNear(ReadOnlyTableFactory.Create(polyFc), 2);
				Container.IFilterEditTest filterTest = test;
				filterTest.SetIssueFilters(null, new Container.IIssueFilter[] { i });

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();

				Assert.AreEqual(2, runner.Errors.Count);
			}
			{
				IfNear i = new IfNear(ReadOnlyTableFactory.Create(polyFc), 2) { Name = "IfNear" };
				Container.IFilterEditTest filterTest = test;
				filterTest.SetIssueFilters("NOT ifNear", new Container.IIssueFilter[] { i });

				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();

				Assert.AreEqual(1, runner.Errors.Count);
			}

		}

	}
}
