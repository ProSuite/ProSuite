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
				"trans2", new ClassDescriptor("factTypeName", "factAssemblyName__"), 0, "desc");

			var i1 = new IssueFilterDescriptor(
				"ifilt1", new ClassDescriptor("issueFilTypeName", "factAssemblyName"), 1);

			CreateSchema(t1, t2, i1);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					IList<InstanceDescriptor> foundDescriptors = Repository.GetAll();

					Assert.AreEqual(3, foundDescriptors.Count);

					InstanceDescriptor foundI1 = foundDescriptors.Single(d => d.Name == "ifilt1");
					Assert.AreEqual(1, foundI1.ConstructorId);
					Assert.AreEqual(i1.Class, foundI1.Class);
				});
		}

		[Test]
		public void CanGetAllTransformers()
		{
			var t1 = new TransformerDescriptor(
				"trans1", new ClassDescriptor("factTypeName", "factAssemblyName"), 0);

			const int constructorId = 2;
			var t2 = new TransformerDescriptor(
				"trans2", new ClassDescriptor("factTypeName", "factAssemblyName__"),
				constructorId, "desc");

			var i1 = new IssueFilterDescriptor(
				"ifilt1", new ClassDescriptor("issueFilTypeName", "factAssemblyName"), 0);

			CreateSchema(t1, t2, i1);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					IList<TransformerDescriptor> foundDescriptors =
						Repository.GetInstanceDescriptors<TransformerDescriptor>();

					Assert.AreEqual(2, foundDescriptors.Count);

					TransformerDescriptor foundTrans2 =
						foundDescriptors.Single(d => d.Name == "trans2");
					Assert.AreEqual("desc", foundTrans2.Description);
					Assert.AreEqual(constructorId, foundTrans2.ConstructorId);
					Assert.AreEqual(t2.Class, foundTrans2.Class);

					IList<IssueFilterDescriptor> foundFilters =
						Repository.GetInstanceDescriptors<IssueFilterDescriptor>();

					Assert.AreEqual(1, foundFilters.Count);
					IssueFilterDescriptor issueFilter =
						foundFilters.Single(f => f.Name == "ifilt1");
					Assert.AreEqual("issueFilTypeName", issueFilter.Class.TypeName);
				});
		}

		[Test]
		public void CanGetWithSameImplementation()
		{
			var trClassDesc1 = new ClassDescriptor("factTypeName", "factAssemblyName");
			var trClassDesc2 = new ClassDescriptor("factType2Name", "factAssemblyName");
			var ifClassDesc1 = new ClassDescriptor("ifTypeName", "factAssemblyName");
			var ifClassDesc2 = new ClassDescriptor("ifType2Name", "factAssemblyName");
			var qClassDesc1 = new ClassDescriptor("testTypeName", "factAssemblyName");
			var qClassDesc2 = new ClassDescriptor("testType2Name", "factAssemblyName");

			var t1 = new TransformerDescriptor("trans1", trClassDesc1, 0);
			var t2 = new TransformerDescriptor("trans2", trClassDesc1, 0, "desc");
			var t3 = new TransformerDescriptor("transDiff", trClassDesc2, 0, "desc");

			var f1 = new IssueFilterDescriptor("filt1", ifClassDesc1, 0);
			var f2 = new IssueFilterDescriptor("filt2", ifClassDesc1, 0, "desc");
			var f3 = new TransformerDescriptor("filtDiff", ifClassDesc2, 0, "desc");

			var q1 = new TestDescriptor("test1", qClassDesc1, 1);
			var q2 = new TestDescriptor("test2", qClassDesc1, 1);
			var q3 = new TestDescriptor("testDiff", qClassDesc2, 1);

			CreateSchema(t1, f1, q1);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					InstanceDescriptor foundDescriptor = Repository.GetWithSameImplementation(t2);

					Assert.NotNull(foundDescriptor);
					Assert.AreEqual(t2.Class, foundDescriptor.Class);
					Assert.IsNull(Repository.GetWithSameImplementation(t3));

					foundDescriptor = Repository.GetWithSameImplementation(f2);
					Assert.NotNull(foundDescriptor);
					Assert.AreEqual(f2.Class, foundDescriptor.Class);
					Assert.IsNull(Repository.GetWithSameImplementation(f3));

					foundDescriptor = Repository.GetWithSameImplementation(q2);
					Assert.NotNull(foundDescriptor);
					Assert.AreEqual(q2.Class, foundDescriptor.Class);
					Assert.IsNull(Repository.GetWithSameImplementation(q3));
				});
		}
	}
}
