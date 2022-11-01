using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMpVerticalFacesTest
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
		public void CanFindNonVerticalPlane()
		{
			var fc = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryMultiPatch);

			var construction = new MultiPatchConstruction();
			construction.StartRing(5, 4, 0).Add(5, 8, 0).Add(5.01, 4, 10);
			IFeature f = fc.CreateFeature(construction.MultiPatch);

			var test = new QaMpVerticalFaces(ReadOnlyTableFactory.Create(fc), 85, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifySlopedPlaneNotChecked()
		{
			var fc = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryMultiPatch);

			const double slopeAngleDeg = 85;
			const double height = 10;
			const double x0 = 5;
			double dx = height * Math.Atan(MathUtils.ToRadians(90 - slopeAngleDeg));

			var construction = new MultiPatchConstruction();
			construction.StartRing(x0, 4, 0).Add(x0, 8, 0).Add(x0 + dx + 0.01, 4, height);
			// slight flatter than limit
			IFeature f = fc.CreateFeature(construction.MultiPatch);

			var test = new QaMpVerticalFaces(ReadOnlyTableFactory.Create(fc), slopeAngleDeg, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);

			construction = new MultiPatchConstruction();
			construction.StartRing(x0, 4, 0).Add(x0, 8, 0).Add(x0 + dx - 0.01, 4, height);
			// slight steeper than limit
			f = fc.CreateFeature(construction.MultiPatch);

			test = new QaMpVerticalFaces(ReadOnlyTableFactory.Create(fc), slopeAngleDeg, 0);
			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyVerticalPlaneNotReported()
		{
			var fc = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryMultiPatch);

			const double toleranceAngleDeg = 0.5;
			const double height = 10;
			const double x0 = 5;
			double dx = height * Math.Atan(MathUtils.ToRadians(toleranceAngleDeg));

			var construction = new MultiPatchConstruction();
			construction.StartRing(x0, 4, 0).Add(x0, 8, 0).Add(x0, 4, height); // full vertical
			IFeature f = fc.CreateFeature(construction.MultiPatch);

			var test = new QaMpVerticalFaces(ReadOnlyTableFactory.Create(fc), 85, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);

			construction = new MultiPatchConstruction();
			construction.StartRing(x0, 4, 0).Add(x0, 8, 0).Add(x0 + dx - 0.01, 4, height);
			// slightly steeper than tolerance
			f = fc.CreateFeature(construction.MultiPatch);

			test = new QaMpVerticalFaces(ReadOnlyTableFactory.Create(fc), 85, toleranceAngleDeg);
			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);

			construction = new MultiPatchConstruction();
			construction.StartRing(x0, 4, 0).Add(x0, 8, 0).Add(x0 + dx + 0.01, 4, height);
			// slightly less steep than tolerance
			f = fc.CreateFeature(construction.MultiPatch);

			test = new QaMpVerticalFaces(ReadOnlyTableFactory.Create(fc), 85, toleranceAngleDeg);
			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void VerifyResolutionProblemsNotReported()
		{
			var fc = new FeatureClassMock(1, "Fc", esriGeometryType.esriGeometryMultiPatch);
			double xyTolerance = GeometryUtils.GetXyTolerance((IFeatureClass) fc);

			const double slopeAngleDeg = 85;
			const double height = 10;
			const double x0 = 5;

			var construction = new MultiPatchConstruction();
			construction.StartRing(x0, 4, 0).Add(x0, 8, 0).Add(x0 + xyTolerance * 0.5, 4, height);
			// less than xyTolerance
			IFeature f = fc.CreateFeature(construction.MultiPatch);

			var test = new QaMpVerticalFaces(ReadOnlyTableFactory.Create(fc), slopeAngleDeg, 0);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);

			construction = new MultiPatchConstruction();
			construction.StartRing(x0, 4, 0).Add(x0, 8, 0).Add(x0 + xyTolerance * 1.5, 4, height);
			// more than xyTolerance
			f = fc.CreateFeature(construction.MultiPatch);

			test = new QaMpVerticalFaces(ReadOnlyTableFactory.Create(fc), slopeAngleDeg, 0);
			runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);
		}
	}
}
