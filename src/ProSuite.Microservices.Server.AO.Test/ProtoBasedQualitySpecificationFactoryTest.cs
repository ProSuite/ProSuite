using System.IO;
using NSubstitute;
using NUnit.Framework;
using ProSuite.Commons.AO.Test;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Server.AO.QA;

namespace ProSuite.Microservices.Server.AO.Test
{
	[TestFixture]
	public class ProtoBasedQualitySpecificationFactoryTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();
			TestUtils.InitializeLicense();
		}

		[Test]
		public void CanCreateConditionListBasedQualitySpecification()
		{
			const string specificationName = "TestSpec";
			const string condition1Name = "Str_Simple";
			string gdbPath = TestData.GetGdb1Path();
			const string featureClassName = "lines";

			QualitySpecification qualitySpecification =
				CreateConditionBasedQualitySpecification(condition1Name, featureClassName,
				                                         specificationName, gdbPath);

			Assert.AreEqual(specificationName, qualitySpecification.Name);
			Assert.AreEqual(2, qualitySpecification.Elements.Count);

			QualitySpecificationElement element1 = qualitySpecification.Elements[0];
			QualityCondition condition1 = element1.QualityCondition;
			Assert.IsTrue(element1.Enabled);
			Assert.IsTrue(element1.StopOnError);
			Assert.NotNull(condition1);
			Assert.IsFalse(condition1.AllowErrors);
			Assert.AreEqual(condition1Name, condition1.Name);
			Assert.NotNull(condition1.Category);
			Assert.AreEqual("Geometry", condition1.Category?.Name);

			Assert.AreEqual(1, condition1.ParameterValues.Count);
			TransformerConfiguration transformer = condition1.ParameterValues[0].ValueSource;
			Assert.NotNull(transformer);
			Assert.NotNull(transformer.TransformerDescriptor);

			var fclassValue = transformer.ParameterValues[0] as DatasetTestParameterValue;
			Assert.NotNull(fclassValue?.DatasetValue);
			Assert.AreEqual(featureClassName, fclassValue.DatasetValue.Name);

			Assert.AreEqual(1, condition1.IssueFilterConfigurations.Count);
			IssueFilterConfiguration issueFilter = condition1.IssueFilterConfigurations[0];
			Assert.NotNull(issueFilter);
			Assert.NotNull(issueFilter.IssueFilterDescriptor);
			Assert.AreEqual(1, issueFilter.ParameterValues.Count);
		}

		[Test]
		public void CanExecuteConditionBasedSpecification()
		{
			const string specificationName = "TestSpec";
			const string condition1Name = "Str_Simple";
			string gdbPath = TestData.GetGdb1Path();
			const string featureClassName = "lines";

			QualitySpecification qualitySpecification =
				CreateConditionBasedQualitySpecification(
					condition1Name, featureClassName, specificationName, gdbPath);

			XmlBasedVerificationService service = new XmlBasedVerificationService();

			string tempDirPath = TestUtils.GetTempDirPath(null);

			service.ExecuteVerification(qualitySpecification, null, 1000, tempDirPath);

			Assert.IsTrue(Directory.Exists(Path.Combine(tempDirPath, "issues.gdb")));
			Assert.IsTrue(File.Exists(Path.Combine(tempDirPath, "verification.xml")));
		}

		private static QualitySpecification CreateConditionBasedQualitySpecification(
			string condition1Name, string featureClassName,
			string specificationName, string gdbPath)
		{
			ISupportedInstanceDescriptors instanceDescriptors =
				Substitute.For<ISupportedInstanceDescriptors>();

			const string simpleGeometryDescriptorName = "SimpleGeometry(0)";
			TestDescriptor simpleGeometryDescriptor = new TestDescriptor(
				simpleGeometryDescriptorName,
				new ClassDescriptor(
					"ProSuite.QA.Tests.QaSimpleGeometry",
					"ProSuite.QA.Tests"), 0);

			const string gdbConstraintsDescriptorName = "GdbConstraintFactory";
			TestDescriptor gdbConstraintsDescriptor = new TestDescriptor(
				gdbConstraintsDescriptorName,
				new ClassDescriptor(
					"ProSuite.QA.TestFactories.QaGdbConstraintFactory",
					"ProSuite.QA.TestFactories"));

			instanceDescriptors.GetTestDescriptor(simpleGeometryDescriptorName).Returns(
				simpleGeometryDescriptor);
			instanceDescriptors.GetTestDescriptor(gdbConstraintsDescriptorName).Returns(
				gdbConstraintsDescriptor);

			const string transformerDescName = "TrGeometryToPoints";
			TransformerDescriptor transformerDescriptor = new TransformerDescriptor(
				transformerDescName,
				new ClassDescriptor(
					"ProSuite.QA.Tests.Transformers.TrGeometryToPoints",
					"ProSuite.QA.Tests"), 0);
			instanceDescriptors.GetInstanceDescriptor<TransformerDescriptor>(transformerDescName)
			                   .Returns(transformerDescriptor);

			const string issueFilterDescName = "IfWithin";
			IssueFilterDescriptor issueFilterDescriptor = new IssueFilterDescriptor(
				issueFilterDescName,
				new ClassDescriptor(
					"ProSuite.QA.Tests.IssueFilters.IfWithin",
					"ProSuite.QA.Tests"), 0);
			instanceDescriptors.GetInstanceDescriptor<IssueFilterDescriptor>(issueFilterDescName)
			                   .Returns(issueFilterDescriptor);

			const string workspaceId = "TestID";

			//transformers
			var transformer1 = new InstanceConfigurationMsg
			                   {
				                   InstanceDescriptorName = transformerDescName,
				                   Name = "transformer",
				                   Parameters =
				                   {
					                   new ParameterMsg
					                   {
						                   Name = "featureClass",
						                   Value = featureClassName,
						                   WorkspaceId = workspaceId
					                   },
					                   new ParameterMsg
					                   {
						                   Name = "component",
						                   Value = "2"
					                   }
				                   }
			                   };

			//issue filters
			var issueFilter1 = new InstanceConfigurationMsg
			                   {
				                   InstanceDescriptorName = issueFilterDescName,
				                   Name = "issueFilter",
				                   Parameters =
				                   {
					                   new ParameterMsg
					                   {
						                   Name = "featureClass",
						                   Value = featureClassName,
						                   WorkspaceId = workspaceId
					                   }
				                   }
			                   };

			var condition1 = new QualityConditionMsg
			                 {
				                 TestDescriptorName = simpleGeometryDescriptorName,
				                 Name = condition1Name,
				                 Parameters =
				                 {
					                 new ParameterMsg
					                 {
						                 Name = "featureClass",
						                 Transformer = transformer1
					                 }
				                 },
				                 ConditionIssueFilters = { issueFilter1 }
			                 };

			var condition2 = new QualityConditionMsg
			                 {
				                 TestDescriptorName = gdbConstraintsDescriptorName,
				                 Name = "Str_GdbConstraints",
				                 Parameters =
				                 {
					                 new ParameterMsg
					                 {
						                 Name = "table",
						                 Value = featureClassName,
						                 WorkspaceId = workspaceId,
						                 WhereClause = "[OBJEKTART] IS NOT NULL"
					                 },
					                 new ParameterMsg
					                 {
						                 Name = "AllowNullValuesForCodedValueDomains",
						                 Value = "True"
					                 }
				                 }
			                 };

			var conditionListSpecificationMsg = new ConditionListSpecificationMsg
			                                    {
				                                    Name = specificationName
			                                    };

			conditionListSpecificationMsg.Elements.Add(new QualitySpecificationElementMsg
			                                           {
				                                           Condition = condition1,
				                                           CategoryName = "Geometry",
				                                           StopOnError = true
			                                           });
			conditionListSpecificationMsg.Elements.Add(new QualitySpecificationElementMsg
			                                           {
				                                           Condition = condition2,
				                                           CategoryName = "Attributes"
			                                           });

			var dataSources = new[]
			                  {
				                  new DataSource("Test DataSource", workspaceId, gdbPath)
			                  };

			QualitySpecification qualitySpecification =
				CreateQualitySpecification(instanceDescriptors, conditionListSpecificationMsg,
				                           dataSources);
			return qualitySpecification;
		}

		private static QualitySpecification CreateQualitySpecification(
			ISupportedInstanceDescriptors instanceDescriptors,
			ConditionListSpecificationMsg conditionListSpecificationMsg, DataSource[] dataSources)
		{
			var modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester());

			var factory = new ProtoBasedQualitySpecificationFactory(
				modelFactory, instanceDescriptors);

			QualitySpecification qualitySpecification =
				factory.CreateQualitySpecification(conditionListSpecificationMsg,
				                                   dataSources);
			return qualitySpecification;
		}
	}
}
