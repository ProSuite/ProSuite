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
	public class QaSchemaFieldNameRegexTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private const string _pattern = @"^[A-Za-z][A-Za-z0-9_]*$";

		private const string _patternDescription =
			"Gültige Zeichen für Feldnamen sind A-Z (ohne Umlaute), 0-9 sowie '_'";

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
		public void InvalidTable()
		{
			IList<QaError> errors = GetErrors("GEO_00100436001", _pattern, false,
			                                  _patternDescription);

			Assert.AreEqual(1, errors.Count);
			Assert.AreEqual(
				string.Format("The field name 'LMPÜNAME' does not match the pattern for '{0}'",
				              _patternDescription), errors[0].Description);
		}

		[Test]
		public void ValidTable1()
		{
			IList<QaError> errors = GetErrors("GEO_00100004001", _pattern, false,
			                                  _patternDescription);

			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void ValidTable2()
		{
			IList<QaError> errors = GetErrors("GEO_00100024004", _pattern, false,
			                                  _patternDescription);

			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void ValidTable3()
		{
			IList<QaError> errors = GetErrors("GEO_00100059002", _pattern, false,
			                                  _patternDescription);

			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void ValidTable4()
		{
			IList<QaError> errors = GetErrors("GEO_00100510001", _pattern, false,
			                                  _patternDescription);

			Assert.AreEqual(0, errors.Count);
		}

		[Test]
		public void ValidTable5()
		{
			IList<QaError> errors = GetErrors("GEO_00100633001", _pattern, false,
			                                  _patternDescription);

			Assert.AreEqual(0, errors.Count);
		}

		[NotNull]
		private static IList<QaError> GetErrors([NotNull] string tableName,
		                                        [NotNull] string pattern,
		                                        bool matchIsError,
		                                        [CanBeNull] string patternDescription)
		{
			var locator = TestDataUtils.GetTestDataLocator();
			string path = locator.GetPath("QaSchemaTests.mdb");

			IFeatureWorkspace workspace = WorkspaceUtils.OpenPgdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			var test = new QaSchemaFieldNameRegex(
				ReadOnlyTableFactory.Create(table), pattern, matchIsError, patternDescription);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}
	}
}
