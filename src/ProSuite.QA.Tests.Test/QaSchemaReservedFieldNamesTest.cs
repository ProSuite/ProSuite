using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Test.TestData;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaReservedFieldNamesTest
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

		[Test]
		public void InvalidTable1()
		{
			IList<QaError> errors = GetErrors("GEO_00100004001");

			Assert.AreEqual(1, errors.Count);
			Assert.AreEqual(
				"Field name 'NAME' corresponds to a reserved name. Reason: reserviert von Access",
				errors[0].Description);
		}

		[Test]
		public void InvalidTable2()
		{
			IList<QaError> errors = GetErrors("GEO_00100510001");

			Assert.AreEqual(2, errors.Count);
			Assert.AreEqual(
				"Field name 'ZRD_GDE_NAME' corresponds to a reserved name. Reason: Schreibweise. Valid name: GEMEINDE",
				errors[0].Description);
			Assert.AreEqual(
				"Field name 'ZRD_GDE_BFSNR' corresponds to a reserved name. Reason: Schreibweise. Valid name: BFS_NR",
				errors[1].Description);
		}

		[Test]
		public void ValidTable1()
		{
			IList<QaError> errors = GetErrors("GEO_00100024004");

			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void ValidTable2()
		{
			IList<QaError> errors = GetErrors("GEO_00100059002");

			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void ValidTable3()
		{
			IList<QaError> errors = GetErrors("GEO_00100436001");

			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void ValidTable4()
		{
			IList<QaError> errors = GetErrors("GEO_00100633001");

			Assert.AreEqual(0, errors.Count);
		}

		[NotNull]
		private static IList<QaError> GetErrors([NotNull] string tableName)
		{
			var locator = TestDataUtils.GetTestDataLocator();
			string path = locator.GetPath("QaSchemaTests.mdb");

			IFeatureWorkspace workspace = WorkspaceUtils.OpenPgdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			ITable reservedNamesTable = workspace.OpenTable("ReservedFieldNames");
			var test = new QaSchemaReservedFieldNames(table, reservedNamesTable,
			                                          "ReservedWord",
			                                          "Reason", "ValidFieldName");

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}
	}
}
