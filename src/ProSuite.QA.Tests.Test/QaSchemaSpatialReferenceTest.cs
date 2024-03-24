using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaSpatialReferenceTest
	{
		private IFeatureWorkspace _workspace;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_workspace = TestWorkspaceUtils.CreateTestFgdbWorkspace(GetType().Name);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanHaveDifferentDomains()
		{
			ISpatialReference sr1 = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				(int) esriSRVerticalCSType.esriSRVertCS_Landeshohennetz1995);
			SpatialReferenceUtils.SetXYDomain(sr1, 0, 0, 3000000, 3000000, 0.0001, 0.001);
			SpatialReferenceUtils.SetZDomain(sr1, 0, 3000000, 0.0001, 0.001);
			SpatialReferenceUtils.SetMDomain(sr1, 0, 3000000, 0.0001, 0.001);

			ISpatialReference sr2 = SpatialReferenceUtils.CreateSpatialReference(
				(int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				(int)esriSRVerticalCSType.esriSRVertCS_Landeshohennetz1995);
			SpatialReferenceUtils.SetXYDomain(sr2, -100, -100, 6000000, 6000000, 0.0002, 0.001);
			SpatialReferenceUtils.SetZDomain(sr2, -100, 3000000, 0.0002, 0.001);
			SpatialReferenceUtils.SetMDomain(sr2, -100, 3000000, 0.0002, 0.001);

			var ws = TestWorkspaceUtils.CreateTestFgdbWorkspace("CanHaveDifferentDomains");
			var fc1 = DatasetUtils.CreateSimpleFeatureClass(ws, "fc1",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPoint, sr1)));

			var fc2 = DatasetUtils.CreateSimpleFeatureClass(
				ws, "fc2",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPoint, sr2)));

			QaSchemaSpatialReference test = new QaSchemaSpatialReference(
				ReadOnlyTableFactory.Create(fc1), ReadOnlyTableFactory.Create(fc2), false, false,
				false);
			var runner = new QaTestRunner(test);

			Assert.AreEqual(0, runner.Execute());

			test.CompareXYDomainOrigin = true;
			Assert.AreEqual(1, runner.Execute());

			test.CompareXYResolution = true;
			Assert.AreEqual(2, runner.Execute());

			test.CompareZDomainOrigin = true;
			Assert.AreEqual(3, runner.Execute());

			test.CompareZResolution = true;
			Assert.AreEqual(4, runner.Execute());

			test.CompareMDomainOrigin = true;
			Assert.AreEqual(5, runner.Execute());

			test.CompareMResolution = true;
			Assert.AreEqual(6, runner.Execute());
		}
	}
}
