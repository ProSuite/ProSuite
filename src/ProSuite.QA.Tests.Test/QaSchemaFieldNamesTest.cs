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
	public class QaSchemaFieldNamesTest
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
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10),
				FieldUtils.CreateTextField("FIELD2", 10),
				FieldUtils.CreateTextField("FIELD3", 10),
				FieldUtils.CreateTextField("FIELD_MAXLENGTH", 10));

			const int maxLength = 15;
			const int uniqueLength = 6;
			var runner = new QaTestRunner(
				new QaSchemaFieldNames(
					ReadOnlyTableFactory.Create(table), maxLength, ExpectedCase.AllUpper,
					uniqueLength));

			runner.Execute();

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedAllUpper()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10),
				FieldUtils.CreateTextField("Field2", 10),
				FieldUtils.CreateTextField("field3", 10));

			const int maxLength = 15;
			const int uniqueLength = 6;
			var runner = new QaTestRunner(
				new QaSchemaFieldNames(
					ReadOnlyTableFactory.Create(table), maxLength, ExpectedCase.AllUpper,
					uniqueLength));

			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedMixed()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10),
				FieldUtils.CreateTextField("field2", 10),
				FieldUtils.CreateTextField("Field3", 10));

			const int maxLength = 15;
			const int uniqueLength = 6;
			var runner = new QaTestRunner(
				new QaSchemaFieldNames(
					ReadOnlyTableFactory.Create(table), maxLength, ExpectedCase.Mixed,
					uniqueLength));

			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedNotAllLower()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10),
				FieldUtils.CreateTextField("field2", 10),
				FieldUtils.CreateTextField("Field3", 10));

			const int maxLength = 15;
			const int uniqueLength = 6;
			var runner = new QaTestRunner(
				new QaSchemaFieldNames(
					ReadOnlyTableFactory.Create(table), maxLength, ExpectedCase.NotAllLower,
					uniqueLength));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedAny()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10),
				FieldUtils.CreateTextField("field2", 10),
				FieldUtils.CreateTextField("Field3", 10));

			const int maxLength = 15;
			const int uniqueLength = 6;

			var runner = new QaTestRunner(
				new QaSchemaFieldNames(
					ReadOnlyTableFactory.Create(table), maxLength, ExpectedCase.Any, uniqueLength));
			runner.Execute();

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedNotAllUpper()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10),
				FieldUtils.CreateTextField("field2", 10),
				FieldUtils.CreateTextField("Field3", 10));

			const int maxLength = 15;
			const int uniqueLength = 6;
			var runner = new QaTestRunner(
				new QaSchemaFieldNames(
					ReadOnlyTableFactory.Create(table), maxLength, ExpectedCase.NotAllUpper,
					uniqueLength));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void InvalidCaseExpectedAllLower()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10),
				FieldUtils.CreateTextField("field2", 10),
				FieldUtils.CreateTextField("Field3", 10));

			const int maxLength = 15;
			const int uniqueLength = 6;

			var runner = new QaTestRunner(
				new QaSchemaFieldNames(
					ReadOnlyTableFactory.Create(table), maxLength, ExpectedCase.AllLower,
					uniqueLength));
			runner.Execute();

			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void TooLong()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10),
				FieldUtils.CreateTextField("FIELD2_1234", 10),
				FieldUtils.CreateTextField("FIELD3", 10));

			const int maxLength = 10;
			const int uniqueLength = 6;
			var runner = new QaTestRunner(
				new QaSchemaFieldNames(
					ReadOnlyTableFactory.Create(table), maxLength, ExpectedCase.AllUpper,
					uniqueLength));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void NonUnique()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD11", 10),
				FieldUtils.CreateTextField("FIELD12", 10),
				FieldUtils.CreateTextField("FIELD13", 10),
				FieldUtils.CreateTextField("FIELD4", 10));

			const int maxLength = 15;
			const int uniqueLength = 6;
			var runner = new QaTestRunner(
				new QaSchemaFieldNames(
					ReadOnlyTableFactory.Create(table), maxLength, ExpectedCase.AllUpper,
					uniqueLength));

			runner.Execute();

			// all non-unique fields are reported in one error
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void InvalidTableMaxLength()
		{
			IList<QaError> errors = GetErrors("GEO_00100633001", 30, ExpectedCase.AllUpper, 10);

			Assert.AreEqual(1, errors.Count);

			Assert.AreEqual(
				"Field 'STREUROUTENREFERENZNUMMERIERUNG': Length of name is greater than maximum length 30: 31",
				errors[0].Description);
		}

		[Test]
		public void InvalidTableFieldCase()
		{
			IList<QaError> errors = GetErrors("GEO_00100510001", 30, ExpectedCase.AllUpper, -1);

			Assert.AreEqual(2, errors.Count);

			Assert.AreEqual(
				"Field 'nitratgebiet': Name must be all uppercase: nitratgebiet",
				errors[0].Description);
			Assert.AreEqual(
				"Field 'BEMERK_Allgemein': Name must be all uppercase: BEMERK_Allgemein",
				errors[1].Description);
		}

		[Test]
		public void InvalidTableUniqueSubstringLength()
		{
			IList<QaError> errors = GetErrors("GEO_00100510001", 30, ExpectedCase.Any, 10);

			Assert.AreEqual(1, errors.Count);

			Assert.AreEqual(
				"The following field names are not unique on the first 10 characters: " +
				"BEZEICHNUNG, BEZEICHNUNG_ZUSATZ, BEZEICHNUNG_ANGABEN, BEZEICHNUNG_BEMERK",
				errors[0].Description);
		}

		[Test]
		public void ValidTable1()
		{
			Assert.AreEqual(0,
			                GetErrors("GEO_00100004001", 30, ExpectedCase.AllUpper, 10).Count);
		}

		[Test]
		public void ValidTable2()
		{
			Assert.AreEqual(0,
			                GetErrors("GEO_00100024004", 30, ExpectedCase.AllUpper, 10).Count);
		}

		[Test]
		public void ValidTable3()
		{
			Assert.AreEqual(0,
			                GetErrors("GEO_00100059002", 30, ExpectedCase.AllUpper, 10).Count);
		}

		[Test]
		public void ValidTable4()
		{
			Assert.AreEqual(0,
			                GetErrors("GEO_00100436001", 30, ExpectedCase.AllUpper, 10).Count);
		}

		[NotNull]
		private static IList<QaError> GetErrors([NotNull] string tableName,
		                                        int maximumLength,
		                                        ExpectedCase expectedCase,
		                                        int uniqueSubstringLength)
		{
			string path = TestDataPreparer.ExtractZip("QaSchemaTests.gdb.zip")
			                              .GetPath();

			IFeatureWorkspace workspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);

			ITable table = workspace.OpenTable(tableName);
			var test = new QaSchemaFieldNames(
				ReadOnlyTableFactory.Create(table), maximumLength, expectedCase,
				uniqueSubstringLength);

			var runner = new QaTestRunner(test);
			runner.Execute();

			return runner.Errors;
		}
	}
}
