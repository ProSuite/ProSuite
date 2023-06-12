using System.Globalization;
using System.Linq;
using NUnit.Framework;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Persistence.Core.Test.QA
{
	[TestFixture]
	public abstract class QualityConditionRepositoryTestBase :
		RepositoryTestBase<IQualityConditionRepository>
	{
		protected abstract DdxModel CreateModel();

		protected abstract VectorDataset CreateVectorDataset(string name);

		[Test]
		public void CanCreateQualityConditionFromFactory()
		{
			const string qconName = "qcon1";
			const string testName = "test1";
			var testDescriptor =
				new TestDescriptor(testName,
				                   new ClassDescriptor(
					                   "ProSuite.QA.TestFactories.QaLineConnection",
					                   "ProSuite.QA.TestFactories"), false, false);

			var condition = new QualityCondition(qconName, testDescriptor);
			const string sVal0 = "objectVal in (1,2)";
			InstanceConfigurationUtils.AddParameterValue(condition, "rules", sVal0);

			const string sVal1 = "objectVal in (3,4)";
			InstanceConfigurationUtils.AddParameterValue(condition, "rules", sVal1);

			CreateSchema(testDescriptor, condition);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					QualityCondition readCondition = Repository.Get(condition.Id);

					Assert.IsNotNull(readCondition);
					Assert.AreNotSame(readCondition, condition);
					Assert.AreEqual(qconName, readCondition.Name);
					Assert.AreEqual(testName, readCondition.TestDescriptor.Name);
					Assert.AreEqual(2, readCondition.ParameterValues.Count);

					var value0 = (ScalarTestParameterValue) readCondition.ParameterValues[0];
					var value1 = (ScalarTestParameterValue) readCondition.ParameterValues[1];

					Assert.IsNotNull(value0);

					Assert.AreEqual("rules", value0.TestParameterName);
					Assert.AreEqual("rules", value1.TestParameterName);

					Assert.AreEqual(sVal0, value0.PersistedStringValue);
					Assert.AreEqual(sVal1, value1.PersistedStringValue);

					Assert.AreEqual(sVal0, value0.GetDisplayValue());
					Assert.AreEqual(sVal1, value1.GetDisplayValue());

					Assert.AreEqual(sVal0, value0.GetValue(typeof(string)));
					Assert.AreEqual(sVal1, value1.GetValue(typeof(string)));

					IInstanceInfo testInfo =
						InstanceDescriptorUtils.GetInstanceInfo(readCondition.TestDescriptor);
					Assert.IsNotNull(testInfo);

					Assert.AreEqual(2, testInfo.Parameters.Count);
				}
			);
		}

		[Test]
		public void CanCreateQualityConditionFromTest()
		{
			const string dsName = "SCHEMA.TLM_DATASET1";
			const string qconName = "qcon1";
			const string testName = "test1";

			const bool stopOnError = true;
			const bool allowErrors = false;

			DdxModel m = CreateModel();

			VectorDataset ds = m.AddDataset(CreateVectorDataset(dsName));

			var testDescriptor = new TestDescriptor(testName,
			                                        new ClassDescriptor(
				                                        "ProSuite.QA.Tests.QaMinSegAngle",
				                                        "ProSuite.QA.Tests"), 0,
			                                        stopOnError, allowErrors);

			var condition = new QualityCondition(qconName, testDescriptor);
			const double limit = 1;
			const bool is3D = true;
			InstanceConfigurationUtils.AddParameterValue(condition, "limit", limit);
			InstanceConfigurationUtils.AddParameterValue(condition, "is3D", is3D);
			InstanceConfigurationUtils.AddParameterValue(condition, "featureClass", ds);

			CreateSchema(testDescriptor, condition, m);

			UnitOfWork.NewTransaction(
				delegate
				{
					QualityCondition readCondition = Repository.Get(condition.Id);

					Assert.IsNotNull(readCondition);
					Assert.AreNotSame(readCondition, condition);
					Assert.AreEqual(qconName, readCondition.Name);
					Assert.AreEqual(testName, readCondition.TestDescriptor.Name);
					Assert.AreEqual(3, readCondition.ParameterValues.Count);
					Assert.AreEqual(allowErrors, readCondition.TestDescriptor.AllowErrors);
					Assert.AreEqual(stopOnError, readCondition.TestDescriptor.StopOnError);
					Assert.AreEqual(allowErrors, readCondition.AllowErrors);
					Assert.AreEqual(stopOnError, readCondition.StopOnError);

					var value0 = readCondition.ParameterValues[0] as ScalarTestParameterValue;
					var value1 = readCondition.ParameterValues[1] as ScalarTestParameterValue;
					var value2 = readCondition.ParameterValues[2] as DatasetTestParameterValue;

					Assert.IsNotNull(value0);
					Assert.IsNotNull(value1);
					Assert.IsNotNull(value2);

					Assert.AreEqual("limit", value0.TestParameterName);
					Assert.AreEqual("is3D", value1.TestParameterName);
					Assert.AreEqual("featureClass", value2.TestParameterName);

					Assert.AreEqual(limit.ToString(CultureInfo.CurrentCulture),
					                value0.GetDisplayValue());
					Assert.AreEqual(is3D.ToString(), value1.GetDisplayValue());

					Assert.AreEqual(limit.ToString(CultureInfo.InvariantCulture),
					                value0.GetDisplayValue());
					Assert.AreEqual(is3D.ToString(), value1.GetDisplayValue());

					Assert.AreEqual(limit, value0.GetValue(typeof(double)));
					Assert.AreEqual(is3D, value1.GetValue(typeof(bool)));

					Dataset dataset = value2.DatasetValue;
					Assert.IsNotNull(dataset);
					Assert.AreEqual(dsName, dataset.Name);

					IInstanceInfo testInfo =
						InstanceDescriptorUtils.GetInstanceInfo(readCondition.TestDescriptor);
					Assert.IsNotNull(testInfo);

					// expect 3 constructor parameters plus 2 optional test parameters (properties)
					Assert.AreEqual(5, testInfo.Parameters.Count);
				}
			);
		}

		[Test]
		public void CanSaveConditionWithTransformer()
		{
			const string ds1Name = "SCHEMA.TLM_DATASET1";
			const string ds2Name = "SCHEMA.TLM_DATASET2";
			const string qconName = "qcon1";
			const string testName = "test1";
			const string filterDefName = "intersects";
			const string filterConfigName = "intersectsDataset2";

			const bool stopOnError = true;
			const bool allowErrors = false;

			DdxModel m = CreateModel();

			VectorDataset ds1 = m.AddDataset(CreateVectorDataset(ds1Name));
			VectorDataset ds2 = m.AddDataset(CreateVectorDataset(ds2Name));

			var testDescriptor = new TestDescriptor(testName,
			                                        new ClassDescriptor(
				                                        "ProSuite.QA.Tests.QaMinSegAngle",
				                                        "ProSuite.QA.Tests"), 0,
			                                        stopOnError, allowErrors);

			var condition = new QualityCondition(qconName, testDescriptor);
			const double limit = 1;
			const bool is3D = true;
			InstanceConfigurationUtils.AddParameterValue(condition, "limit", limit);
			InstanceConfigurationUtils.AddParameterValue(condition, "is3D", is3D);

			TransformerDescriptor filterDescriptor = new TransformerDescriptor(
				filterDefName, new ClassDescriptor(
					"ProSuite.QA.Tests.Transformers.Filters.TrOnlyIntersectingFeatures",
					"ProSuite.QA.Tests"), 0);

			var filterTransformerConfig =
				new TransformerConfiguration(filterConfigName, filterDescriptor);
			InstanceConfigurationUtils.AddParameterValue(filterTransformerConfig,
			                                             "featureClassToFilter", ds1);
			InstanceConfigurationUtils.AddParameterValue(filterTransformerConfig, "intersecting",
			                                             ds2);

			// Instead of the feature class directly, specify the filter-transformer configuration:
			DatasetTestParameterValue conditionParameter =
				InstanceConfigurationUtils.AddParameterValue(
					condition, "featureClass", filterTransformerConfig);

			Assert.NotNull(conditionParameter.ValueSource);

			CreateSchema(testDescriptor, filterDescriptor, filterTransformerConfig, condition, m);

			UnitOfWork.NewTransaction(
				delegate
				{
					QualityCondition readCondition = Repository.Get(condition.Id);

					Assert.IsNotNull(readCondition);
					Assert.AreNotSame(readCondition, condition);
					Assert.AreEqual(qconName, readCondition.Name);
					Assert.AreEqual(testName, readCondition.TestDescriptor.Name);
					Assert.AreEqual(3, readCondition.ParameterValues.Count);
					Assert.AreEqual(allowErrors, readCondition.TestDescriptor.AllowErrors);
					Assert.AreEqual(stopOnError, readCondition.TestDescriptor.StopOnError);
					Assert.AreEqual(allowErrors, readCondition.AllowErrors);
					Assert.AreEqual(stopOnError, readCondition.StopOnError);

					var value0 = readCondition.ParameterValues[0] as ScalarTestParameterValue;
					var value1 = readCondition.ParameterValues[1] as ScalarTestParameterValue;
					var value2 = readCondition.ParameterValues[2] as DatasetTestParameterValue;

					Assert.IsNotNull(value0);
					Assert.IsNotNull(value1);
					Assert.IsNotNull(value2);

					Assert.AreEqual("featureClass", value2.TestParameterName);

					Assert.NotNull(value2.ValueSource);
					var readFilterConfig = value2.ValueSource;

					var filteredByDatasetParameter =
						readFilterConfig.ParameterValues[1] as DatasetTestParameterValue;
					Assert.NotNull(filteredByDatasetParameter);
					var dataset = filteredByDatasetParameter.DatasetValue;

					Assert.IsNotNull(dataset);
					Assert.AreEqual(ds2Name, dataset.Name);

					Assert.AreEqual(filterDefName, readFilterConfig.InstanceDescriptor.Name);

					IInstanceInfo filterInfo = InstanceDescriptorUtils.GetInstanceInfo(
						readFilterConfig.InstanceDescriptor);

					Assert.IsNotNull(filterInfo);

					Assert.AreEqual(2, filterInfo.Parameters.Count(p => p.IsConstructorParameter));
				}
			);
		}

		[Test]
		public void CanSaveConditionWithIssueFilter()
		{
			const string ds1Name = "SCHEMA.TLM_DATASET1";
			const string ds2Name = "SCHEMA.TLM_DATASET2";
			const string qconName = "qcon1";
			const string testName = "test1";
			const string filterDefName = "intersects";
			const string filterDescription = "Filter by ObjId";
			const string filterConfigName = "intersectsDataset2";
			const string constraint = "OBJECTID = 42";

			const bool stopOnError = true;
			const bool allowErrors = false;

			DdxModel m = CreateModel();

			VectorDataset ds1 = m.AddDataset(CreateVectorDataset(ds1Name));
			VectorDataset ds2 = m.AddDataset(CreateVectorDataset(ds2Name));

			var testDescriptor = new TestDescriptor(testName,
			                                        new ClassDescriptor(
				                                        "ProSuite.QA.Tests.QaMinSegAngle",
				                                        "ProSuite.QA.Tests"), 0,
			                                        stopOnError, allowErrors);

			var condition = new QualityCondition(qconName, testDescriptor);
			const double limit = 1;
			const bool is3D = true;
			InstanceConfigurationUtils.AddParameterValue(condition, "limit", limit);
			InstanceConfigurationUtils.AddParameterValue(condition, "is3D", is3D);

			DatasetTestParameterValue datasetParameterValue =
				InstanceConfigurationUtils.AddParameterValue(condition, "featureClass", ds1);

			var filterDescriptor = new IssueFilterDescriptor(
				filterDefName, new ClassDescriptor("ProSuite.QA.Tests.IssueFilters.IfInvolvedRows",
				                                   "ProSuite.QA.Tests"), 0,
				filterDescription);

			var filterConfig = new IssueFilterConfiguration(filterConfigName, filterDescriptor);

			InstanceConfigurationUtils.AddParameterValue(filterConfig, "constraint",
			                                             constraint);

			condition.AddIssueFilterConfiguration(filterConfig);

			CreateSchema(testDescriptor, filterDescriptor, filterConfig, condition, m);

			UnitOfWork.NewTransaction(
				delegate
				{
					QualityCondition readCondition = Repository.Get(condition.Id);

					Assert.IsNotNull(readCondition);
					Assert.AreNotSame(readCondition, condition);
					Assert.AreEqual(qconName, readCondition.Name);
					Assert.AreEqual(testName, readCondition.TestDescriptor.Name);
					Assert.AreEqual(3, readCondition.ParameterValues.Count);
					Assert.AreEqual(allowErrors, readCondition.TestDescriptor.AllowErrors);
					Assert.AreEqual(stopOnError, readCondition.TestDescriptor.StopOnError);
					Assert.AreEqual(allowErrors, readCondition.AllowErrors);
					Assert.AreEqual(stopOnError, readCondition.StopOnError);

					var value0 = readCondition.ParameterValues[0] as ScalarTestParameterValue;
					var value1 = readCondition.ParameterValues[1] as ScalarTestParameterValue;
					var value2 = readCondition.ParameterValues[2] as DatasetTestParameterValue;

					Assert.IsNotNull(value0);
					Assert.IsNotNull(value1);
					Assert.IsNotNull(value2);

					Assert.AreEqual(1, readCondition.IssueFilterConfigurations.Count);

					var readFilterConfig = readCondition.IssueFilterConfigurations[0];
					var filterConstraint =
						readFilterConfig.ParameterValues[0] as ScalarTestParameterValue;

					Assert.NotNull(filterConstraint);
					Assert.AreEqual(constraint, filterConstraint.PersistedStringValue);

					Assert.NotNull(readFilterConfig.InstanceDescriptor);
					Assert.AreEqual(filterDescription,
					                readFilterConfig.InstanceDescriptor.Description);
					IInstanceInfo filterInfo =
						InstanceDescriptorUtils.GetInstanceInfo(
							readFilterConfig.InstanceDescriptor);

					Assert.NotNull(filterInfo);
					Assert.NotNull(filterInfo.TestDescription);
				}
			);
		}
	}
}
