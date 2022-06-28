using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.QA
{
	[TestFixture]
	public abstract class InstanceConfigurationRepositoryTestBase
		: RepositoryTestBase<IInstanceConfigurationRepository>
	{
		[Test]
		public void CanGetAll()
		{
			var t1 = new TransformerDescriptor(
				"trans1", new ClassDescriptor("factTypeName", "factAssemblyName"), 0);

			var tc1 = new TransformerConfiguration("transConfig1", t1, "bla bla1");

			var t2 = new TransformerDescriptor(
				"trans2", new ClassDescriptor("factTypeName", "factAssemblyName__"),
				0, "desc");

			var tc2 = new TransformerConfiguration("transConfig2", t2, "bla bla2");
			var tc2b = new TransformerConfiguration("transConfig2b", t2, "bla bla2");

			var f1 = new RowFilterDescriptor(
				"filt1", new ClassDescriptor("rowFiltTypeName", "factAssemblyName"), 0);

			var fc1 = new RowFilterConfiguration("filterConfig2", f1, "bla bla1");

			const int testConstructorId = 1;
			var i1 = new IssueFilterDescriptor(
				"ifilt1", new ClassDescriptor("issueFilTypeName", "factAssemblyName"),
				testConstructorId);

			var ic1 = new IssueFilterConfiguration("issueFilterConfig1", i1);

			CreateSchema(t1, t2, f1, i1, tc1, tc2, tc2b, fc1, ic1);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					IList<RowFilterConfiguration> foundRowFilters =
						Repository.GetRowFilterConfigurations();
					Assert.AreEqual(1, foundRowFilters.Count);
					Assert.AreEqual(foundRowFilters[0].RowFilterDescriptor, f1);

					IList<IssueFilterConfiguration> foundIssueFilters =
						Repository.GetIssueFilterConfigurations();
					Assert.AreEqual(1, foundIssueFilters.Count);
					Assert.AreEqual(foundIssueFilters[0].IssueFilterDescriptor, i1);

					IList<TransformerConfiguration> foundTransformers =
						Repository.GetTransformerConfigurations();
					Assert.AreEqual(3, foundTransformers.Count);
					Assert.False(foundTransformers.Any(t => t.TransformerDescriptor == null));

					IList<InstanceConfiguration> foundConfigs = Repository.GetAll();

					Assert.AreEqual(5, foundConfigs.Count);
					Assert.False(foundConfigs.Any(t => t.InstanceDescriptor == null));

					InstanceConfiguration foundI1 =
						foundConfigs.Single(d => d.Name == "issueFilterConfig1");
					Assert.AreEqual(testConstructorId, foundI1.InstanceDescriptor.ConstructorId);
					Assert.AreEqual(i1.Class, foundI1.InstanceDescriptor.Class);
				});
		}

		[Test]
		public void CanGetAllTransformers()
		{
			var t1 = new TransformerDescriptor(
				"trans1", new ClassDescriptor("factTypeName", "factAssemblyName"), 0);
			var tc1 = new TransformerConfiguration("transConfig1", t1, "bla bla1");

			const int testConstructorId = 2;
			var t2 = new TransformerDescriptor(
				"trans2", new ClassDescriptor("factTypeName", "factAssemblyName__"),
				testConstructorId, "desc");
			var tc2 = new TransformerConfiguration("transConfig42", t2, "what's the meaning");
			var tc2b = new TransformerConfiguration("transConfig2b", t2, "bla bla2");

			CreateSchema(t1, t2, tc1, tc2, tc2b);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();
					IList<TransformerConfiguration> foundDescriptors =
						Repository.GetTransformerConfigurations();

					Assert.AreEqual(3, foundDescriptors.Count);

					TransformerConfiguration foundTrans =
						foundDescriptors.Single(d => d.Name == "transConfig42");
					Assert.AreEqual("what's the meaning", foundTrans.Description);
					Assert.AreEqual(testConstructorId,
					                foundTrans.TransformerDescriptor.ConstructorId);
					Assert.AreEqual(t2.Class, foundTrans.TransformerDescriptor.Class);
				});
		}
	}
}
