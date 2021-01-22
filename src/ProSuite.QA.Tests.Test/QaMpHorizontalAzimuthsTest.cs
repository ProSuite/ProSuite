using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMpHorizontalAzimuthsTest
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
		public void CanTestMultiPatches()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);
			// make sure the table is known by the workspace

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(5, 4, 0).Add(5, 8, 0).Add(8, 8, 0).Add(8, 4, 0)
			            .StartInnerRing(6, 5, 0).Add(6.01, 7, 0).Add(7, 7, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalAzimuths(featureClassMock, 5, 0, 0, false);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyAzimuthsInToleranceNotReported()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);
			// make sure the table is known by the workspace

			const double azimuthToleranceDeg = 0.5;
			const double dy = 10;
			double xLimit = dy * Math.Tan(MathUtils.ToRadians(azimuthToleranceDeg));
			double xError = xLimit + 0.001;

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 0)
			            .Add(0, dy, 0)
			            .Add(xError, 2 * dy, 0)
			            .Add(xError, 3 * dy, 0)
			            .Add(8, 4, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalAzimuths(featureClassMock, 5, azimuthToleranceDeg, 0,
			                                      false);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);

			double xNoError = xLimit - 0.001;
			construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 0)
			            .Add(0, dy, 0)
			            .Add(xNoError, 2 * dy, 0)
			            .Add(xNoError, 3 * dy, 0)
			            .Add(8, 4, 0);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalAzimuths(featureClassMock, 5, azimuthToleranceDeg, 0,
			                                  false);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void VerifyAnglesLargerNearAngleNotChecked()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);
			// make sure the table is known by the workspace

			const double nearAngle = 5;
			const double dy = 10;
			double xLimit = dy * Math.Tan(MathUtils.ToRadians(nearAngle));
			double xNotChecked = xLimit + 0.001;

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 0)
			            .Add(0, dy, 0)
			            .Add(xNotChecked, 2 * dy, 0)
			            .Add(xNotChecked, 3 * dy, 0)
			            .Add(8, 4, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalAzimuths(featureClassMock, nearAngle, 0, 0, false);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			double xChecked = xLimit - 0.001;
			construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 0)
			            .Add(0, dy, 0)
			            .Add(xChecked, 2 * dy, 0)
			            .Add(xChecked, 3 * dy, 0)
			            .Add(8, 4, 0);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalAzimuths(featureClassMock, nearAngle, 0, 0, false);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifySegmentsGroupContinuously()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);
			// make sure the table is known by the workspace

			const double nearAngle = 5;
			const double y = 10;

			var construction = new MultiPatchConstruction();
			double xi = 0;
			double yi = y;
			construction.StartOuterRing(0, 0, 0)
			            .Add(xi, yi, 0);

			double angleRad = 1.01 * MathUtils.ToRadians(nearAngle);
			for (int i = 1; i < 4; i++)
			{
				double currentAngle = i * angleRad;
				double dx = y * Math.Sin(currentAngle);
				double dy = y * Math.Cos(currentAngle);

				xi += dx;
				yi += dy;

				construction.Add(xi, yi, 0);
			}

			construction.Add(20, 0, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalAzimuths(featureClassMock, nearAngle, 0, 0, false);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			construction = new MultiPatchConstruction();
			xi = 0;
			yi = y;
			construction.StartOuterRing(0, 0, 0)
			            .Add(xi, yi, 0);

			angleRad = 0.99 * MathUtils.ToRadians(nearAngle);
			for (int i = 1; i < 4; i++)
			{
				double currentAngle = i * angleRad;
				double dx = y * Math.Sin(currentAngle);
				double dy = y * Math.Cos(currentAngle);

				xi += dx;
				yi += dy;

				construction.Add(xi, yi, 0);
			}

			construction.Add(20, 0, 0);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalAzimuths(featureClassMock, nearAngle, 0, 0, false);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyNotHorizontalLinesIgnored()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);
			// make sure the table is known by the workspace

			const double horizontalToleranceDeg = 1;
			const double dy = 10;
			double zLimit = dy * Math.Tan(MathUtils.ToRadians(horizontalToleranceDeg));
			double zNotTested = zLimit + 0.001;

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 0)
			            .Add(0.01, dy, zNotTested)
			            .Add(0, 2 * dy, 0)
			            .Add(-0.01, 3 * dy, zNotTested)
			            .Add(8, 4, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalAzimuths(featureClassMock, 5, 0,
			                                      horizontalToleranceDeg, false);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			double zTested = zLimit - 0.001;
			construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 0)
			            .Add(0.01, dy, zTested)
			            .Add(0, 2 * dy, 0)
			            .Add(-0.01, 3 * dy, zTested)
			            .Add(8, 4, 0);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalAzimuths(featureClassMock, 5, 0, horizontalToleranceDeg,
			                                  false);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}
	}
}
