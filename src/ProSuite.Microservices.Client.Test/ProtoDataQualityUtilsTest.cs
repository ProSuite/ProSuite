using System;
using System.Linq;
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

			var testDescriptor = new TestDescriptor(
				"Descriptor1",
				new ClassDescriptor("QaConstraint", "ProSuite.QA.Tests",
				                    "Descriptor of qa constraint"));

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

			const string specName = "TestSpecification (Customized)";
			var customQualitySpecification =
				new CustomQualitySpecification(qualitySpecification, specName);

			ConditionListSpecificationMsg conditionListSpecificationMsg =
				ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
					customQualitySpecification);

			Assert.AreEqual(specName, conditionListSpecificationMsg.Name);
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

			var testDescriptor = new TestDescriptor(
				"Descriptor1",
				new ClassDescriptor("QaConstraint", "ProSuite.QA.Tests",
				                    "Descriptor of qa constraint"));

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
				"IssueFilter", new ClassDescriptor("IfWithin", "ProSuite.QA.Tests"), 0,
				"Descriptor of IfWithin");

			var issueFilterConfiguration = new IssueFilterConfiguration(
				"TestIssueFilterConfig1", issueFilterDescriptor, "IF desc");

			issueFilterConfiguration.AddParameterValue(
				new DatasetTestParameterValue("featureclass", tableType)
				{
					DatasetValue = dataset,
					FilterExpression = filterExpression
				});

			qc1.AddIssueFilterConfiguration(issueFilterConfiguration);

			qualitySpecification.AddElement(qc1);

			const string specName = "TestSpecification (Customized)";
			var customQualitySpecification =
				new CustomQualitySpecification(qualitySpecification, specName);

			ConditionListSpecificationMsg conditionListSpecificationMsg =
				ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
					customQualitySpecification);

			Assert.AreEqual(specName, conditionListSpecificationMsg.Name);
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
			Assert.AreEqual("featureclass", issueFilterMsgParameter.Name);
			Assert.AreEqual(filterExpression, issueFilterMsgParameter.WhereClause);
		}

		[Test]
		public void CanCreateConditionListSpecificationMsgWithTransformer()
		{
			var qualitySpecification =
				new QualitySpecification("TestSpecification");

			var testDescriptor = new TestDescriptor(
				"Descriptor2",
				new ClassDescriptor("QaConstraint", "ProSuite.QA.Tests",
				                    "Descriptor of qa constraint"));

			const string conditionName = "TestCondition2";

			Type tableType = typeof(IReadOnlyTable);
			Assert.NotNull(tableType);

			const string filterExpression = "OBJECTID > 100";
			Dataset dataset = new TestDataset("TEST_TABLE");
			var model = new TestModel("TEST_MODEL");
			model.AddDataset(dataset);

			var transformerDescriptor = new TransformerDescriptor(
				"Transformer1", new ClassDescriptor("TrFootprint", "ProSuite.QA.Tests"), 0,
				"Descriptor of TrFootprint");

			var transformerConfig = new TransformerConfiguration(
				"TestTransformerConfig1", transformerDescriptor, "Transformer desc");

			transformerConfig.AddParameterValue(
				new DatasetTestParameterValue("featureclass", tableType)
				{
					DatasetValue = dataset,
					FilterExpression = filterExpression
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

			const string specName = "TestSpecification (Customized)";
			var customQualitySpecification =
				new CustomQualitySpecification(qualitySpecification, specName);

			ConditionListSpecificationMsg conditionListSpecificationMsg =
				ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
					customQualitySpecification);

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
			Assert.AreEqual("featureclass", transformerMsgParameter.Name);
			Assert.AreEqual(filterExpression, transformerMsgParameter.WhereClause);

			Assert.AreEqual("table", param0.Name);
			Assert.IsEmpty(param0.WhereClause);

			ParameterMsg param1 = conditionMsg.Parameters[1];
			Assert.AreEqual("constraint", param1.Name);
			Assert.AreEqual(constraintExpression, param1.Value);

			Assert.AreEqual(qc1.TestDescriptor.Name, conditionMsg.TestDescriptorName);
		}

		private class TestModel : DdxModel
		{
			public TestModel(string name) : base(name) { }

			#region Overrides of DdxModel

			public override string QualifyModelElementName(string modelElementName)
			{
				return modelElementName;
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
