using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Core;

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
			
			const int testConstructorId = 1;
			var i1 = new IssueFilterDescriptor(
				"ifilt1", new ClassDescriptor("issueFilTypeName", "factAssemblyName"),
				testConstructorId);

			var ic1 = new IssueFilterConfiguration("issueFilterConfig1", i1);

			CreateSchema(t1, t2, i1, tc1, tc2, tc2b, ic1);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					IList<IssueFilterConfiguration> foundIssueFilters =
						Repository.GetIssueFilterConfigurations();
					Assert.AreEqual(1, foundIssueFilters.Count);
					Assert.AreEqual(foundIssueFilters[0].IssueFilterDescriptor, i1);

					IList<TransformerConfiguration> foundTransformers =
						Repository.GetTransformerConfigurations();
					Assert.AreEqual(3, foundTransformers.Count);
					Assert.False(foundTransformers.Any(t => t.TransformerDescriptor == null));

					IList<InstanceConfiguration> foundConfigs = Repository.GetAll();

					Assert.AreEqual(4, foundConfigs.Count);
					Assert.False(foundConfigs.Any(t => t.InstanceDescriptor == null));

					InstanceConfiguration foundI1 =
						foundConfigs.Single(d => d.Name == "issueFilterConfig1");
					Assert.AreEqual(testConstructorId, foundI1.InstanceDescriptor.ConstructorId);
					Assert.AreEqual(i1.Class, foundI1.InstanceDescriptor.Class);
				});
		}

		[Test]
		public void CanAddTransformerToParameter()
		{
			var t1 = new TransformerDescriptor(
				"trans1", new ClassDescriptor("factTypeName", "factAssemblyName"), 0);

			var tc1 = new TransformerConfiguration("transConfig1", t1, "bla bla1");

			tc1.AddParameterValue(new DatasetTestParameterValue(
				                      new TestParameter("paramName", typeof(DataTable),
				                                        "desc for trans")));

			// Currently filters are implemented via transformers. They could also be modelled as a separate concept
			// on the parameter.
			var f1 = new TransformerDescriptor(
				"filt1", new ClassDescriptor("filterTypeName", "factAssemblyName"), 0);

			var fc1 = new TransformerConfiguration("filterConfig2", f1, "bla bla1");

			CreateSchema(t1, f1, tc1, fc1);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					IList<TransformerConfiguration> foundTransformers =
						Repository.GetTransformerConfigurations();
					Assert.AreEqual(2, foundTransformers.Count);
					Assert.False(foundTransformers.Any(t => t.TransformerDescriptor == null));

					TransformerConfiguration foundTransformer = foundTransformers[0];

					var parameterValue =
						foundTransformer.ParameterValues.First() as DatasetTestParameterValue;
					Assert.NotNull(parameterValue);

					parameterValue.ValueSource = fc1;
				});

			UnitOfWork.NewTransaction(
				() =>
				{
					IList<TransformerConfiguration> foundAgain =
						Repository.GetTransformerConfigurations();

					Assert.AreEqual(2, foundAgain.Count);

					TransformerConfiguration foundTransformer = foundAgain[0];

					var parameterValue =
						foundTransformer.ParameterValues.First() as DatasetTestParameterValue;
					Assert.NotNull(parameterValue);

					TransformerConfiguration filterTransformer = parameterValue.ValueSource;

					Assert.IsNotNull(filterTransformer);
					Assert.AreEqual("filterConfig2", filterTransformer.Name);
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
