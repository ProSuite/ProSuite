using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.QA
{
	[TestFixture]
	public abstract class QualitySpecificationRepositoryTestBase :
		RepositoryTestBase<IQualitySpecificationRepository>
	{
		protected abstract DdxModel CreateModel();

		protected abstract VectorDataset CreateVectorDataset(string name);

		[Test]
		public void CanGetSpecificationsFromDatasets()
		{
			const string dsName0 = "TOPGIS.TLM_DATASET0";
			const string dsName1 = "TOPGIS.TLM_DATASET1";
			DdxModel m = CreateModel();

			Dataset ds0 = m.AddDataset(CreateVectorDataset(dsName0));
			Dataset ds1 = m.AddDataset(CreateVectorDataset(dsName1));

			// spec 0
			var spec0 = new QualitySpecification("spec0");

			var qaMinLength = new TestDescriptor(
				"name",
				new ClassDescriptor(
					"ProSuite.QA.Tests.QaMinLength",
					"ProSuite.QA.Tests"), 0, false, false);

			var cond0 = new QualityCondition("cond0", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond0, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond0, "featureClass", ds0);

			spec0.AddElement(cond0);

			// spec 1
			var spec1 = new QualitySpecification("spec1");

			var cond1 = new QualityCondition("cond1", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond1, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond1, "featureClass", ds1);

			spec1.AddElement(cond1);

			// spec 2 (hidden)
			QualitySpecification spec2 = spec1.CreateCopy();
			spec2.Hidden = true;

			CreateSchema(m, cond0.TestDescriptor, cond1.TestDescriptor,
			             cond0, cond1,
			             spec0, spec1, spec2);

			const bool excludeHidden = true;

			UnitOfWork.NewTransaction(
				delegate
				{
					// reread spec

					var datasets = Resolve<IDatasetRepository>();

					var dsList = new List<Dataset>();

					Dataset rDs0 = datasets.Get(ds0.Id);
					QualitySpecification rqspec0 = Repository.Get(spec0.Id);

					dsList.Add(rDs0);
					IList<QualitySpecification> qspecList = Repository.Get(dsList, excludeHidden);
					Assert.AreEqual(1, qspecList.Count);
					Assert.AreEqual(rqspec0, qspecList[0]);

					Dataset rDs1 = datasets.Get(ds1.Id);
					QualitySpecification rqspec1 = Repository.Get(spec1.Id);

					dsList.Clear();
					dsList.Add(rDs1);
					qspecList = Repository.Get(dsList, excludeHidden);
					Assert.AreEqual(1, qspecList.Count);
					Assert.AreEqual(rqspec1, qspecList[0]);

					dsList.Add(rDs0);
					qspecList = Repository.Get(dsList, excludeHidden);
					Assert.AreEqual(2, qspecList.Count);

					// get hidden spec also
					qspecList = Repository.Get(dsList);
					Assert.AreEqual(3, qspecList.Count);
				});
		}

		[Test]
		public void CanReorderElements()
		{
			var specification = new QualitySpecification("specName");

			const string conName1 = "conName1";
			const string conName2 = "conName2";

			var condition1 = new QualityCondition(
				conName1,
				new TestDescriptor("name1", new ClassDescriptor("type1", "asm1"), true, true));
			var condition2 = new QualityCondition(
				conName2,
				new TestDescriptor("name2", new ClassDescriptor("type2", "asm2"), false, false));

			specification.AddElement(condition1);
			specification.AddElement(condition2);

			CreateSchema(condition1.TestDescriptor,
			             condition2.TestDescriptor, condition1, condition2,
			             specification);

			QualitySpecification readSpecification = null;

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					// reread spec
					readSpecification = Repository.Get(specification.Id);

					Assert.IsNotNull(readSpecification);

					Assert.AreEqual(conName1,
					                readSpecification.Elements[0].QualityCondition.Name);
					Assert.AreEqual(conName2,
					                readSpecification.Elements[1].QualityCondition.Name);

					// move first to last
					readSpecification.MoveElementTo(0, 1);
				});

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					// reread spec
					readSpecification = Repository.Get(specification.Id);

					// assert that order is switched
					Assert.AreEqual(conName2,
					                readSpecification.Elements[0].QualityCondition.Name);
					Assert.AreEqual(conName1,
					                readSpecification.Elements[1].QualityCondition.Name);

					// move last to first
					readSpecification.MoveElementTo(1, 0);
				});

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					// reread spec
					readSpecification = Repository.Get(specification.Id);

					// assert original order
					Assert.AreEqual(conName1,
					                readSpecification.Elements[0].QualityCondition.Name);
					Assert.AreEqual(conName2,
					                readSpecification.Elements[1].QualityCondition.Name);
				});
		}

		[Test]
		public void CanSaveQualitySpecification()
		{
			const string specName = "specName";
			const string specDesc = "specDesc";

			var specification = new QualitySpecification(specName);
			specification.Description = specDesc;

			const bool stopOnError = false;
			const bool allowErrors = true;
			const string conName = "conName";
			const string paramValue = "400";

			var condition = new QualityCondition(
				conName,
				new TestDescriptor("name",
				                   new ClassDescriptor(
					                   "ProSuite.QA.Tests.QaMinLength",
					                   "ProSuite.QA.Tests"), 0, stopOnError, allowErrors));
			InstanceConfigurationUtils.AddParameterValue(condition, "limit", paramValue);
			specification.AddElement(condition);

			CreateSchema(condition.TestDescriptor, condition, specification);

			UnitOfWork.NewTransaction(
				delegate
				{
					QualitySpecification readSpecification =
						Repository.Get(specification.Id);

					Assert.IsNotNull(readSpecification);
					Assert.AreNotSame(readSpecification, specification);
					Assert.AreEqual(specName, readSpecification.Name);
					Assert.AreEqual(specDesc, readSpecification.Description);
					Assert.AreEqual(1, readSpecification.Elements.Count);
					Assert.AreEqual(conName,
					                readSpecification.Elements[0].QualityCondition.Name);
					var value = (ScalarTestParameterValue) readSpecification
					                                       .Elements[0].QualityCondition
					                                       .ParameterValues[0];

					// Unsolved problem: In order to know the data type the TestFactory is needed
					// -> To allow AO-independent access, the data type probably has to be encoded
					//    in the persisted sring value
					Assert.AreEqual(paramValue, value.PersistedStringValue);
					Assert.AreEqual(paramValue, value.GetDisplayValue());
					Assert.AreEqual(paramValue, value.GetValue(typeof(string)));
				});
		}
	}
}
