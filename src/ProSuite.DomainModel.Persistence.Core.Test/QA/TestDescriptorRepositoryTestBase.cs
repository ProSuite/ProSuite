using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.QA
{
	[TestFixture]
	public abstract class TestDescriptorRepositoryTestBase :
		RepositoryTestBase<ITestDescriptorRepository>
	{
		[Test]
		public void CanGetAll()
		{
			TestDescriptor d1 = new TestDescriptor(
				"test1", new ClassDescriptor("factTypeName", "factAssemblyName"));

			const int testConstructorId = 2;
			TestDescriptor d2 = new TestDescriptor(
				"test2", new ClassDescriptor("factTypeName", "factAssemblyName__"),
				testConstructorId, true, false, "desc");

			CreateSchema(d1, d2);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					IList<TestDescriptor> foundDescriptors = Repository.GetAll();

					Assert.AreEqual(2, foundDescriptors.Count);

					TestDescriptor foundTest2 = foundDescriptors.Single(d => d.Name == "test2");
					Assert.AreEqual("desc", foundTest2.Description);
					Assert.AreEqual(testConstructorId, foundTest2.TestConstructorId);
					Assert.AreEqual(d2.Class, foundTest2.Class);
				});
		}

		[Test]
		public void CanGetWithSameImplementation()
		{
			TestDescriptor d1 = new TestDescriptor(
				"test1", new ClassDescriptor("factTypeName", "factAssemblyName"));

			TestDescriptor d2 = new TestDescriptor(
				"test2", new ClassDescriptor("factTypeName", "factAssemblyName__"),
				0, true, false, "desc");

			CreateSchema(d1, d2);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					TestDescriptor foundDescriptor = Repository.GetWithSameImplementation(d1);

					Assert.NotNull(foundDescriptor);

					Assert.AreEqual(d1.Class, foundDescriptor.Class);
				});
		}

		[Test]
		public void CanGetReferencingQualityConditionCount()
		{
			TestDescriptor d1 = new TestDescriptor(
				"test1", new ClassDescriptor("factTypeName", "factAssemblyName"));

			TestDescriptor d2 = new TestDescriptor(
				"test2", new ClassDescriptor("factTypeName", "factAssemblyName__"),
				0, true, false, "desc");

			QualityCondition c1 = new QualityCondition("testCondition", d1);

			CreateSchema(d1, d2, c1);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					var result = Repository.GetReferencingQualityConditionCount();

					Assert.AreEqual(1, result.Count);

					Assert.AreEqual(1, result[d1.Id]);

					Assert.IsFalse(result.ContainsKey(d2.Id));
				});
		}
	}
}
