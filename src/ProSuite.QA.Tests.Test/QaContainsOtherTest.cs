using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Licensing;
using ProSuite.QA.Container;
using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaContainsOtherTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			TestWorkspaceUtils.CreateTestFgdbWorkspace("QaContainesOtherTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		[Ignore("Uses local data")]
		public void TestRealDataF5()
		{
			//          <CoveringFeatureSet name="Kanton Luzern Buffer 10m" whereClause="OBJECTID = 1">
			//<FeatureClass catalogPath=@"D:\git\ebp-far\CtLU.ProSuite\CtLU.ProSuite\src\CtLU.GDM.Services.Test\TestData\QA_Rules.mdb\Kanton_Luzern_Buffer_00010m" />

			IWorkspace coverWs = TestDataUtils.OpenPgdb("validfields.mdb");
			IFeatureClass covers =
				((IFeatureWorkspace)coverWs).OpenFeatureClass("Kanton_Luzern_Buffer_00010m");

			IWorkspace withinWs =
				TestDataUtils.OpenPgdb("20111201_Produkte_SDE_zALL_bereinigt.mdb");
			IFeatureClass within =
				((IFeatureWorkspace)withinWs).OpenFeatureClass("GEO_00100420001");

			var test = new QaContainsOther(ReadOnlyTableFactory.Create(covers),
			                               ReadOnlyTableFactory.Create(within));
			test.SetConstraint(0, "OBJECTID = 1");
			test.SetConstraint(1, "OBJECTID = 69157");

			var ctr = new QaContainerTestRunner(10000, test);
			ctr.Execute();
			Assert.AreEqual(0, ctr.Errors.Count);
		}
	}
}
