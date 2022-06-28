using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.QA
{
	[TestFixture]
	public abstract class InstanceDescriptorRepositoryTestBase :
		RepositoryTestBase<IInstanceDescriptorRepository>
	{
		[Test]
		public void CanGetAll()
		{
			var t1 = new TransformerDescriptor(
				"trans1", new ClassDescriptor("factTypeName", "factAssemblyName"), 0);

			var t2 = new TransformerDescriptor(
				"trans2", new ClassDescriptor("factTypeName", "factAssemblyName__"),
				0, "desc");

			var f1 = new RowFilterDescriptor(
				"filt1", new ClassDescriptor("rowFiltTypeName", "factAssemblyName"), 0);

			const int testConstructorId = 1;
			var i1 = new IssueFilterDescriptor(
				"ifilt1", new ClassDescriptor("issueFilTypeName", "factAssemblyName"),
				testConstructorId);

			CreateSchema(t1, t2, f1, i1);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					IList<InstanceDescriptor> foundDescriptors = Repository.GetAll();

					Assert.AreEqual(4, foundDescriptors.Count);

					InstanceDescriptor foundI1 = foundDescriptors.Single(d => d.Name == "ifilt1");
					Assert.AreEqual(testConstructorId, foundI1.ConstructorId);
					Assert.AreEqual(i1.Class, foundI1.Class);
				});
		}

		[Test]
		public void CanGetAllTransformers()
		{
			var t1 = new TransformerDescriptor(
				"trans1", new ClassDescriptor("factTypeName", "factAssemblyName"), 0);

			const int testConstructorId = 2;
			var t2 = new TransformerDescriptor(
				"trans2", new ClassDescriptor("factTypeName", "factAssemblyName__"),
				testConstructorId, "desc");

			CreateSchema(t1, t2);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					IList<TransformerDescriptor> foundDescriptors =
						Repository.GetTransformerDescriptors();

					Assert.AreEqual(2, foundDescriptors.Count);

					TransformerDescriptor foundTest2 =
						foundDescriptors.Single(d => d.Name == "trans2");
					Assert.AreEqual("desc", foundTest2.Description);
					Assert.AreEqual(testConstructorId, foundTest2.ConstructorId);
					Assert.AreEqual(t2.Class, foundTest2.Class);
				});
		}
	}
}
