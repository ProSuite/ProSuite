using System.Collections.Generic;
using System.Data;
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
	public abstract class InstanceConfigurationRepositoryTestBase
		: RepositoryTestBase<IInstanceConfigurationRepository>
	{
		protected abstract DdxModel CreateModel();

		protected abstract VectorDataset CreateVectorDataset(string name);

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

		[Test]
		public void CanGetTransformersRecursively()
		{
			// Reproduces TOP-5575

			const string dsName0 = "SCHEMA.TLM_DATASET0";
			const string dsName1 = "SCHEMA.TLM_DATASET1";
			DdxModel m = CreateModel();

			Dataset ds0 = m.AddDataset(CreateVectorDataset(dsName0));
			Dataset ds1 = m.AddDataset(CreateVectorDataset(dsName1));

			// spec 0: Contains both a direct dataset reference and a transformer
			var spec0 = new QualitySpecification("spec0");

			var qaMinLength = new TestDescriptor(
				"name",
				new ClassDescriptor(
					"ProSuite.QA.Tests.QaMinLength",
					"ProSuite.QA.Tests"), 0, false, false);

			var transformerDescriptor = new TransformerDescriptor(
				"transformer",
				new ClassDescriptor("ProSuite.QA.Tests.Transformers.TrFootprint",
				                    "ProSuite.QA.Tests"), 0);

			var transformerConfig1 =
				new TransformerConfiguration("footprint1", transformerDescriptor);
			InstanceConfigurationUtils.AddParameterValue(transformerConfig1, "multipatchClass",
			                                             ds0);

			var transformerConfig2 =
				new TransformerConfiguration("footprint2", transformerDescriptor);
			InstanceConfigurationUtils.AddParameterValue(transformerConfig2, "multipatchClass",
			                                             transformerConfig1);

			var transformerConfig3 =
				new TransformerConfiguration("footprint3", transformerDescriptor);
			InstanceConfigurationUtils.AddParameterValue(transformerConfig3, "multipatchClass",
			                                             transformerConfig2);

			var transformerConfig4 =
				new TransformerConfiguration("footprint4", transformerDescriptor);
			InstanceConfigurationUtils.AddParameterValue(transformerConfig4, "multipatchClass",
			                                             transformerConfig3);

			var cond0 = new QualityCondition("cond0", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond0, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond0, "featureClass", transformerConfig4);

			var cond1 = new QualityCondition("cond1", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond1, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond1, "featureClass", ds1);

			spec0.AddElement(cond0);
			spec0.AddElement(cond1);

			// spec 1: contains only an indirect reference via transformer
			var spec1 = new QualitySpecification("spec1");
			spec1.AddElement(cond0);

			// spec 2 (hidden)
			QualitySpecification spec2 = spec1.CreateCopy();
			spec2.Hidden = true;

			CreateSchema(m, ds0, ds1, transformerDescriptor,
			             transformerConfig1, transformerConfig2, transformerConfig3,
			             transformerConfig4,
			             cond0.TestDescriptor, cond1.TestDescriptor,
			             cond1, cond0);

			IList<Dataset> foundDatasets = null;
			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					List<QualityCondition> qCondIds = new List<QualityCondition> { cond0, cond1 };
					foundDatasets = Repository.GetAllReferencedDatasets(qCondIds).ToList();
				});

			Assert.AreEqual(2, foundDatasets.Count);
			Assert.IsTrue(foundDatasets.Contains(ds0));
			Assert.IsTrue(foundDatasets.Contains(ds1));
		}

		[Test]
		public void CanGetByCategory()
		{
			var category1 = new DataQualityCategory("cat1", "c1", "Category number 1");
			var category2 = new DataQualityCategory("cat2", "c2", "Category number 2");

			var t1 = new TransformerDescriptor(
				"trans1", new ClassDescriptor("factTypeName", "factAssemblyName"), 0);

			var tc1 = new TransformerConfiguration("transConfig1", t1, "bla bla1");
			tc1.Category = category1;

			var t2 = new TransformerDescriptor(
				"trans2", new ClassDescriptor("factTypeName", "factAssemblyName__"),
				0, "desc");

			var tc2 = new TransformerConfiguration("transConfig2", t2, "bla bla2");
			tc2.Category = category2;

			var tc2b = new TransformerConfiguration("transConfig2b", t2, "bla bla2");

			const int testConstructorId = 1;
			var i1 = new IssueFilterDescriptor(
				"ifilt1", new ClassDescriptor("issueFilTypeName", "factAssemblyName"),
				testConstructorId);

			var ic1 = new IssueFilterConfiguration("issueFilterConfig1", i1);
			ic1.Category = category2;

			CreateSchema(category1, category2, t1, t2, i1, tc1, tc2, tc2b, ic1);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					IList<TransformerConfiguration> transformersInCat1 =
						Repository.Get<TransformerConfiguration>(category1);

					Assert.AreEqual(1, transformersInCat1.Count);
					Assert.AreEqual(tc1, transformersInCat1[0]);

					// Using category list:
					IList<TransformerConfiguration> transformersInCategories =
						Repository.Get<TransformerConfiguration>(new[] { category1 });
					Assert.AreEqual(1, transformersInCategories.Count);
					Assert.AreEqual(tc1, transformersInCategories[0]);

					// Get both types in category2:
					IList<InstanceConfiguration> instanceConfigsInCat2 =
						Repository.Get<InstanceConfiguration>(category2);
					Assert.AreEqual(2, instanceConfigsInCat2.Count);
					Assert.AreEqual(
						tc2, instanceConfigsInCat2.First(i => i is TransformerConfiguration));
					Assert.AreEqual(
						ic1, instanceConfigsInCat2.First(i => i is IssueFilterConfiguration));

					// Get both types using list
					instanceConfigsInCat2 =
						Repository.Get<InstanceConfiguration>(new[] { category2 });
					Assert.AreEqual(2, instanceConfigsInCat2.Count);
					Assert.AreEqual(
						tc2, instanceConfigsInCat2.First(i => i is TransformerConfiguration));
					Assert.AreEqual(
						ic1, instanceConfigsInCat2.First(i => i is IssueFilterConfiguration));

					// Get all in list of 2
					IList<InstanceConfiguration> instanceConfigsInAnyCat =
						Repository.Get<InstanceConfiguration>(new[] { category1, category2 });
					Assert.AreEqual(3, instanceConfigsInAnyCat.Count);

					Assert.IsTrue(instanceConfigsInAnyCat.Contains(tc1));
					Assert.IsTrue(instanceConfigsInAnyCat.Contains(tc2));
					Assert.IsTrue(instanceConfigsInAnyCat.Contains(ic1));
				});
		}
	}
}
