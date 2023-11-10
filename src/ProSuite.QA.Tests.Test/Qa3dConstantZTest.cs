using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class Qa3dConstantZTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void SpatialReferenceSet()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("Ws3dConstantZ");

			IFeatureClass fc = TestWorkspaceUtils.CreateSimpleFeatureClass(
				ws, "testFc",
				FieldUtils.CreateFields(FieldUtils.CreateOIDField()),
				esriGeometryType.esriGeometryPolyline,
				esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, hasZ: true);

			{
				IFeature f = fc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10, 5).LineTo(20, 20, 5).LineTo(30, 30, 5)
				                           .LineTo(40, 40, 6).LineTo(44, 44, 6).LineTo(50, 50, 5)
				                           .Curve;
				f.Store();
			}

			var test = new Qa3dConstantZ(ReadOnlyTableFactory.Create(fc), 0.01);

			var testRunner = new QaContainerTestRunner(1000, test);
			testRunner.TestContainer.QaError += (e, a) =>
			{
				Assert.NotNull(a.QaError.Geometry?.SpatialReference);
			};
			testRunner.Execute();
			Assert.AreEqual(1, testRunner.Errors.Count);
		}
	}
}
