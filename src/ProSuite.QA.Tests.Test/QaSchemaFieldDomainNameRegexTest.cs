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
	public class QaSchemaFieldNameRegexTest
	{
		private const string _pattern = @"^[A-Za-z][A-Za-z0-9_]*$";

		private const string _patternDescription =
			"Gültige Zeichen für Feldnamen sind A-Z (ohne Umlaute), 0-9 sowie '_'";

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
			string path = TestDataPreparer.ExtractZip("QaSchemaTests.gdb.zip")
			                              .GetPath();

			IFeatureWorkspace workspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			var test = new QaSchemaFieldNameRegex(
				ReadOnlyTableFactory.Create(table), pattern, matchIsError, patternDescription);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}
	}
}
