using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMpHorizontalPerpendicularTest
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
			// make sure the table is known by the workspace

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(5, 0, 0)
			            .Add(0.1, 5, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalPerpendicular(
				ReadOnlyTableFactory.Create(featureClassMock), 5, 0, 0, false, 0);
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
			            .Add(dy, xError, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalPerpendicular(
				ReadOnlyTableFactory.Create(featureClassMock), 5, azimuthToleranceDeg,
				0, false, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);

			double xNoError = xLimit - 0.001;
			construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 0)
			            .Add(0, dy, 0)
			            .Add(dy, xNoError, 0);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalPerpendicular(
				ReadOnlyTableFactory.Create(featureClassMock), 5, azimuthToleranceDeg, 0,
				false, 0);
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
			            .Add(dy, xNotChecked, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalPerpendicular(
				ReadOnlyTableFactory.Create(featureClassMock), nearAngle, 0, 0, false, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(0, runner.Errors.Count);

			double xChecked = xLimit - 0.001;
			construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 0)
			            .Add(0, dy, 0)
			            .Add(dy, xChecked, 0);

			row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalPerpendicular(
				ReadOnlyTableFactory.Create(featureClassMock), nearAngle, 0, 0, false, 0);
			runner = new QaTestRunner(test);
			runner.Execute(row1);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifySegmentsGroupContinuously()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);

			const double nearAngle = 5;

			var construction = new MultiPatchConstruction();
			construction.StartOuterRing(0, 0, 0)
			            .Add(5, 0, 0)
			            .Add(5.01, 1, 0)
			            .Add(10, 1, 0)
			            .Add(10.01, 0, 0)
			            .Add(5, -3, 0);

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalPerpendicular(
				ReadOnlyTableFactory.Create(featureClassMock), nearAngle, 0, 0, false, 0);
			var runner = new QaTestRunner(test);
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
			construction.StartRing(0, 0, 0)
			            .Add(0.01, dy, zNotTested)
			            .Add(dy, 0, 0);

			IFeature feature = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalPerpendicular(
				ReadOnlyTableFactory.Create(featureClassMock), 5, 0,
				horizontalToleranceDeg, false, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);
			Assert.AreEqual(0, runner.Errors.Count);

			double zTested = zLimit - 0.001;
			construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0)
			            .Add(0.01, dy, zTested)
			            .Add(dy, 0, 0);

			feature = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalPerpendicular(
				ReadOnlyTableFactory.Create(featureClassMock), 5, 0,
				horizontalToleranceDeg, false, 0);

			runner = new QaTestRunner(test);
			runner.Execute(feature);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyNotConnectedIgnored()
		{
			var featureClassMock = new FeatureClassMock(1, "mock",
			                                            esriGeometryType.esriGeometryMultiPatch);
			// make sure the table is known by the workspace

			const double connected = 0.1;
			double dx = connected * Math.Sqrt(0.5);

			var construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0)
			            .Add(1.1 * dx, -dx, 0)
			            .Add(10, -dx, 0)
			            .Add(0.1, 10, 0);

			IFeature feature = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpHorizontalPerpendicular(
				ReadOnlyTableFactory.Create(featureClassMock), 5, 0,
				0, true, connected);
			var runner = new QaTestRunner(test);
			runner.Execute(feature);
			Assert.AreEqual(0, runner.Errors.Count);

			construction = new MultiPatchConstruction();
			construction.StartRing(0, 0, 0)
			            .Add(0.9 * dx, -dx, 0)
			            .Add(10, -dx, 0)
			            .Add(0.1, 10, 0);

			feature = featureClassMock.CreateFeature(construction.MultiPatch);

			test = new QaMpHorizontalPerpendicular(
				ReadOnlyTableFactory.Create(featureClassMock), 5, 0, 0, true, connected);

			runner = new QaTestRunner(test);
			runner.Execute(feature);
			Assert.AreEqual(1, runner.Errors.Count);
		}
	}
}
