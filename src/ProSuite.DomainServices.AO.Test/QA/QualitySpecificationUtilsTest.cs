using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using ProSuite.Commons.DomainModels;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA;

namespace ProSuite.DomainServices.AO.Test.QA
{
	[TestFixture]
	public class QualitySpecificationUtilsTest
	{
		[Test]
		public void CanInitializeAssociatedEntitiesClassic()
		{
			const string dsName0 = "SCHEMA.TLM_DATASET0";

			DdxModel m = new MockModel("testModel");

			Dataset ds0 = m.AddDataset(new MockVectorDataset(dsName0));

			// spec 0
			var spec = new QualitySpecification("spec0");

			var qaMinLength = new TestDescriptor(
				"name",
				new ClassDescriptor(
					"ProSuite.QA.Tests.QaMinLength",
					"ProSuite.QA.Tests"), 0);

			var cond0 = new QualityCondition("cond0", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond0, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond0, "featureClass", ds0);

			spec.AddElement(cond0);

			var uowSubstitute = Substitute.For<IUnitOfWork>();

			var domainTransactions = new StatelessDomainTransactionManager(uowSubstitute);

			ICollection<Dataset> datasets =
				QualitySpecificationUtils.InitializeAssociatedEntitiesTx(
					spec, domainTransactions);

			Assert.AreEqual(1, datasets.Count);

			uowSubstitute.Received(1).Reattach(datasets);
		}

		[Test]
		public void CanInitializeAssociatedEntitiesWithIssueFilter()
		{
			const string dsName0 = "SCHEMA.TLM_DATASET0";
			const string dsName1 = "SCHEMA.TLM_DATASET1";
			DdxModel m = new MockModel("testModel");

			Dataset ds0 = m.AddDataset(new MockVectorDataset(dsName0));
			Dataset ds1 = m.AddDataset(new MockVectorDataset(dsName1));

			// spec 0
			var spec = new QualitySpecification("spec0");

			var qaMinLength = new TestDescriptor(
				"name",
				new ClassDescriptor(
					"ProSuite.QA.Tests.QaMinLength",
					"ProSuite.QA.Tests"), 0);

			var cond0 = new QualityCondition("cond0", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond0, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond0, "featureClass", ds0);

			IssueFilterConfiguration issueFilter = new IssueFilterConfiguration("testFilter",
				new IssueFilterDescriptor(
					"filterDesc",
					new ClassDescriptor("ProSuite.QA.Tests.IssueFilters.IfIntersects",
					                    "ProSuite.QA.Tests"), 0));

			InstanceConfigurationUtils.AddParameterValue(issueFilter, "featureClass", ds1);

			cond0.AddIssueFilterConfiguration(issueFilter);

			spec.AddElement(cond0);

			var uowSubstitute = Substitute.For<IUnitOfWork>();

			var domainTransactions = new StatelessDomainTransactionManager(uowSubstitute);

			ICollection<Dataset> datasets =
				QualitySpecificationUtils.InitializeAssociatedEntitiesTx(
					spec, domainTransactions);

			Assert.AreEqual(2, datasets.Count);

			uowSubstitute.Received(1).Reattach(datasets);
		}

		private class MockModel : Model
		{
			public MockModel(string name) : base(name) { }

			protected override IWorkspaceContext CreateMasterDatabaseWorkspaceContext()
			{
				return CreateDefaultMasterDatabaseWorkspaceContext();
			}

			protected override void CheckAssignSpecialDatasetCore(Dataset dataset) { }
		}

		private class MockVectorDataset : VectorDataset
		{
			public MockVectorDataset(string name) : base(name) { }
		}
	}
}