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

			TestDescriptor d2 = new TestDescriptor(
				"test2", new ClassDescriptor("factTypeName", "factAssemblyName__"),
				0, true, false, "desc");

			CreateSchema(d1, d2);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					IList<TestDescriptor> foundDescriptors = Repository.GetAll();

					Assert.AreEqual(2, foundDescriptors.Count);

					Assert.AreEqual(
						"desc", foundDescriptors.Single(d => d.Name == "test2").Description);
				});
		}
	}
}
