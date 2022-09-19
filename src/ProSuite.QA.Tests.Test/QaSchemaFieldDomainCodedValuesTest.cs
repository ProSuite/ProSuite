using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Test.TestData;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaFieldDomainCodedValuesTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		private IFeatureWorkspace _workspace;

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

		[TearDown]
		public void TearDown()
		{
			_workspace = null;

			GC.Collect();
			GC.WaitForPendingFinalizers();
			// important, otherwise locks are still there in next test
		}

		[SetUp]
		public void Setup()
		{
			_workspace = TestWorkspaceUtils.CreateTestFgdbWorkspace(GetType().Name);
		}

		[Test]
		public void AllValid()
		{
			IDomain domain1 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD1",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        new CodedValue(1, "Value 1"),
				                                        new CodedValue(2, "Value 2")));
			IDomain domain2 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD2",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        new CodedValue(1, "Value 1"),
				                                        new CodedValue(2, "Value 2")));

			IField field1 = FieldUtils.CreateField("FIELD1", esriFieldType.esriFieldTypeInteger);
			IField field2 = FieldUtils.CreateField("FIELD2", esriFieldType.esriFieldTypeInteger);
			IField field3 = FieldUtils.CreateTextField("FIELD3", 20);

			((IFieldEdit) field1).Domain_2 = domain1;
			((IFieldEdit) field2).Domain_2 = domain2;

			ITable table = DatasetUtils.CreateTable(_workspace,
			                                        MethodBase.GetCurrentMethod().Name,
			                                        FieldUtils.CreateOIDField(),
			                                        field1, field2, field3);

			const int maxLength = 10;
			const int minimumValueCount = 1;
			const int minimumNonEqualNameValueCount = 1;
			const bool allowEmptyName = false;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainCodedValues(
					ReadOnlyTableFactory.Create(table), maxLength,
					UniqueStringsConstraint.UniqueAnyCase,
					minimumValueCount, minimumNonEqualNameValueCount,
					allowEmptyName));

			runner.Execute();

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void NameTooLong()
		{
			IDomain domain1 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD1",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        new CodedValue(1, "Value 1 abc"),
				                                        new CodedValue(2, "Value 2")));
			IDomain domain2 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD2",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        new CodedValue(1, "Value 1"),
				                                        new CodedValue(2, "Value 2 123")));

			IField field1 = FieldUtils.CreateField("FIELD1", esriFieldType.esriFieldTypeInteger);
			IField field2 = FieldUtils.CreateField("FIELD2", esriFieldType.esriFieldTypeInteger);
			IField field3 = FieldUtils.CreateField("FIELD3", esriFieldType.esriFieldTypeInteger);

			((IFieldEdit) field1).Domain_2 = domain1;
			((IFieldEdit) field2).Domain_2 = domain2;
			((IFieldEdit) field3).Domain_2 = domain2;
			// reuse domain2 - to test if error is reported only once

			ITable table = DatasetUtils.CreateTable(_workspace,
			                                        MethodBase.GetCurrentMethod().Name,
			                                        FieldUtils.CreateOIDField(),
			                                        field1, field2, field3);

			const int maxLength = 10;
			const int minimumValueCount = 1;
			const int minimumNonEqualNameValueCount = 1;
			const bool allowEmptyName = false;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainCodedValues(
					ReadOnlyTableFactory.Create(table), maxLength,
					UniqueStringsConstraint.UniqueAnyCase,
					minimumValueCount, minimumNonEqualNameValueCount,
					allowEmptyName));

			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void MissingName()
		{
			IDomain domain1 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD1",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        new CodedValue(1, " "),
				                                        new CodedValue(2, "Value 2")));
			IDomain domain2 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD2",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        new CodedValue(1, "Value 1"),
				                                        new CodedValue(2, " ")));

			IField field1 = FieldUtils.CreateField("FIELD1", esriFieldType.esriFieldTypeInteger);
			IField field2 = FieldUtils.CreateField("FIELD2", esriFieldType.esriFieldTypeInteger);
			IField field3 = FieldUtils.CreateField("FIELD3", esriFieldType.esriFieldTypeInteger);

			((IFieldEdit) field1).Domain_2 = domain1;
			((IFieldEdit) field2).Domain_2 = domain2;
			((IFieldEdit) field3).Domain_2 = domain2;
			// reuse domain2 - to test if error is reported only once

			ITable table = DatasetUtils.CreateTable(_workspace,
			                                        MethodBase.GetCurrentMethod().Name,
			                                        FieldUtils.CreateOIDField(),
			                                        field1, field2, field3);

			const int maxLength = 10;
			const int minimumValueCount = 1;
			const int minimumNonEqualNameValueCount = 1;
			const bool allowEmptyName = false;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainCodedValues(
					ReadOnlyTableFactory.Create(table), maxLength,
					UniqueStringsConstraint.UniqueAnyCase,
					minimumValueCount, minimumNonEqualNameValueCount,
					allowEmptyName));

			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		#region Tests based on QaSchemaTests.mdb

		[Test]
		public void ValidTable1()
		{
			NoErrors(GetErrors("GEO_00100004001", 100));
		}

		[Test]
		public void InvalidTable1()
		{
			IList<QaError> errors = GetErrors("GEO_00100024004", 100);

			Assert.AreEqual(2, errors.Count);
			Assert.AreEqual(
				"Value [1] in coded value domain 'GEF_MASSNAHME3': Length of name is greater than maximum length 100: 106 " +
				"('Altern. 1 Teilbereich (a,b,c) der Gehölze auslichten, insbesondere raschwüchs. Gehölze; Ghdeckgrad. 10-25%')",
				errors[0].Description);
			Assert.AreEqual(
				"Value [3] in coded value domain 'GEF_MASSNAHME3': Length of name is greater than maximum length 100: 108 " +
				"('Standortger. Bewirtschaftung, alte + abgestorbene Bäume vermehrt stehen lassen, notfalls Baumkrone entfernen')",
				errors[1].Description);
		}

		[Test]
		public void ValidTable2()
		{
			NoErrors(GetErrors("GEO_00100059002", 100));
		}

		[Test]
		public void ValidTable3()
		{
			NoErrors(GetErrors("GEO_00100436001", 100));
		}

		[Test]
		public void InvalidTable3()
		{
			IList<QaError> errors = GetErrors("GEO_00100510001", 100);

			Assert.AreEqual(3, errors.Count);

			Assert.AreEqual(
				"Domain 'A_GZB_BEZANG' has 0 coded values with a name that is different from the value. Minimum: 1",
				errors[0].Description);
			Assert.AreEqual(
				"Domain 'A_GWS_NITRAT' has 0 coded values with a name that is different from the value. Minimum: 1",
				errors[1].Description);
			Assert.AreEqual(
				"Domain 'A_VORL_MSTAB' has 0 coded values with a name that is different from the value. Minimum: 1",
				errors[2].Description);
		}

		[Test]
		public void ValidTable5()
		{
			NoErrors(GetErrors("GEO_00100633001", 100));
		}

		[Test]
		public void InvalidTable2()
		{
			IList<QaError> errors = GetErrors("ovaktlu1_li", 100);

			Assert.AreEqual(1, errors.Count);
			Assert.AreEqual(
				"Name 'Bahn' in coded value domain 'OVA_LINTYP' is non-unique. The following values have the same name: 1, 4",
				errors[0].Description);
		}

		private static void NoErrors([NotNull] ICollection<QaError> errors)
		{
			Assert.AreEqual(0, errors.Count);
		}

		[NotNull]
		private static IList<QaError> GetErrors([NotNull] string tableName,
		                                        int maximumLength)
		{
			var locator = TestDataUtils.GetTestDataLocator();
			string path = locator.GetPath("QaSchemaTests.mdb");

			IFeatureWorkspace workspace = WorkspaceUtils.OpenPgdbFeatureWorkspace(path);

			const int minimumValueCount = 1;
			const int minimumNonEqualNameValueCount = 1;
			const bool allowEmptyName = false;

			ITable table = workspace.OpenTable(tableName);
			var test = new QaSchemaFieldDomainCodedValues(
				ReadOnlyTableFactory.Create(table), maximumLength,
				UniqueStringsConstraint.UniqueAnyCase,
				minimumValueCount,
				minimumNonEqualNameValueCount,
				allowEmptyName);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}

		#endregion
	}
}
