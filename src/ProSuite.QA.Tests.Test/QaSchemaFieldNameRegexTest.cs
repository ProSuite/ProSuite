using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaFieldDomainNameRegexTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		private const string _pattern = @"^[A-Z]{3}[_][A-Z0-9_]*$";

		private const string _patternDescription =
			"Gültige Zeichen für Domaenennamen sind A-Z (ohne Umlaute), 0-9 sowie '_'. " +
			"Domaenennamen muessen mit einer dreistelligen Buchstabenfolge gefolgt von einem Underscore beginnen.";

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void InvalidValidTable1()
		{
			IList<QaError> errors = GetErrors("GEO_00100004001", _pattern, false,
			                                  _patternDescription);

			Assert.AreEqual(1, errors.Count);
			Assert.AreEqual(
				string.Format("The domain name 'ZONENTYP' does not match the pattern for '{0}'",
				              _patternDescription), errors[0].Description);
		}

		[Test]
		public void ValidTable1()
		{
			Assert.AreEqual(0, GetErrors("GEO_00100024004", _pattern, false,
			                             _patternDescription).Count);
		}

		[Test]
		public void ValidTable2()
		{
			Assert.AreEqual(0, GetErrors("GEO_00100059002", _pattern, false,
			                             _patternDescription).Count);
		}

		[Test]
		public void ValidTable3()
		{
			Assert.AreEqual(0, GetErrors("GEO_00100436001", _pattern, false,
			                             _patternDescription).Count);
		}

		[Test]
		public void InvalidTable2()
		{
			IList<QaError> errors = GetErrors("GEO_00100510001", _pattern, false,
			                                  _patternDescription);

			Assert.AreEqual(10, errors.Count);
			Assert.AreEqual(
				string.Format(
					"The domain name 'A_GZB_OBJART' does not match the pattern for '{0}'",
					_patternDescription), errors[0].Description);
			Assert.AreEqual(
				string.Format("The domain name 'A_GZB_BEZ' does not match the pattern for '{0}'",
				              _patternDescription), errors[1].Description);
			Assert.AreEqual(
				string.Format(
					"The domain name 'A_GZB_BEZZUS' does not match the pattern for '{0}'",
					_patternDescription), errors[2].Description);
			Assert.AreEqual(
				string.Format(
					"The domain name 'A_GZB_BEZANG' does not match the pattern for '{0}'",
					_patternDescription), errors[3].Description);
			Assert.AreEqual(
				string.Format(
					"The domain name 'A_GWS_NITRAT' does not match the pattern for '{0}'",
					_patternDescription), errors[4].Description);
			Assert.AreEqual(
				string.Format("The domain name 'A_ERF_VORL' does not match the pattern for '{0}'",
				              _patternDescription), errors[5].Description);
			Assert.AreEqual(
				string.Format("The domain name 'A_VORL_ART' does not match the pattern for '{0}'",
				              _patternDescription), errors[6].Description);
			Assert.AreEqual(
				string.Format(
					"The domain name 'A_VORL_MSTAB' does not match the pattern for '{0}'",
					_patternDescription), errors[7].Description);
			Assert.AreEqual(
				string.Format(
					"The domain name 'A_VORL_HERK' does not match the pattern for '{0}'",
					_patternDescription), errors[8].Description);
			Assert.AreEqual(
				string.Format(
					"The domain name 'a_erf_genau' does not match the pattern for '{0}'",
					_patternDescription), errors[9].Description);
		}

		[Test]
		public void ValidTable4()
		{
			Assert.AreEqual(0, GetErrors("GEO_00100633001", _pattern, false,
			                             _patternDescription).Count);
		}

		[NotNull]
		private static IList<QaError> GetErrors([NotNull] string tableName,
		                                        [NotNull] string pattern,
		                                        bool matchIsError,
		                                        [CanBeNull] string patternDescription)
		{
			var locator = new TestDataLocator(@"..\..\EsriDE.ProSuite\src");
			string path = locator.GetPath("QaSchemaTests.mdb");

			IFeatureWorkspace workspace = WorkspaceUtils.OpenPgdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			var test = new QaSchemaFieldDomainNameRegex(table, pattern, matchIsError,
			                                            patternDescription);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}
	}
}
