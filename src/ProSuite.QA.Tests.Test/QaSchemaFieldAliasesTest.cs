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
	public class QaSchemaFieldAliasesTest
	{
		private IFeatureWorkspace _workspace;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_workspace = TestWorkspaceUtils.CreateTestFgdbWorkspace(GetType().Name);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void AllValid()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField("OBJECTID", "Object ID"),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD2", 10, "Field 2"),
				FieldUtils.CreateTextField("FIELD3", 10, "Field 3"));

			const bool requireUniqueAliasNames = true;
			const bool allowCustomSystemFieldAlias = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldAliases(
					ReadOnlyTableFactory.Create(table), 30, ExpectedCase.Mixed,
					requireUniqueAliasNames, allowCustomSystemFieldAlias));
			runner.Execute();

			NoErrors(runner.Errors);
		}

		[Test]
		public void NonUnique()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField("OBJECTID", "Object ID"),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD2", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD3", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD4", 10, "Field 3"));

			const bool requireUniqueAliasNames = true;
			const bool allowCustomSystemFieldAlias = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldAliases(
					ReadOnlyTableFactory.Create(table), 30, ExpectedCase.Mixed,
					requireUniqueAliasNames, allowCustomSystemFieldAlias,
					ExpectedStringDifference.CaseInsensitiveDifference));
			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void Missing()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField("OBJECTID", "Object ID"),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD2", 10, "  "),
				FieldUtils.CreateTextField("FIELD3", 10, "Field 3"),
				FieldUtils.CreateTextField("FIELD4", 10, " "));

			const bool requireUniqueAliasNames = true;
			const bool allowCustomSystemFieldAlias = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldAliases(
					ReadOnlyTableFactory.Create(table), 30, ExpectedCase.Mixed,
					requireUniqueAliasNames, allowCustomSystemFieldAlias,
					ExpectedStringDifference.CaseInsensitiveDifference));
			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedAllUpper()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField("OBJECTID", "Object ID"),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD2", 10, "FIELD 2"),
				FieldUtils.CreateTextField("FIELD3", 10, "field 3"));

			const bool requireUniqueAliasNames = true;
			const bool allowCustomSystemFieldAlias = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldAliases(
					ReadOnlyTableFactory.Create(table), 30, ExpectedCase.AllUpper,
					requireUniqueAliasNames, allowCustomSystemFieldAlias));
			runner.Execute();

			Assert.AreEqual(3, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedMixed()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField("OBJECTID", "Object ID"),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD2", 10, "FIELD 2"),
				FieldUtils.CreateTextField("FIELD3", 10, "field 3"));

			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldAliases(
					ReadOnlyTableFactory.Create(table), 30, ExpectedCase.Mixed,
					requireUnique, allowCustomSystemFieldAlias,
					ExpectedStringDifference.CaseInsensitiveDifference));
			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedNotAllLower()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField("OBJECTID", "Object ID"),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD2", 10, "FIELD 2"),
				FieldUtils.CreateTextField("FIELD3", 10, "field 3"));

			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldAliases(
					ReadOnlyTableFactory.Create(table), 30, ExpectedCase.NotAllLower,
					requireUnique, allowCustomSystemFieldAlias));
			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedNotAllUpper()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField("OBJECTID", "Object ID"),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD2", 10, "FIELD 2"),
				FieldUtils.CreateTextField("FIELD3", 10, "field 3"));

			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldAliases(
					ReadOnlyTableFactory.Create(table), 30, ExpectedCase.NotAllUpper,
					requireUnique, allowCustomSystemFieldAlias,
					ExpectedStringDifference.CaseInsensitiveDifference));
			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedAllLower()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField("OBJECTID", "Object ID"),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD2", 10, "FIELD 2"),
				FieldUtils.CreateTextField("FIELD3", 10, "field 3"));

			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldAliases(
					ReadOnlyTableFactory.Create(table), 30, ExpectedCase.AllLower,
					requireUnique, allowCustomSystemFieldAlias));
			runner.Execute();

			Assert.AreEqual(3, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedAny()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField("OBJECTID", "Object ID"),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"),
				FieldUtils.CreateTextField("FIELD2", 10, "FIELD 2"),
				FieldUtils.CreateTextField("FIELD3", 10, "field 3"));

			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = true;
			var runner = new QaTestRunner(
				new QaSchemaFieldAliases(
					ReadOnlyTableFactory.Create(table), 30, ExpectedCase.Any,
					requireUnique, allowCustomSystemFieldAlias));
			runner.Execute();

			NoErrors(runner.Errors);
		}

		#region Tests based on QaSchemaTests.mdb

		[Test]
		public void InvalidTableMaxLength()
		{
			const bool requireUnique = false;
			const bool allowCustomSystemFieldAlias = false;
			IList<QaError> errors = GetErrors("GEO_00100510001", 30, ExpectedCase.Any,
			                                  requireUnique, allowCustomSystemFieldAlias,
			                                  ExpectedStringDifference.Any);

			Assert.AreEqual(4, errors.Count);

			Assert.AreEqual(
				"Field alias name 'Gültig für GW-Schutzzone / -areal' (field 'SZ_SA_NUMMER'): " +
				"Length of alias name is greater than maximum length 30: 33",
				errors[0].Description);

			Assert.AreEqual(
				"Field alias name 'Zuordnung zu Gemeinde (Nr_ Kanton)' (field 'ZRD_GDE_KTNR'): " +
				"Length of alias name is greater than maximum length 30: 34",
				errors[1].Description);

			Assert.AreEqual(
				"Field alias name 'Zuordnung zu Gemeinde (Nr_ BfS)' (field 'ZRD_GDE_BFSNR'): " +
				"Length of alias name is greater than maximum length 30: 31",
				errors[2].Description);

			Assert.AreEqual(
				"Field alias name 'Detailplan als Erfassungsvorlage' (field 'ERFASSUNG_VORLAGE'): " +
				"Length of alias name is greater than maximum length 30: 32",
				errors[3].Description);
		}

		[Test]
		public void InvalidTableCaseSensitiveDifference()
		{
			const bool requireUnique = false;
			const bool allowCustomSystemFieldAlias = false;
			IList<QaError> errors = GetErrors("GEO_00100510001", -1, ExpectedCase.Any,
			                                  requireUnique, allowCustomSystemFieldAlias,
			                                  ExpectedStringDifference.CaseSensitiveDifference);

			Assert.AreEqual(3, errors.Count);

			Assert.AreEqual(
				"Alias name 'OBJEKTNAME' must be different from field name 'OBJEKTNAME'",
				errors[0].Description);

			Assert.AreEqual(
				"Alias name 'UWE_REF_NR' must be different from field name 'UWE_REF_NR'",
				errors[1].Description);

			Assert.AreEqual(
				"Alias name 'UUID' must be different from field name 'UUID'",
				errors[2].Description);
		}

		[Test]
		public void InvalidTableCaseInsensitiveDifference()
		{
			const bool requireUnique = false;
			const bool allowCustomSystemFieldAlias = false;
			IList<QaError> errors = GetErrors("GEO_00100510001", -1, ExpectedCase.Any,
			                                  requireUnique, allowCustomSystemFieldAlias,
			                                  ExpectedStringDifference.CaseInsensitiveDifference);

			Assert.AreEqual(6, errors.Count);

			Assert.AreEqual(
				"Alias name 'Objektart' must not differ only in character case from field name 'OBJEKTART'",
				errors[0].Description);

			Assert.AreEqual(
				"Alias name 'OBJEKTNAME' must be different from field name 'OBJEKTNAME'",
				errors[1].Description);

			Assert.AreEqual(
				"Alias name 'Bezeichnung' must not differ only in character case from field name 'BEZEICHNUNG'",
				errors[2].Description);

			Assert.AreEqual(
				"Alias name 'UWE_REF_NR' must be different from field name 'UWE_REF_NR'",
				errors[3].Description);

			Assert.AreEqual(
				"Alias name 'Nitratgebiet' must not differ only in character case from field name 'nitratgebiet'",
				errors[4].Description);

			Assert.AreEqual(
				"Alias name 'UUID' must be different from field name 'UUID'",
				errors[5].Description);
		}

		[Test]
		public void InvalidTableFieldCase()
		{
			const bool requireUnique = false;
			const bool allowCustomSystemFieldAlias = false;
			IList<QaError> errors = GetErrors("GEO_00100510001", -1, ExpectedCase.Mixed,
			                                  requireUnique, allowCustomSystemFieldAlias,
			                                  ExpectedStringDifference.Any);

			Assert.AreEqual(3, errors.Count);

			Assert.AreEqual(
				"Field alias name 'OBJEKTNAME' (field 'OBJEKTNAME'): Alias Name must be mixed case: OBJEKTNAME",
				errors[0].Description);

			Assert.AreEqual(
				"Field alias name 'UWE_REF_NR' (field 'UWE_REF_NR'): Alias Name must be mixed case: UWE_REF_NR",
				errors[1].Description);

			Assert.AreEqual(
				"Field alias name 'UUID' (field 'UUID'): Alias Name must be mixed case: UUID",
				errors[2].Description);
		}

		[Test]
		public void InvalidTableNonUnique()
		{
			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = false;
			IList<QaError> errors = GetErrors("GEO_00100510001", -1, ExpectedCase.Any,
			                                  requireUnique, allowCustomSystemFieldAlias,
			                                  ExpectedStringDifference.Any);

			Assert.AreEqual(1, errors.Count);

			Assert.AreEqual(
				"Alias name 'Bezeichnung' in table 'GEO_00100510001' is not unique. The following fields have the same alias name: BEZEICHNUNG, BEZEICHNUNG_BEMERK",
				errors[0].Description);
		}

		[Test]
		public void ValidShapeAliasDifferentCase()
		{
			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = false;
			IList<QaError> errors = GetErrors("GEO_00100024004", 30, ExpectedCase.Mixed,
			                                  requireUnique, allowCustomSystemFieldAlias,
			                                  ExpectedStringDifference.CaseSensitiveDifference);

			Assert.AreEqual(0, errors.Count);

			//Assert.AreEqual(
			//    "Alias name 'Shape' must be equal to field name for system field 'SHAPE'",
			//    errors[0].Description);
		}

		[Test]
		public void ValidTable1()
		{
			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = false;
			NoErrors(GetErrors("GEO_00100004001", 30, ExpectedCase.Mixed,
			                   requireUnique, allowCustomSystemFieldAlias,
			                   ExpectedStringDifference.CaseSensitiveDifference));
		}

		[Test]
		public void ValidTable2()
		{
			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = false;
			NoErrors(GetErrors("GEO_00100059002", 30, ExpectedCase.Mixed,
			                   requireUnique, allowCustomSystemFieldAlias,
			                   ExpectedStringDifference.CaseSensitiveDifference));
		}

		[Test]
		public void ValidTable3()
		{
			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = false;
			NoErrors(GetErrors("GEO_00100436001", 30, ExpectedCase.Mixed,
			                   requireUnique, allowCustomSystemFieldAlias,
			                   ExpectedStringDifference.CaseSensitiveDifference));
		}

		[Test]
		public void ValidTable4()
		{
			const bool requireUnique = true;
			const bool allowCustomSystemFieldAlias = false;
			NoErrors(GetErrors("GEO_00100633001", 30, ExpectedCase.Mixed,
			                   requireUnique, allowCustomSystemFieldAlias,
			                   ExpectedStringDifference.CaseSensitiveDifference));
		}

		#endregion

		private static void NoErrors([NotNull] ICollection<QaError> errors)
		{
			Assert.AreEqual(0, errors.Count);
		}

		[NotNull]
		private static IList<QaError> GetErrors([NotNull] string tableName,
		                                        int maximumLength,
		                                        ExpectedCase expectedCase,
		                                        bool requireUnique,
		                                        bool allowCustomSystemFieldAlias,
		                                        ExpectedStringDifference
			                                        expectedStringDifference)
		{
			string path = TestDataPreparer.ExtractZip("QaSchemaTests.gdb.zip")
			                              .GetPath();

			IFeatureWorkspace workspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			var test = new QaSchemaFieldAliases(
				ReadOnlyTableFactory.Create(table), maximumLength, expectedCase,
				requireUnique, allowCustomSystemFieldAlias,
				expectedStringDifference);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}
	}
}
