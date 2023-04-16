using System.IO;
using NUnit.Framework;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;

namespace ProSuite.DomainServices.AO.Test.QA.Standalone.XmlBased
{
	public class XmlBasedVerificationServiceTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanExecuteConditionBasedSpecification()
		{
			const string specificationName = "TestSpec";
			string gdbPath = TestData.GetGdb1Path();
			const string featureClassName = "lines";

			QualitySpecification qualitySpecification =
				CreateQualitySpecification(featureClassName, specificationName,
				                           gdbPath);

			XmlBasedVerificationService service = new XmlBasedVerificationService();

			string tempDirPath = TestUtils.GetTempDirPath(null);

			service.ExecuteVerification(qualitySpecification, null, 1000,
			                            tempDirPath);

			Assert.IsTrue(Directory.Exists(Path.Combine(tempDirPath, "issues.gdb")));
			Assert.IsTrue(File.Exists(Path.Combine(tempDirPath, "verification.xml")));
		}

		private static QualitySpecification CreateQualitySpecification(
			string featureClassName,
			string specificationName,
			string gdbPath)
		{
			const string simpleGeometryDescriptorName = "SimpleGeometry(0)";
			TestDescriptor simpleGeometryDescriptor = new TestDescriptor(
				simpleGeometryDescriptorName,
				new ClassDescriptor(
					"ProSuite.QA.Tests.QaSimpleGeometry",
					"ProSuite.QA.Tests"),
				0);

			const string gdbConstraintsDescriptorName = "GdbConstraintFactory";
			TestDescriptor gdbConstraintsDescriptor = new TestDescriptor(
				gdbConstraintsDescriptorName,
				new ClassDescriptor(
					"ProSuite.QA.TestFactories.QaGdbConstraintFactory",
					"ProSuite.QA.TestFactories"));

			Model model = new TestModel("testModel");
			model.UserConnectionProvider = new FileGdbConnectionProvider(gdbPath);
			TestDataset lineDataset = model.AddDataset(new TestDataset(featureClassName));

			var condition1 =
				new QualityCondition("condition1", simpleGeometryDescriptor);
			InstanceConfigurationUtils.AddParameterValue(condition1, "featureClass", lineDataset);

			var condition2 =
				new QualityCondition("condition2", gdbConstraintsDescriptor);

			InstanceConfigurationUtils.AddParameterValue(condition2, "table", lineDataset,
			                                             "[OBJEKTART] IS NOT NULL");
			InstanceConfigurationUtils.AddScalarParameterValue(
				condition2, "AllowNullValuesForCodedValueDomains", true);

			QualitySpecification specification = new QualitySpecification(specificationName);
			specification.AddElement(condition1);
			specification.AddElement(condition2);

			return specification;
		}

		private class TestModel : ProductionModel
		{
			public TestModel(string name) : base(name) { }

			protected override IWorkspaceContext CreateMasterDatabaseWorkspaceContext()
			{
				return CreateDefaultMasterDatabaseWorkspaceContext();
			}
		}

		private class TestDataset : VectorDataset
		{
			public TestDataset([NotNull] string name) : base(name) { }
		}
	}
}
