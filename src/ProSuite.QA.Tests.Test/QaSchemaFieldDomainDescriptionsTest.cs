using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaFieldDomainDescriptionsTest
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
				                                        "Description of DOM_FIELD1",
				                                        new CodedValue(1, "Value 1"),
				                                        new CodedValue(2, "Value 2")));
			IDomain domain2 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD2",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        "Description of DOM_FIELD2",
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

			const int maxLength = 25;
			const bool noDuplicates = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainDescriptions(
					ReadOnlyTableFactory.Create(table), maxLength,
					noDuplicates,
					null));

			runner.Execute();

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void TooLong()
		{
			IDomain domain1 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD1",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        "Description of DOM_FIELD1_toolong",
				                                        new CodedValue(1, "Value 1"),
				                                        new CodedValue(2, "Value 2")));
			IDomain domain2 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD2",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        "Description of DOM_FIELD2",
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

			const int maxLength = 25;
			const bool noDuplicates = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainDescriptions(
					ReadOnlyTableFactory.Create(table), maxLength, noDuplicates, null));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void Duplicates()
		{
			IDomain domain1 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD1",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        "Description of DOM_FIELD1",
				                                        new CodedValue(1, "Value 1"),
				                                        new CodedValue(2, "Value 2")));
			IDomain domain2 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD2",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        "Description of DOM_FIELD2",
				                                        new CodedValue(1, "Value 1"),
				                                        new CodedValue(2, "Value 2")));

			// domain (not used in table) with duplicate name
			DomainUtils.AddDomain(_workspace,
			                      DomainUtils.CreateCodedValueDomain("DOM_FIELD3",
				                      esriFieldType
					                      .esriFieldTypeInteger,
				                      "Description of DOM_FIELD2",
				                      new CodedValue(1,
				                                     "Value 1"),
				                      new CodedValue(2,
				                                     "Value 2")));

			DomainUtils.AddDomain(_workspace,
			                      DomainUtils.CreateRangeDomain("DOM_FIELD4",
			                                                    esriFieldType.esriFieldTypeInteger,
			                                                    0, 100,
			                                                    "Description of DOM_FIELD2"));

			IField field1 = FieldUtils.CreateField("FIELD1", esriFieldType.esriFieldTypeInteger);
			IField field2 = FieldUtils.CreateField("FIELD2", esriFieldType.esriFieldTypeInteger);
			IField field3 = FieldUtils.CreateTextField("FIELD3", 20);

			((IFieldEdit) field1).Domain_2 = domain1;
			((IFieldEdit) field2).Domain_2 = domain2;

			ITable table = DatasetUtils.CreateTable(_workspace,
			                                        MethodBase.GetCurrentMethod().Name,
			                                        FieldUtils.CreateOIDField(),
			                                        field1, field2, field3);

			const int maxLength = 25;
			const bool noDuplicates = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainDescriptions(
					ReadOnlyTableFactory.Create(table), maxLength, noDuplicates, null));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void DuplicatesInOtherWorkspace()
		{
			IDomain domain1 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD1",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        "Description of DOM_FIELD1",
				                                        new CodedValue(1, "Value 1"),
				                                        new CodedValue(2, "Value 2")));
			IDomain domain2 = DomainUtils.AddDomain(_workspace,
			                                        DomainUtils.CreateCodedValueDomain(
				                                        "DOM_FIELD2",
				                                        esriFieldType.esriFieldTypeInteger,
				                                        "Description of DOM_FIELD2",
				                                        new CodedValue(1, "Value 1"),
				                                        new CodedValue(2, "Value 2")));

			// domain (not used in table) with duplicate name - this should not be reported since duplicates are searched in target (?)
			DomainUtils.AddDomain(_workspace,
			                      DomainUtils.CreateCodedValueDomain("DOM_FIELD3",
				                      esriFieldType
					                      .esriFieldTypeInteger,
				                      "Description of DOM_FIELD2",
				                      new CodedValue(1,
				                                     "Value 1"),
				                      new CodedValue(2,
				                                     "Value 2")));

			IField field1 = FieldUtils.CreateField("FIELD1", esriFieldType.esriFieldTypeInteger);
			IField field2 = FieldUtils.CreateField("FIELD2", esriFieldType.esriFieldTypeInteger);
			IField field3 = FieldUtils.CreateTextField("FIELD3", 20);

			((IFieldEdit) field1).Domain_2 = domain1;
			((IFieldEdit) field2).Domain_2 = domain2;

			ITable table = DatasetUtils.CreateTable(_workspace,
			                                        MethodBase.GetCurrentMethod().Name,
			                                        FieldUtils.CreateOIDField(),
			                                        field1, field2, field3);

			// add domains/table to target workspace

			IFeatureWorkspace targetWorkspace =
				TestWorkspaceUtils.CreateTestFgdbWorkspace($"{GetType().Name}_target");

			// same name, same description --> should be considered equal, no duplicate
			DomainUtils.AddDomain(targetWorkspace,
			                      DomainUtils.CreateCodedValueDomain("DOM_FIELD1",
				                      esriFieldType
					                      .esriFieldTypeInteger,
				                      "Description of DOM_FIELD1",
				                      new CodedValue(1,
				                                     "Value 1"),
				                      new CodedValue(2,
				                                     "Value 2")));

			// different name, same description --> should be reported
			DomainUtils.AddDomain(targetWorkspace,
			                      DomainUtils.CreateCodedValueDomain("DOM_FIELD4",
				                      esriFieldType
					                      .esriFieldTypeInteger,
				                      "Description of DOM_FIELD2",
				                      new CodedValue(1,
				                                     "Value 1"),
				                      new CodedValue(2,
				                                     "Value 2")));

			// different name, same description --> should be reported
			DomainUtils.AddDomain(targetWorkspace,
			                      DomainUtils.CreateRangeDomain("DOM_FIELD5",
			                                                    esriFieldType.esriFieldTypeInteger,
			                                                    0, 100,
			                                                    "Description of DOM_FIELD2"));

			ITable targetTable = DatasetUtils.CreateTable(targetWorkspace,
			                                              MethodBase.GetCurrentMethod().Name,
			                                              FieldUtils.CreateOIDField(),
			                                              FieldUtils.CreateTextField("FIELD1",
				                                              10));

			const int maxLength = 25;
			const bool noDuplicates = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldDomainDescriptions(
					ReadOnlyTableFactory.Create(table), maxLength, noDuplicates,
					ReadOnlyTableFactory.Create(targetTable)));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		#region Tests based on QaSchemaTests.mdb

		[Test]
		public void InvalidTableNonUnique()
		{
			IList<QaError> errors = GetErrors("ovartis", 40, true);

			Assert.AreEqual(1, errors.Count);

			Assert.AreEqual(
				"Domain description 'Art des öffentlichen Verkehrsmittels' is not unique. The following domains have the same description: OVA_LINTYP, OVA_TYP",
				errors[0].Description);
		}

		[Test]
		public void InvalidTable1()
		{
			IList<QaError> errors = GetErrors("GEO_00100004001", 30, true);

			Assert.AreEqual(2, errors.Count);

			Assert.AreEqual(
				"Domain 'ZONENTYP': Length of description is greater than maximum length 30: 39 ('Zonentypen des  aggregierten Zonenplans')",
				errors[0].Description);
			Assert.AreEqual(
				"Domain 'ZON_ESTUFE': Length of description is greater than maximum length 30: 53 ('Lärmempfindlichkeitsstufe Zonenplan (für AfU), Nov 05')",
				errors[1].Description);
		}

		[Test]
		public void InvalidTable2()
		{
			IList<QaError> errors = GetErrors("GEO_00100024004", 30, true);

			Assert.AreEqual(1, errors.Count);

			Assert.AreEqual(
				"Domain 'GEF_MASSNAHME3': Length of description is greater than maximum length 30: 31 ('Pflegemassnahme Flächen Gehölze')",
				errors[0].Description);
		}

		[Test]
		public void ValidTable1()
		{
			NoErrors(GetErrors("GEO_00100059002", 30, true));
		}

		[Test]
		public void ValidTable2()
		{
			NoErrors(GetErrors("GEO_00100436001", 30, true));
		}

		[Test]
		public void InvalidTable3()
		{
			IList<QaError> errors = GetErrors("GEO_00100510001", 30, true);

			Assert.AreEqual(4, errors.Count);

			Assert.AreEqual(
				"Domain 'A_GZB_BEZZUS': Length of description is greater than maximum length 30: 36 ('Zuströmbereiche: Bezeichnungs-Zusatz')",
				errors[0].Description);
			Assert.AreEqual(
				"Domain 'A_GZB_BEZANG': Length of description is greater than maximum length 30: 37 ('Zuströmbereiche: Bezeichnungs-Angaben')",
				errors[1].Description);
			Assert.AreEqual(
				"Domain 'A_GWS_NITRAT': Length of description is greater than maximum length 30: 36 ('SZ, SA, Zu: Hinweis auf Nitratgebiet')",
				errors[2].Description);
			Assert.AreEqual(
				"Domain 'A_ERF_VORL': Length of description is greater than maximum length 30: 50 ('Allg.: Hinweis auf vorhandene SZ-Erfassungsvorlage')",
				errors[3].Description);
		}

		[Test]
		public void ValidTable3()
		{
			NoErrors(GetErrors("GEO_00100633001", 30, true));
		}

		private static void NoErrors([NotNull] ICollection<QaError> errors)
		{
			Assert.AreEqual(0, errors.Count);
		}

		[NotNull]
		private static IList<QaError> GetErrors([NotNull] string tableName,
		                                        int maximumLength,
		                                        bool noDuplicateDescriptions)
		{
			var locator = TestDataUtils.GetTestDataLocator();
			string path = locator.GetPath("QaSchemaTests.mdb");

			IFeatureWorkspace workspace = WorkspaceUtils.OpenPgdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			var test = new QaSchemaFieldDomainDescriptions(
				ReadOnlyTableFactory.Create(table), maximumLength,
				noDuplicateDescriptions, null);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}

		#endregion
	}
}
