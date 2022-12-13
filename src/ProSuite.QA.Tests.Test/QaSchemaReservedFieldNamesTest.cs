using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Testing;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaReservedFieldNamesTest
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
			string path = TestDataPreparer.ExtractZip("QaSchemaTests.gdb.zip")
			                              .GetPath();

			IFeatureWorkspace workspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			ITable reservedNamesTable = workspace.OpenTable("ReservedFieldNames");
			var test = new QaSchemaReservedFieldNames(
				ReadOnlyTableFactory.Create(table),
				ReadOnlyTableFactory.Create(reservedNamesTable),
				"ReservedWord",
				"Reason", "ValidFieldName");

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}
	}
}
