using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.Test
{
	public class ProtoDataQualityUtilsTest
	{
		[Test]
		public void CanCreateConditionListSpecificationMsg()
		{
			var qualitySpecification =
				new QualitySpecification("TestSpecification");

			TestDescriptor testDescriptor = CreateConstraintTestDescriptor();

			const string conditionName = "TestCondition1";
			var qc1 = new QualityCondition(conditionName, testDescriptor)
			          {
				          TestDescriptor = testDescriptor,
				          Description = "Condition1 Description"
			          };

			Type tableType = typeof(IReadOnlyTable);
			Assert.NotNull(tableType);

			const string filterExpression = "OBJECTID > 100";
			Dataset dataset = new TestDataset("TEST_TABLE");
			var model = new TestModel("TEST_MODEL");
			model.AddDataset(dataset);

			qc1.AddParameterValue(new DatasetTestParameterValue("table", tableType)
			                      {
				                      DatasetValue = dataset,
				                      FilterExpression = filterExpression
			                      });

			const string constraintExpression = "OPERATOR = 'XXX'";
			qc1.AddParameterValue(new ScalarTestParameterValue("constraint", typeof(string))
			                      {
				                      StringValue = constraintExpression
			                      });

			qualitySpecification.AddElement(qc1);

			var customQualitySpecification = qualitySpecification.GetCustomizable();

			ConditionListSpecificationMsg conditionListSpecificationMsg =
				ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
					customQualitySpecification, null, out _);

			const string expectedName = "TestSpecification (Clone)";
			Assert.AreEqual(expectedName, conditionListSpecificationMsg.Name);
			QualityConditionMsg conditionMsg =
				conditionListSpecificationMsg.Elements.First().Condition;
			Assert.AreEqual(conditionName, conditionMsg.Name);
			Assert.AreEqual(qc1.Description, conditionMsg.Description);

			ParameterMsg param0 = conditionMsg.Parameters[0];

			Assert.AreEqual(dataset.Name, param0.Value);
			Assert.AreEqual("table", param0.Name);
			Assert.AreEqual(filterExpression, param0.WhereClause);

			ParameterMsg param1 = conditionMsg.Parameters[1];
			Assert.AreEqual("constraint", param1.Name);
			Assert.AreEqual(constraintExpression, param1.Value);

			Assert.AreEqual("Descriptor1", conditionMsg.TestDescriptorName);

			var transformerDescriptor = new TransformerDescriptor(
				"Transformer1", new ClassDescriptor("TrFootprint", "ProSuite.QA.Tests"), 0,
				"Descriptor of TrFootprint");

			var transformerConfig = new TransformerConfiguration(
				"TestTransformerConfig1", transformerDescriptor, "Transformer desc");
			transformerConfig.AddParameterValue(
				new DatasetTestParameterValue("featureClass", tableType)
				{
					DatasetValue = dataset,
					FilterExpression = "OBJECTID < 1234"
				});

			var qc2 = new QualityCondition(conditionName, testDescriptor)
			          {
				          TestDescriptor = testDescriptor
			          };
			qc2.AddParameterValue(new DatasetTestParameterValue("table", tableType)
			                      {
				                      ValueSource = transformerConfig
			                      });

			qc2.AddParameterValue(new ScalarTestParameterValue("constraint", typeof(string))
			                      {
				                      StringValue = constraintExpression
			                      });
		}

		[Test]
		public void CanCreateConditionListSpecificationMsgWithIssueFilter()
		{
			var qualitySpecification =
				new QualitySpecification("TestSpecification");

			TestDescriptor testDescriptor = CreateConstraintTestDescriptor();

			const string conditionName = "TestCondition1";
			var qc1 = new QualityCondition(conditionName, testDescriptor)
			          {
				          TestDescriptor = testDescriptor,
				          Description = "Condition1 Description"
			          };

			Type tableType = typeof(IReadOnlyTable);
			Assert.NotNull(tableType);

			const string filterExpression = "OBJECTID > 100";
			Dataset dataset = new TestDataset("TEST_TABLE");
			var model = new TestModel("TEST_MODEL");
			model.AddDataset(dataset);

			qc1.AddParameterValue(new DatasetTestParameterValue("table", tableType)
			                      {
				                      DatasetValue = dataset
			                      });

			const string constraintExpression = "OPERATOR = 'XXX'";
			qc1.AddParameterValue(new ScalarTestParameterValue("constraint", typeof(string))
			                      {
				                      StringValue = constraintExpression
			                      });

			var issueFilterDescriptor = new IssueFilterDescriptor(
				"IssueFilter",
				new ClassDescriptor("ProSuite.QA.Tests.IssueFilters.IfWithin", "ProSuite.QA.Tests"),
				0,
				"Descriptor of IfWithin");

			var issueFilterConfiguration = new IssueFilterConfiguration(
				"TestIssueFilterConfig1", issueFilterDescriptor, "IF desc");

			issueFilterConfiguration.AddParameterValue(
				new DatasetTestParameterValue("featureClass", tableType)
				{
					DatasetValue = dataset,
					FilterExpression = filterExpression
				});

			qc1.AddIssueFilterConfiguration(issueFilterConfiguration);

			qualitySpecification.AddElement(qc1);

			var customQualitySpecification = qualitySpecification.GetCustomizable();
			//new CustomQualitySpecification(qualitySpecification, specName);

			ConditionListSpecificationMsg conditionListSpecificationMsg =
				ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
					customQualitySpecification, null, out _);

			const string expectedName = "TestSpecification (Clone)";
			Assert.AreEqual(expectedName, conditionListSpecificationMsg.Name);
			QualityConditionMsg conditionMsg =
				conditionListSpecificationMsg.Elements.First().Condition;
			Assert.AreEqual(conditionName, conditionMsg.Name);
			Assert.AreEqual(qc1.Description, conditionMsg.Description);

			InstanceConfigurationMsg issueFilterConfigMsg =
				conditionMsg.ConditionIssueFilters.First();

			Assert.AreEqual(issueFilterConfiguration.Name, issueFilterConfigMsg.Name);
			Assert.AreEqual(issueFilterConfiguration.Description, issueFilterConfigMsg.Description);
			Assert.AreEqual(issueFilterConfiguration.IssueFilterDescriptor.Name,
			                issueFilterConfigMsg.InstanceDescriptorName);

			ParameterMsg issueFilterMsgParameter = issueFilterConfigMsg.Parameters[0];
			Assert.AreEqual(dataset.Name, issueFilterMsgParameter.Value);
			Assert.AreEqual("featureClass", issueFilterMsgParameter.Name);
			Assert.AreEqual(filterExpression, issueFilterMsgParameter.WhereClause);
		}

		[Test]
		public void CanCreateConditionListSpecificationMsgWithTransformer()
		{
			var qualitySpecification =
				new QualitySpecification("TestSpecification");

			TestDescriptor testDescriptor = CreateConstraintTestDescriptor();

			const string conditionName = "TestCondition2";

			Type tableType = typeof(IReadOnlyTable);
			Assert.NotNull(tableType);

			const string filterExpression = "OBJECTID > 100";
			Dataset dataset = new TestDataset("TEST_TABLE");
			var model = new TestModel("TEST_MODEL");
			model.AddDataset(dataset);

			var transformerDescriptor = new TransformerDescriptor(
				"Transformer1",
				new ClassDescriptor("ProSuite.QA.Tests.Transformers.TrFootprint",
				                    "ProSuite.QA.Tests"), 0,
				"Descriptor of TrFootprint");

			var transformerConfig = new TransformerConfiguration(
				"TestTransformerConfig1", transformerDescriptor, "Transformer desc");

			transformerConfig.AddParameterValue(
				new DatasetTestParameterValue("multipatchClass", tableType)
				{
					DatasetValue = dataset,
					FilterExpression = filterExpression,
				});

			var qc1 = new QualityCondition(conditionName, testDescriptor)
			          {
				          TestDescriptor = testDescriptor,
				          Description = "Condition2 Description"
			          };

			qc1.AddParameterValue(new DatasetTestParameterValue("table", tableType)
			                      {
				                      ValueSource = transformerConfig
			                      });

			const string constraintExpression = "OPERATOR = 'XXX'";
			qc1.AddParameterValue(new ScalarTestParameterValue("constraint", typeof(string))
			                      {
				                      StringValue = constraintExpression
			                      });

			qualitySpecification.AddElement(qc1);

			const string specName = "TestSpecification (Clone)";
			var customQualitySpecification = qualitySpecification.GetCustomizable();
			//new CustomQualitySpecification(qualitySpecification, specName);

			ConditionListSpecificationMsg conditionListSpecificationMsg =
				ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
					customQualitySpecification, null, out _);

			Assert.AreEqual(specName, conditionListSpecificationMsg.Name);
			QualityConditionMsg conditionMsg =
				conditionListSpecificationMsg.Elements.First().Condition;
			Assert.AreEqual(conditionName, conditionMsg.Name);
			Assert.AreEqual(qc1.Description, conditionMsg.Description);

			ParameterMsg param0 = conditionMsg.Parameters[0];

			// Protobuf 'no value': Empty
			Assert.AreEqual(string.Empty, param0.Value);
			InstanceConfigurationMsg transformerMsg = param0.Transformer;
			Assert.AreEqual(transformerConfig.Name, transformerMsg.Name);
			Assert.AreEqual(transformerConfig.Description, transformerMsg.Description);
			Assert.AreEqual(transformerConfig.TransformerDescriptor.Name,
			                transformerMsg.InstanceDescriptorName);
			ParameterMsg transformerMsgParameter = transformerMsg.Parameters[0];
			Assert.AreEqual(dataset.Name, transformerMsgParameter.Value);
			Assert.AreEqual("multipatchClass", transformerMsgParameter.Name);
			Assert.AreEqual(filterExpression, transformerMsgParameter.WhereClause);

			Assert.AreEqual("table", param0.Name);
			Assert.IsEmpty(param0.WhereClause);

			ParameterMsg param1 = conditionMsg.Parameters[1];
			Assert.AreEqual("constraint", param1.Name);
			Assert.AreEqual(constraintExpression, param1.Value);

			Assert.AreEqual(qc1.TestDescriptor.Name, conditionMsg.TestDescriptorName);
		}

		private static TestDescriptor CreateConstraintTestDescriptor()
		{
			var testDescriptor = new TestDescriptor(
				"Descriptor1",
				new ClassDescriptor("ProSuite.QA.Tests.QaConstraint", "ProSuite.QA.Tests",
				                    "Descriptor of qa constraint"), 0);
			return testDescriptor;
		}

		[Test]
		public void CanCreateConditionListSpecificationMsgWithTransformerUndefinedScalarParameters()
		{
			// If the specification comes from persistence, the DataType of the parameter values are not set!
			// They must be initialized (including transformers, filters) before serializing to proto.

			// Additionally test the behaviour with a provided ISupportedInstanceDescriptors
			// that allows checking the descriptor name already on the client side.

			var instanceDescriptors = Substitute.For<ISupportedInstanceDescriptors>();

			var qualitySpecification =
				new QualitySpecification("TestSpecification");

			TestDescriptor testDescriptor = CreateConstraintTestDescriptor();

			// Match using current name
			instanceDescriptors.GetInstanceDescriptor<InstanceDescriptor>(testDescriptor.Name)
			                   .Returns(testDescriptor);

			const string conditionName = "TestCondition2";

			Type tableType = typeof(IReadOnlyTable);
			Assert.NotNull(tableType);

			const string filterExpression = "OBJECTID > 100";
			Dataset dataset = new TestDataset("TEST_TABLE");
			var model = new TestModel("TEST_MODEL");
			model.AddDataset(dataset);

			var transformerDescriptor = new TransformerDescriptor(
				"Transformer1",
				new ClassDescriptor("ProSuite.QA.Tests.Transformers.TrDissolve",
				                    "ProSuite.QA.Tests"), 0,
				"Descriptor of TrDissolve");

			// Match using canonical name (the transformer descriptor's name will be replaced)
			const string trCanonicalName = "TrDissolve(0)";
			instanceDescriptors.GetInstanceDescriptor<InstanceDescriptor>(trCanonicalName)
			                   .Returns(transformerDescriptor);

			var transformerConfig = new TransformerConfiguration(
				"TestTransformerConfig1", transformerDescriptor, "Transformer desc");

			transformerConfig.AddParameterValue(
				new DatasetTestParameterValue("featureClass", tableType)
				{
					DatasetValue = dataset,
					FilterExpression = filterExpression,
				});

			const string scalarParameterName = "Search";
			const string scalarStringValue = "25.8";

			var scalar = transformerConfig.AddParameterValue(
				new ScalarTestParameterValue(scalarParameterName, typeof(double))
				{
					StringValue = scalarStringValue
				});

			// Simulate it coming from persistence:
			scalar.DataType = null;

			var qc1 = new QualityCondition(conditionName, testDescriptor)
			          {
				          TestDescriptor = testDescriptor,
				          Description = "Condition2 Description"
			          };

			qc1.AddParameterValue(new DatasetTestParameterValue("table", tableType)
			                      {
				                      ValueSource = transformerConfig
			                      });

			const string constraintExpression = "OPERATOR = 'XXX'";
			qc1.AddParameterValue(new ScalarTestParameterValue("constraint", typeof(string))
			                      {
				                      StringValue = constraintExpression
			                      });

			// TODO: Once we have issue filters with scalar parameters, check them here as well.

			qualitySpecification.AddElement(qc1);

			const string specName = "TestSpecification (Clone)";
			var customQualitySpecification = qualitySpecification.GetCustomizable();

			ConditionListSpecificationMsg conditionListSpecificationMsg =
				ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
					customQualitySpecification, instanceDescriptors,
					out IDictionary<int, DdxModel> usedModels);

			Assert.AreEqual(1, usedModels.Count);
			Assert.AreSame(model, usedModels[model.Id]);

			Assert.AreEqual(specName, conditionListSpecificationMsg.Name);
			QualityConditionMsg conditionMsg =
				conditionListSpecificationMsg.Elements.First().Condition;
			Assert.AreEqual(conditionName, conditionMsg.Name);
			Assert.AreEqual(qc1.Description, conditionMsg.Description);

			ParameterMsg param0 = conditionMsg.Parameters[0];

			// Protobuf 'no value': Empty
			Assert.AreEqual(string.Empty, param0.Value);
			InstanceConfigurationMsg transformerMsg = param0.Transformer;
			Assert.AreEqual(transformerConfig.Name, transformerMsg.Name);
			Assert.AreEqual(transformerConfig.Description, transformerMsg.Description);
			Assert.AreEqual("Transformer1",
			                transformerMsg.InstanceDescriptorName);
			ParameterMsg transformerMsgParameter0 = transformerMsg.Parameters[0];
			Assert.AreEqual(dataset.Name, transformerMsgParameter0.Value);
			Assert.AreEqual("featureClass", transformerMsgParameter0.Name);
			Assert.AreEqual(filterExpression, transformerMsgParameter0.WhereClause);

			ParameterMsg transformerMsgParameter1 = transformerMsg.Parameters[1];
			Assert.AreEqual(scalarStringValue, transformerMsgParameter1.Value);
			Assert.AreEqual(scalarParameterName, transformerMsgParameter1.Name);

			Assert.AreEqual("table", param0.Name);
			Assert.IsEmpty(param0.WhereClause);

			ParameterMsg param1 = conditionMsg.Parameters[1];
			Assert.AreEqual("constraint", param1.Name);
			Assert.AreEqual(constraintExpression, param1.Value);

			Assert.AreEqual(qc1.TestDescriptor.Name, conditionMsg.TestDescriptorName);
		}

		private class TestModel : DdxModel
		{
			public TestModel(string name) : base(name)
			{
				SetCloneId(123);
			}

			#region Overrides of DdxModel

			public override string QualifyModelElementName(string modelElementName)
			{
				return modelElementName;
			}

			public override string TranslateToModelElementName(string masterDatabaseDatasetName)
			{
				return masterDatabaseDatasetName;
			}

			protected override void CheckAssignSpecialDatasetCore(Dataset dataset) { }

			#endregion
		}

		private class TestDataset : VectorDataset
		{
			public TestDataset(string name) : base(name) { }
		}
	}
}
