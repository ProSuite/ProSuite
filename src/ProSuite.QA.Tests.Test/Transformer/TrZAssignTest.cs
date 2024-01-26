using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Tests.Surface;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test.Transformer;

public class TrZAssignTest
{
	private string _simpleGdbPath;

	[OneTimeSetUp]
	public void SetupFixture()
	{
		TestUtils.InitializeLicense(activateAdvancedLicense: true);
		_simpleGdbPath = Commons.AO.Test.TestData.GetGdb1Path();
	}

	[OneTimeTearDown]
	public void TearDownFixture()
	{
		TestUtils.ReleaseLicense();
	}

	[Test]
	public void CanUseTrZAssign()
	{
		int idLv95 = (int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
		ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

		IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

		IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
			FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(
					"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));

		{
			IFeature f = fc.CreateFeature();
			f.Shape = CurveConstruction.StartLine(2600000, 1200000)
									   .LineTo(2601000, 1201000).Curve;
			f.Store();
		}

		IFeatureWorkspace rws = WorkspaceUtils.OpenFeatureWorkspace(_simpleGdbPath);
		IRasterDataset rds = ((IRasterWorkspace2) rws).OpenRasterDataset("DHM200_Bern");

		RasterDatasetReference rasterRef = new RasterDatasetReference((IRasterDataset2) rds);

		IReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
		TrZAssign tr = new TrZAssign(roFc, rasterRef);
		Qa3dConstantZ test =
			new Qa3dConstantZ(tr.GetTransformed(), 0);

		{
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}
	}
}
