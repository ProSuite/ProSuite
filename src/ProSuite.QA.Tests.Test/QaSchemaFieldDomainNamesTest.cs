using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Testing;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.ParameterTypes;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaFieldDomainNamesTest
	{
		private IFeatureWorkspace _workspace;

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

		[TearDown]
		public void TearDown()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			// important, otherwise locks are still there in next test
		}

		[Test]
		public void AllValid()
		{
			IDomain domain1 = DomainUtils.AddDomain(TestWorkspace, CreateCVDomain("DOM_FIELD1"));
			IDomain domain2 = DomainUtils.AddDomain(TestWorkspace,
			                                        CreateRangeDomain("DOM_FIELD2"));

			IField field1 = FieldUtils.CreateField("FIELD1", esriFieldType.esriFieldTypeInteger);
			IField field2 = FieldUtils.CreateField("FIELD2", esriFieldType.esriFieldTypeInteger);
			IField field3 = FieldUtils.CreateTextField("FIELD3", 20);

			((IFieldEdit) field1).Domain_2 = domain1;
			((IFieldEdit) field2).Domain_2 = domain2;

			ITable table = DatasetUtils.CreateTable(TestWorkspace,
			                                        MethodBase.GetCurrentMethod().Name,
			                                        FieldUtils.CreateOIDField(),
			                                        field1, field2, field3);

			const int maxLength = 10;
			const bool mustContainFieldName = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainNames(
					ReadOnlyTableFactory.Create(table), "DOM_", maxLength, mustContainFieldName,
					ExpectedCase.AllUpper));

			runner.Execute();

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void MissingPrefix()
		{
			IDomain domain1 = DomainUtils.AddDomain(TestWorkspace, CreateCVDomain("PRE_FIELD1"));
			IDomain domain2 = DomainUtils.AddDomain(TestWorkspace, CreateRangeDomain("FIELD2"));

			IField field1 = FieldUtils.CreateField("FIELD1", esriFieldType.esriFieldTypeInteger);
			IField field2 = FieldUtils.CreateField("FIELD2", esriFieldType.esriFieldTypeInteger);
			IField field3 = FieldUtils.CreateTextField("FIELD3", 20);

			((IFieldEdit) field1).Domain_2 = domain1;
			((IFieldEdit) field2).Domain_2 = domain2;

			ITable table = DatasetUtils.CreateTable(TestWorkspace,
			                                        MethodBase.GetCurrentMethod().Name,
			                                        FieldUtils.CreateOIDField(),
			                                        field1, field2, field3);

			const int maxLength = 10;
			const bool mustContainFieldName = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainNames(
					ReadOnlyTableFactory.Create(table), "PRE_", maxLength, mustContainFieldName,
					ExpectedCase.AllUpper));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void TooLong()
		{
			IDomain domain1 = DomainUtils.AddDomain(TestWorkspace,
			                                        CreateCVDomain("PRE_FIELD1_ABL"));
			IDomain domain2 = DomainUtils.AddDomain(TestWorkspace,
			                                        CreateRangeDomain("PRE_FIELD4"));

			IField field1 = FieldUtils.CreateField("FIELD1", esriFieldType.esriFieldTypeInteger);
			IField field2 = FieldUtils.CreateField("FIELD4", esriFieldType.esriFieldTypeInteger);
			IField field3 = FieldUtils.CreateTextField("FIELD3", 20);

			((IFieldEdit) field1).Domain_2 = domain1;
			((IFieldEdit) field2).Domain_2 = domain2;

			ITable table = DatasetUtils.CreateTable(TestWorkspace,
			                                        MethodBase.GetCurrentMethod().Name,
			                                        FieldUtils.CreateOIDField(),
			                                        field1, field2, field3);

			const int maxLength = 10;
			const bool mustContainFieldName = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainNames(
					ReadOnlyTableFactory.Create(table), "PRE_", maxLength, mustContainFieldName,
					ExpectedCase.AllUpper));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedAllUpper()
		{
			IDomain domain1 = DomainUtils.AddDomain(TestWorkspace,
			                                        CreateCVDomain("PRE_FIELD1_abc"));
			IDomain domain2 = DomainUtils.AddDomain(TestWorkspace,
			                                        CreateRangeDomain("PRE_FIELD2_123"));

			IField field1 = FieldUtils.CreateField("FIELD1", esriFieldType.esriFieldTypeInteger);
			IField field2 = FieldUtils.CreateField("FIELD2", esriFieldType.esriFieldTypeInteger);
			IField field3 = FieldUtils.CreateTextField("FIELD3", 20);

			((IFieldEdit) field1).Domain_2 = domain1;
			((IFieldEdit) field2).Domain_2 = domain2;

			ITable table = DatasetUtils.CreateTable(TestWorkspace,
			                                        MethodBase.GetCurrentMethod().Name,
			                                        FieldUtils.CreateOIDField(),
			                                        field1, field2, field3);

			const int maxLength = 20;
			const bool mustContainFieldName = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainNames(
					ReadOnlyTableFactory.Create(table), "PRE_", maxLength, mustContainFieldName,
					ExpectedCase.AllUpper));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void DoesNotContainFieldName()
		{
			IDomain domain1 = DomainUtils.AddDomain(TestWorkspace, CreateCVDomain("PRE_Field1_D"));
			IDomain domain2 = DomainUtils.AddDomain(TestWorkspace,
			                                        CreateRangeDomain("PRE_FIELD2"));

			IField field1 = FieldUtils.CreateField("FIELD1", esriFieldType.esriFieldTypeInteger);
			IField field2 = FieldUtils.CreateField("FIELD2", esriFieldType.esriFieldTypeInteger);
			IField field3 = FieldUtils.CreateField("FIELD3", esriFieldType.esriFieldTypeInteger);

			((IFieldEdit) field1).Domain_2 = domain1; // case mismatch
			((IFieldEdit) field2).Domain_2 = domain2;
			((IFieldEdit) field3).Domain_2 = domain2; // assign again - different field name

			ITable table = DatasetUtils.CreateTable(TestWorkspace,
			                                        MethodBase.GetCurrentMethod().Name,
			                                        FieldUtils.CreateOIDField(),
			                                        field1, field2, field3);

			const int maxLength = 20;
			const bool mustContainFieldName = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainNames(
					ReadOnlyTableFactory.Create(table), "PRE_", maxLength, mustContainFieldName,
					ExpectedCase.Any));

			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		#region Tests based on QaSchemaTests.mdb

		[Test]
		public void InvalidTableCase()
		{
			const bool mustContainFieldName = false;
			const string prefix = "";
			IList<QaError> errors = GetErrors("GEO_00100510001", 30, ExpectedCase.AllUpper,
			                                  mustContainFieldName, prefix);

			Assert.AreEqual(1, errors.Count);
			Assert.AreEqual(
				"Domain name 'a_erf_genau' has unexpected case: Name must be all uppercase: a_erf_genau",
				errors[0].Description);
		}

		[Test]
		public void ValidTableCase1()
		{
			const bool mustContainFieldName = false;
			const string prefix = "";
			Assert.AreEqual(0, GetErrors("GEO_00100004001", 30, ExpectedCase.AllUpper,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void ValidTableCase2()
		{
			const bool mustContainFieldName = false;
			const string prefix = "";
			Assert.AreEqual(0, GetErrors("GEO_00100024004", 30, ExpectedCase.AllUpper,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void ValidTableCase3()
		{
			const bool mustContainFieldName = false;
			const string prefix = "";
			Assert.AreEqual(0, GetErrors("GEO_00100059002", 30, ExpectedCase.AllUpper,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void ValidTableCase4()
		{
			const bool mustContainFieldName = false;
			const string prefix = "";
			Assert.AreEqual(0, GetErrors("GEO_00100436001", 30, ExpectedCase.AllUpper,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void ValidTableCase5()
		{
			const bool mustContainFieldName = false;
			const string prefix = "";
			Assert.AreEqual(0, GetErrors("GEO_00100633001", 31, ExpectedCase.AllUpper,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void InvalidTableContainedFieldName()
		{
			const bool mustContainFieldName = true;
			const string prefix = "";
			IList<QaError> errors = GetErrors("GEO_00100510001", 30, ExpectedCase.Any,
			                                  mustContainFieldName, prefix);

			Assert.AreEqual(10, errors.Count);

			Assert.AreEqual(
				"Domain name 'A_GZB_OBJART' does not contain the field name 'OBJEKTART'",
				errors[0].Description);
			Assert.AreEqual(
				"Domain name 'A_GZB_BEZ' does not contain the field name 'BEZEICHNUNG'",
				errors[1].Description);
			Assert.AreEqual(
				"Domain name 'A_GZB_BEZZUS' does not contain the field name 'BEZEICHNUNG_ZUSATZ'",
				errors[2].Description);
			Assert.AreEqual(
				"Domain name 'A_GZB_BEZANG' does not contain the field name 'BEZEICHNUNG_ANGABEN'",
				errors[3].Description);
			Assert.AreEqual(
				"Domain name 'A_GWS_NITRAT' does not contain the field name 'nitratgebiet'",
				errors[4].Description);
			Assert.AreEqual(
				"Domain name 'A_ERF_VORL' does not contain the field name 'ERFASSUNG_VORLAGE'",
				errors[5].Description);
			Assert.AreEqual(
				"Domain name 'A_VORL_ART' does not contain the field name 'ERF_VORL_ART'",
				errors[6].Description);
			Assert.AreEqual(
				"Domain name 'A_VORL_MSTAB' does not contain the field name 'ERF_VORL_MSTAB'",
				errors[7].Description);
			Assert.AreEqual(
				"Domain name 'A_VORL_HERK' does not contain the field name 'ERF_VORL_HERKUNFT'",
				errors[8].Description);
			Assert.AreEqual(
				"Domain name 'a_erf_genau' does not contain the field name 'ERF_GENAUIGKEIT'",
				errors[9].Description);
		}

		[Test]
		public void ValidTableContainedFieldName1()
		{
			const bool mustContainFieldName = true;
			const string prefix = "";
			Assert.AreEqual(0, GetErrors("GEO_00100004001", 30, ExpectedCase.Any,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void ValidTableContainedFieldName2()
		{
			const bool mustContainFieldName = true;
			const string prefix = "";
			Assert.AreEqual(0, GetErrors("GEO_00100024004", 30, ExpectedCase.Any,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void ValidTableContainedFieldName3()
		{
			const bool mustContainFieldName = true;
			const string prefix = "";
			Assert.AreEqual(0, GetErrors("GEO_00100059002", 30, ExpectedCase.Any,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void ValidTableContainedFieldName4()
		{
			const bool mustContainFieldName = true;
			const string prefix = "";
			Assert.AreEqual(0, GetErrors("GEO_00100436001", 30, ExpectedCase.Any,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void InvalidTableContainedFieldName2()
		{
			const bool mustContainFieldName = true;
			const string prefix = "";
			IList<QaError> errors = GetErrors("GEO_00100633001", 31, ExpectedCase.Any,
			                                  mustContainFieldName, prefix);
			Assert.AreEqual(1, errors.Count);

			Assert.AreEqual(
				"Domain name 'SRW_BERGTAL' does not contain the field name 'BERG_TAL'",
				errors[0].Description);
		}

		[Test]
		public void InvalidTableMaxLength()
		{
			const bool mustContainFieldName = false;
			const string prefix = "";
			IList<QaError> errors = GetErrors("GEO_00100633001", 30, ExpectedCase.Any,
			                                  mustContainFieldName, prefix);
			Assert.AreEqual(1, errors.Count);

			Assert.AreEqual(
				"Domain 'SRW_ZUGEHOERIGKEITEN_STREUROUTE': 'Length of name is greater than maximum length 30: 31'",
				errors[0].Description);
		}

		[Test]
		public void InvalidTablePrefix1()
		{
			const bool mustContainFieldName = false;
			const string prefix = "ZON_";
			IList<QaError> errors = GetErrors("GEO_00100004001", 30, ExpectedCase.AllUpper,
			                                  mustContainFieldName, prefix);

			Assert.AreEqual(1, errors.Count);
			Assert.AreEqual(
				"Domain name 'ZONENTYP' does not start with prefix 'ZON_'",
				errors[0].Description);
		}

		[Test]
		public void ValidTablePrefix1()
		{
			const bool mustContainFieldName = false;
			const string prefix = "GEF_";
			Assert.AreEqual(0, GetErrors("GEO_00100024004", 30, ExpectedCase.AllUpper,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void ValidTablePrefix2()
		{
			const bool mustContainFieldName = false;
			const string prefix = "MST_";
			Assert.AreEqual(0, GetErrors("GEO_00100059002", 30, ExpectedCase.AllUpper,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void ValidTablePrefix3()
		{
			const bool mustContainFieldName = false;
			const string prefix = "HASNODOMAINS_DOESNOTMATTER_";
			Assert.AreEqual(0, GetErrors("GEO_00100436001", 30, ExpectedCase.AllUpper,
			                             mustContainFieldName, prefix).Count);
		}

		[Test]
		public void InvalidTablePrefix2()
		{
			// not sure what the correct prefix would be - maybe it's more than one?
			// in this case the general naming pattern can be checked with QaSchemaFieldDomainNamesRegex
			const bool mustContainFieldName = false;
			const string prefix = "ZON_";
			IList<QaError> errors = GetErrors("GEO_00100510001", 30, ExpectedCase.AllUpper,
			                                  mustContainFieldName, prefix);

			Assert.AreEqual(11, errors.Count);
		}

		[Test]
		public void ValidTablePrefix4()
		{
			const bool mustContainFieldName = false;
			const string prefix = "SRW_";
			Assert.AreEqual(0, GetErrors("GEO_00100633001", 31, ExpectedCase.AllUpper,
			                             mustContainFieldName, prefix).Count);
		}

		#endregion

		[NotNull]
		private static IList<QaError> GetErrors([NotNull] string tableName,
		                                        int maximumLength,
		                                        ExpectedCase expectedCase,
		                                        bool mustContainFieldName,
		                                        [CanBeNull] string expectedPrefix)
		{
			string path = TestDataPreparer.ExtractZip("QaSchemaTests.gdb.zip")
			                              .GetPath();

			IFeatureWorkspace workspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			var test = new QaSchemaFieldDomainNames(
				ReadOnlyTableFactory.Create(table), expectedPrefix, maximumLength,
				mustContainFieldName, expectedCase);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}

		private static ICodedValueDomain CreateCVDomain(string name)
		{
			return DomainUtils.CreateCodedValueDomain(name,
			                                          esriFieldType.esriFieldTypeInteger,
			                                          new CodedValue(1, "Value 1"),
			                                          new CodedValue(2, "Value 2"));
		}

		private static IRangeDomain CreateRangeDomain(string name)
		{
			return DomainUtils.CreateRangeDomain(name,
			                                     esriFieldType.esriFieldTypeInteger,
			                                     0, 100);
		}

		private IFeatureWorkspace TestWorkspace
			=> _workspace ??
			   (_workspace = TestWorkspaceUtils.CreateTestFgdbWorkspace(GetType().Name));
	}
}
