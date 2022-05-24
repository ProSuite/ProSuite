using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSchemaFieldPropertiesTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _workspace;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			_workspace = TestWorkspaceUtils.CreateTestFgdbWorkspace(GetType().Name);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		// TODO add additional tests

		[Test]
		public void ValidField1()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"));

			var runner = new QaTestRunner(new QaSchemaFieldProperties(
				                              ReadOnlyTableFactory.Create(table),
				                              "FIELD1", esriFieldType.esriFieldTypeString,
				                              10, "Field 1", null, false));

			runner.Execute();

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void MissingFieldIsOptional()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"));

			var runner = new QaTestRunner(new QaSchemaFieldProperties(
				                              ReadOnlyTableFactory.Create(table),
				                              "MISSING", esriFieldType.esriFieldTypeString,
				                              0, null, null, true));

			runner.Execute();

			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		public void MissingFieldNotOptional()
		{
			ITable table = DatasetUtils.CreateTable(
				_workspace, MethodBase.GetCurrentMethod().Name,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("FIELD1", 10, "Field 1"));

			var runner = new QaTestRunner(new QaSchemaFieldProperties(
				                              ReadOnlyTableFactory.Create(table),
				                              "MISSING", esriFieldType.esriFieldTypeString,
				                              0, null, null, false));

			runner.Execute();

			Assert.AreEqual(1, runner.Errors.Count);
		}
	}
}
