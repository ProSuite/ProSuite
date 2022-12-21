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
	public class QaSchemaFieldDomainsTest
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
			string path = TestDataPreparer.ExtractZip("QaSchemaTests.gdb.zip")
			                              .GetPath();

			IFeatureWorkspace workspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			var test = new QaSchemaFieldDomains(ReadOnlyTableFactory.Create(table));

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}

		#endregion
	}
}
