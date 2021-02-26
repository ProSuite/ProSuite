using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Test.TestData;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaFieldDomainsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		#region Tests based on QaSchemaTests.mdb

		[Test]
		public void ValidTable1()
		{
			NoErrors(GetErrors("GEO_00100004001"));
		}

		[Test]
		public void ValidTable2()
		{
			NoErrors(GetErrors("GEO_00100024004"));
		}

		[Test]
		public void InvalidTable1()
		{
			IList<QaError> errors = GetErrors("GEO_00100059002");

			Assert.AreEqual(1, errors.Count);

			Assert.AreEqual(
				"Domain 'MST_MASTN' is used for field 'MASTN' with type Long Integer, but the domain field type is Short Integer",
				errors[0].Description);
		}

		[Test]
		public void ValidTable3()
		{
			NoErrors(GetErrors("GEO_00100436001"));
		}

		[Test]
		public void ValidTable4()
		{
			NoErrors(GetErrors("GEO_00100510001"));
		}

		[Test]
		public void ValidTable5()
		{
			NoErrors(GetErrors("GEO_00100633001"));
		}

		private static void NoErrors([NotNull] ICollection<QaError> errors)
		{
			Assert.AreEqual(0, errors.Count);
		}

		[NotNull]
		private static IList<QaError> GetErrors([NotNull] string tableName)
		{
			var locator = TestDataUtils.GetTestDataLocator();
			string path = locator.GetPath("QaSchemaTests.mdb");

			IFeatureWorkspace workspace = WorkspaceUtils.OpenPgdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			var test = new QaSchemaFieldDomains(table);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}

		#endregion
	}
}
