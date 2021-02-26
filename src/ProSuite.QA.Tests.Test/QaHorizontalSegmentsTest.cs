using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaHorizontalSegmentsTest
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
		public void CanTestMultiPatches()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 10)
			            .Add(-5, 4, 10.1)
			            .Add(-5, -3, 10.05)
			            .Add(5, -7, 10);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaHorizontalSegments(featureClassMock, 5, 0);
			var runner = new QaTestRunner(test);
			runner.KeepGeometry = true;

			runner.Execute(row1);

			Assert.AreEqual(1, runner.Errors.Count);
			Assert.IsTrue(runner.ErrorGeometries[0].Envelope.ZMin > 5);
		}

		[Test]
		public void CanTestPolylines()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryPolyline);

			CurveConstruction construction = CurveConstruction
			                                 .StartLine(5, 4, 10).LineTo(-5, 4, 10.1)
			                                 .LineTo(-5, -3, 10.05)
			                                 .LineTo(5, -7, 10);

			IFeature row1 = featureClassMock.CreateFeature(construction.Curve);
			GeometryUtils.EnsureSpatialReference(row1.Shape, featureClassMock);

			var test = new QaHorizontalSegments(featureClassMock, 5, 0);
			var runner = new QaTestRunner(test);
			runner.KeepGeometry = true;

			runner.Execute(row1);

			Assert.AreEqual(1, runner.Errors.Count);
			Assert.IsTrue(runner.ErrorGeometries[0].Envelope.ZMin > 5);
		}

		[Test]
		public void CanTestPolygons()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryPolygon);

			CurveConstruction construction = CurveConstruction
			                                 .StartPoly(5, 4, 10).LineTo(-5, 4, 10.1)
			                                 .LineTo(-5, -3, 10.05)
			                                 .LineTo(5, -7, 10);

			IFeature row1 = featureClassMock.CreateFeature(construction.ClosePolygon());
			GeometryUtils.EnsureSpatialReference(row1.Shape, featureClassMock);

			var test = new QaHorizontalSegments(featureClassMock, 5, 0);
			var runner = new QaTestRunner(test);
			runner.KeepGeometry = true;

			runner.Execute(row1);

			Assert.AreEqual(1, runner.Errors.Count);
			Assert.IsTrue(runner.ErrorGeometries[0].Envelope.ZMin > 5);
		}

		[Test]
		public void VerifyErrorInToleranceNotReported()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);

			const double toleranceAngle = 0.5;
			const double dy = 10;
			double dz = dy * Math.Tan(MathUtils.ToRadians(toleranceAngle));

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 0, 0)
			            .Add(5, dy, 1.1 * dz)
			            .Add(5, 2 * dy, 0)
			            .Add(4, 4, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaHorizontalSegments(featureClassMock, 5, toleranceAngle);
			var runner = new QaTestRunner(test);

			runner.Execute(row1);

			Assert.AreEqual(1, runner.Errors.Count);

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 0, 0)
			            .Add(5, dy, 0.9 * dz)
			            .Add(5, 2 * dy, 0)
			            .Add(4, 4, 0);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaHorizontalSegments(featureClassMock, 5, toleranceAngle);
			runner = new QaTestRunner(test);
			runner.Execute(row1);

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void VerifyStepSegmentsNotTested()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);

			const double limitAngle = 5;
			const double dy = 10;
			double dz = dy * Math.Tan(MathUtils.ToRadians(limitAngle));

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 0, 0)
			            .Add(5, dy, dz + 0.01)
			            .Add(5, 2 * dy, 0)
			            .Add(4, 4, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaHorizontalSegments(featureClassMock, limitAngle, 0);
			var runner = new QaTestRunner(test);

			runner.Execute(row1);

			Assert.AreEqual(0, runner.Errors.Count);

			construction = new MultiPatchConstruction();
			construction.StartOuterRing(5, 0, 0)
			            .Add(5, dy, dz - 0.01)
			            .Add(5, 2 * dy, 0)
			            .Add(4, 4, 0);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaHorizontalSegments(featureClassMock, limitAngle, 0);
			runner = new QaTestRunner(test);
			runner.Execute(row1);

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyErrorTypes()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);

			var construction = new MultiPatchConstruction();

			construction.StartRing(5, 4, 0)
			            .Add(-5, 4, 0.1)
			            .Add(-5, -3, 0.05)
			            .Add(5, -7, -0.1)
			            .Add(5, -8, -0.1)
			            .Add(5, -11, 0.0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaHorizontalSegments(featureClassMock, 5, 0);
			var runner = new QaTestRunner(test);

			runner.Execute(row1);

			Assert.AreEqual(2, runner.Errors.Count);
			Assert.AreNotEqual(runner.Errors[0].Description.Split()[1],
			                   runner.Errors[1].Description.Split()[1]);
		}
	}
}
