using System.Globalization;
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
			const string dsName = "TOPGIS.TLM_DATASET1";
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
	}
}
