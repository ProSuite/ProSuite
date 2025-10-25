using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainModel.AO.Test.QA
{
	[TestFixture]
	public class TestParameterAttributeTest
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

		[Test]
		public void CanCreateWithoutParameters()
		{
			VectorDataset dataset = GetVectorDataset();

			var classDesc = new ClassDescriptor(typeof(BaseTest));
			var testDesc = new TestDescriptor("BaseTest", classDesc, 0);

			var condition = new QualityCondition("BaseCondition", testDesc);
			TestParameterValueUtils.AddParameterValue(condition, "table", dataset);

			var factory = TestFactoryUtils.CreateTestFactory(condition);
			Assert.IsNotNull(factory);

            var test =
                (BaseTest) factory.CreateTests(
                    new SimpleDatasetOpener(new MasterDatabaseDatasetContext()))[0];
			Assert.IsNotNull(test);
		}

		[Test]
		public void CanCreateWithMissingParameters()
		{
			VectorDataset dataset = GetVectorDataset();

			var classDesc = new ClassDescriptor(typeof(BaseTest));
			var testDesc = new TestDescriptor("BaseTest", classDesc, 0);

			var condition = new QualityCondition("BaseCondition", testDesc);
			TestParameterValueUtils.AddParameterValue(condition, "table", dataset);
			const string format = "N1";

			InstanceConfigurationUtils.AddScalarParameterValue(condition, "Format", format);

			var factory = TestFactoryUtils.CreateTestFactory(condition);
			Assert.IsNotNull(factory);

            var test =
                (BaseTest) factory.CreateTests(
                    new SimpleDatasetOpener(new MasterDatabaseDatasetContext()))[0];
			Assert.AreEqual(test.Format, format);
		}

		[Test]
		public void CanCreateWithDoublePropertyParameter()
		{
			VectorDataset dataset = GetVectorDataset();

			var classDesc = new ClassDescriptor(typeof(BaseTest));
			var testDesc = new TestDescriptor("BaseTest", classDesc, 1);

			var condition = new QualityCondition("BaseCondition", testDesc);
			TestParameterValueUtils.AddParameterValue(condition, "table", dataset);
			const double number = 2.71828;
			TestParameterValueUtils.AddParameterValue(condition, "Number", number);

			var factory = TestFactoryUtils.CreateTestFactory(condition);
			Assert.IsNotNull(factory);

            var test =
                (BaseTest) factory.CreateTests(
                    new SimpleDatasetOpener(new MasterDatabaseDatasetContext()))[0];

			Assert.AreEqual(number, test.Number);
		}

		[Test]
		public void CanCreateWithDoublePropertyUsingDefaultValue()
		{
			VectorDataset dataset = GetVectorDataset();

			var classDesc = new ClassDescriptor(typeof(BaseTest));
			var testDesc = new TestDescriptor("BaseTest", classDesc, 1);

			var condition = new QualityCondition("BaseCondition", testDesc);
			TestParameterValueUtils.AddParameterValue(condition, "table", dataset);
			TestParameterValueUtils.AddParameterValue(condition, "Number", 2.71828);

			var factory = TestFactoryUtils.CreateTestFactory(condition);
			Assert.IsNotNull(factory);

            var test =
                (BaseTest) factory.CreateTests(
                    new SimpleDatasetOpener(new MasterDatabaseDatasetContext()))[0];

			Assert.AreEqual(1.2345, test.Number2);
		}

		[Test]
		public void CanCreateWithObsoleteParameters()
		{
			VectorDataset dataset = GetVectorDataset();

			var classDesc = new ClassDescriptor(typeof(BaseTest));
			var testDesc = new TestDescriptor("BaseTest", classDesc, 0);

			var condition = new QualityCondition("BaseCondition", testDesc);
			TestParameterValueUtils.AddParameterValue(condition, "table", dataset);
			const string value = "obsoleteValue";
			InstanceConfigurationUtils.AddScalarParameterValue(condition, "Obsolete", value);

			var factory = TestFactoryUtils.CreateTestFactory(condition);
			Assert.IsNotNull(factory);

            var test =
                (BaseTest) factory.CreateTests(
                    new SimpleDatasetOpener(new MasterDatabaseDatasetContext()))[0];
			Assert.IsNotNull(test);
#pragma warning disable 612,618
			Assert.AreEqual(value, test.Obsolete);
#pragma warning restore 612,618
		}

		[Test]
		public void DerivedClassParameterTest()
		{
			VectorDataset dataset = GetVectorDataset();

			var classDesc = new ClassDescriptor(typeof(DerivedTest));
			var testDesc = new TestDescriptor("DerivedTest", classDesc, 0);
			var condition = new QualityCondition("BaseCondition", testDesc);

			TestParameterValueUtils.AddParameterValue(condition, "table", dataset);
			const string format = "N1";
			InstanceConfigurationUtils.AddScalarParameterValue(
				condition, "Format", format);

			var factory = TestFactoryUtils.CreateTestFactory(condition);
			Assert.IsNotNull(factory);

            var test =
                (BaseTest) factory.CreateTests(
                    new SimpleDatasetOpener(new MasterDatabaseDatasetContext()))[0];
			Assert.AreEqual(test.Format, format);
		}

		[NotNull]
		private static VectorDataset GetVectorDataset()
		{
			const string tableName = "Strassen";
			var fcStrassen = new FeatureClassMock(tableName,
			                                      esriGeometryType.esriGeometryPolyline, 1);
			var workspaceMock = new WorkspaceMock();
			workspaceMock.AddDataset(fcStrassen);

			var model = new TestModel
			            {
				            UserConnectionProvider =
					            new OpenWorkspaceConnectionProvider((IWorkspace) workspaceMock),
				            UseDefaultDatabaseOnlyForSchema = false
			            };

			var dataset = new TestVectorDataset(tableName);
			model.AddDataset(dataset);
			return dataset;
		}

		private class TestVectorDataset : VectorDataset
		{
			public TestVectorDataset(string name) : base(name) { }
		}

		private class TestModel : ProductionModel, IModelMasterDatabase
		{
			public override string QualifyModelElementName(string modelElementName)
			{
				return ModelUtils.QualifyModelElementName(this, modelElementName);
			}

			public override string TranslateToModelElementName(string masterDatabaseDatasetName)
			{
				return ModelUtils.TranslateToModelElementName(this, masterDatabaseDatasetName);
			}

			IWorkspaceContext IModelMasterDatabase.CreateMasterDatabaseWorkspaceContext()
			{
				return ModelUtils.CreateDefaultMasterDatabaseWorkspaceContext(this);
			}
		}
	}
}
