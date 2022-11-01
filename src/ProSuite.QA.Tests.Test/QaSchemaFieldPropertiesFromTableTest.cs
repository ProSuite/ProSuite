using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaFieldPropertiesFromTableTest
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

		// TODO add tests to cover all cases (expected domain etc.)

		#region Tests based on QaSchemaTests.mdb

		[Test]
		public void ValidTable1()
		{
			NoErrors(GetErrors("GEO_00100024004"));
		}

		[Test]
		public void ValidTable2()
		{
			NoErrors(GetErrors("GEO_00100059002"));
		}

		[Test]
		public void ValidTable3()
		{
			NoErrors(GetErrors("GEO_00100633001"));
		}

		[Test]
		public void InvalidTable1()
		{
			IList<QaError> errors = GetErrors("GEO_00100004001");

			Assert.AreEqual(1, errors.Count);
			Assert.AreEqual(
				"Field 'NAME' has same alias ('Gemeindename') as specification for field 'GEMEINDE'. Field name should also be equal",
				errors[0].Description);
		}

		[Test]
		public void InvalidTable2()
		{
			IList<QaError> errors = GetErrors("GEO_00100436001");
			Assert.AreEqual(3, errors.Count);

			Assert.AreEqual(
				"Expected field length for field 'GEMEINDE': 30. Actual field length: 13",
				errors[0].Description);
			Assert.AreEqual(
				"Expected alias name for field 'GEMEINDE': 'Gemeindename'. Actual alias name: 'Name der Standortgemeinde'",
				errors[1].Description);
			Assert.AreEqual(
				"Expected field type for field 'Y_COORD': Double. Actual field type: Text",
				errors[2].Description);
		}

		[Test]
		public void InvalidTable3()
		{
			// errors via 'reserved fields' lookup are reported by QaSchemaReservedFieldProperties
			IList<QaError> errors = GetErrors("GEO_00100510001");

			Assert.AreEqual(0, errors.Count);
		}

		#endregion

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
			ITable fieldSpecificationsTable = workspace.OpenTable("FieldProperties");

			var test = new QaSchemaFieldPropertiesFromTable(
				ReadOnlyTableFactory.Create(table),
				ReadOnlyTableFactory.Create(fieldSpecificationsTable),
				matchAliasName: true);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}
	}
}
