using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
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
			const string dsName0 = "SCHEMA.TLM_DATASET0";
			const string dsName1 = "SCHEMA.TLM_DATASET1";
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
		public void CanGetSpecificationsWithFiltersAndTransformersFromDatasets()
		{
			const string dsName0 = "SCHEMA.TLM_DATASET0";
			const string dsName1 = "SCHEMA.TLM_DATASET1";
			const string dsName2 = "SCHEMA.TLM_DATASET2";
			const string dsName3 = "SCHEMA.TLM_DATASET3";
			const string dsName4 = "SCHEMA.TLM_DATASET4";

			DdxModel m = CreateModel();

			Dataset ds0 = m.AddDataset(CreateVectorDataset(dsName0));
			Dataset ds1 = m.AddDataset(CreateVectorDataset(dsName1));
			Dataset ds2 = m.AddDataset(CreateVectorDataset(dsName2));
			Dataset ds3 = m.AddDataset(CreateVectorDataset(dsName3));
			Dataset ds4 = m.AddDataset(CreateVectorDataset(dsName4));

			// spec 0
			var spec0 = new QualitySpecification("spec0");

			var qaMinLength = new TestDescriptor(
				"name",
				new ClassDescriptor(
					"ProSuite.QA.Tests.QaMinLength",
					"ProSuite.QA.Tests"), 0);

			var trDissolve = new TransformerDescriptor(
				"dissolveDs1",
				new ClassDescriptor(
					"ProSuite.QA.Tests.Transformers.TrDissolve",
					"ProSuite.QA.Tests"), 0);

			var ifIntersecting = new IssueFilterDescriptor(
				"ifIntersecting",
				new ClassDescriptor(
					"ProSuite.QA.Tests.IssueFilters.IfIntersecting",
					"ProSuite.QA.Tests"), 0);

			var cond0 = new QualityCondition("cond0", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond0, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond0, "featureClass", ds0);

			var filt0 = new IssueFilterConfiguration("filt0", ifIntersecting);
			InstanceConfigurationUtils.AddParameterValue(filt0, "featureClass", ds2);
			cond0.AddIssueFilterConfiguration(filt0);

			var trans01 = new TransformerConfiguration("trans01", trDissolve);
			InstanceConfigurationUtils.AddParameterValue(trans01, "featureClass", ds3);

			var cond2 = new QualityCondition("cond2", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond2, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond2, "featureClass", trans01);

			// With dataset used as reference data
			var cond3 = new QualityCondition("cond3", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond0, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond0, "featureClass", ds3, null,
			                                             usedAsReferenceData: true);

			spec0.AddElement(cond0);
			spec0.AddElement(cond2);
			spec0.AddElement(cond3);

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
			             trans01.TransformerDescriptor, filt0.IssueFilterDescriptor,
			             trans01, filt0,
			             cond0, cond1, cond2, cond3,
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

					AssertFindSpecification(dsList, excludeHidden, rqspec0);

					Dataset rDs1 = datasets.Get(ds1.Id);
					QualitySpecification rqspec1 = Repository.Get(spec1.Id);

					dsList.Clear();
					dsList.Add(rDs1);
					AssertFindSpecification(dsList, excludeHidden, rqspec1);

					dsList.Add(rDs0);
					IList<QualitySpecification> qspecList = Repository.Get(dsList, excludeHidden);
					Assert.AreEqual(2, qspecList.Count);

					// get hidden spec also
					qspecList = Repository.Get(dsList);
					Assert.AreEqual(3, qspecList.Count);

					// get spec0 by dataset 2 -> Finds nothing because a dataset used in a filter
					// is by definition just used as a reference.
					Dataset rDs2 = datasets.Get(ds2.Id);
					dsList.Clear();
					dsList.Add(rDs2);
					AssertFindSpecification(dsList, excludeHidden, null);

					// get spec0 by dataset 3
					Dataset rDs3 = datasets.Get(ds3.Id);
					dsList.Clear();
					dsList.Add(rDs3);
					AssertFindSpecification(dsList, excludeHidden, rqspec0);

					// get no spec by dataset 4 (as reference data)
					Dataset rDs4 = datasets.Get(ds4.Id);
					dsList.Clear();
					dsList.Add(rDs4);
					AssertFindSpecification(dsList, excludeHidden, null);

					// get spec0 by full list (filter, transformer, referenced only)
					dsList.Add(rDs2);
					dsList.Add(rDs3);
					AssertFindSpecification(dsList, excludeHidden, rqspec0);
				});
		}

		[Test]
		public void CanGetSpecificationsFromTransformer()
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

			var transformerConfig =
				new TransformerConfiguration("footprint", transformerDescriptor);
			InstanceConfigurationUtils.AddParameterValue(transformerConfig, "multipatchClass", ds0);

			var cond0 = new QualityCondition("cond0", qaMinLength);
			InstanceConfigurationUtils.AddParameterValue(cond0, "limit", "0.5");
			InstanceConfigurationUtils.AddParameterValue(cond0, "featureClass", transformerConfig);

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

			CreateSchema(m, transformerDescriptor, transformerConfig, cond0.TestDescriptor,
			             cond1.TestDescriptor,
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

					// All non-hidden that contain cond0
					Assert.AreEqual(2, qspecList.Count);
					Assert.AreEqual(rqspec0, qspecList[0]);

					Dataset rDs1 = datasets.Get(ds1.Id);
					QualitySpecification rqspec1 = Repository.Get(spec1.Id);

					dsList.Clear();
					dsList.Add(rDs1);
					qspecList = Repository.Get(dsList, excludeHidden);
					Assert.AreEqual(1, qspecList.Count);
					Assert.AreEqual(rqspec0, qspecList[0]);

					dsList.Add(rDs0);
					qspecList = Repository.Get(dsList, excludeHidden);

					// All non-hidden that contain cond0 or cond1:
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

		[Test]
		public void CanSaveQualitySpecificationWithTransformer()
		{
			const string specName = "specName";
			const string specDesc = "specDesc";

			const string dsName = "SCHEMA.GEO_DATASET1";

			DdxModel model = CreateModel();

			VectorDataset ds = model.AddDataset(CreateVectorDataset(dsName));

			var specification = new QualitySpecification(specName);
			specification.Description = specDesc;

			const bool stopOnError = false;
			const bool allowErrors = true;
			const string conName = "conName";
			const string paramValue = "400";

			const string transName = "transName";

			TransformerDescriptor transformerDescriptor = new TransformerDescriptor(
				transName, new ClassDescriptor("ProSuite.QA.Tests.Transformers.TrMultilineToLine",
				                               "ProSuite.QA.Tests"), 0);

			TransformerConfiguration transformerConfig = new TransformerConfiguration(
				"transformedBB",
				transformerDescriptor);
			InstanceConfigurationUtils.AddParameterValue(transformerConfig,
			                                             "featureClass", ds);

			var condition = new QualityCondition(
				conName,
				new TestDescriptor("name",
				                   new ClassDescriptor(
					                   "ProSuite.QA.Tests.QaMinLength",
					                   "ProSuite.QA.Tests"), 0, stopOnError, allowErrors));

			// Add transformer configuration as dataset parameter value
			DatasetTestParameterValue dsParameterValue =
				InstanceConfigurationUtils.AddParameterValue(
					condition, "featureClass", transformerConfig);

			Assert.NotNull(dsParameterValue.ValueSource);
			Assert.Null(dsParameterValue.DatasetValue);

			Assert.AreEqual(dsParameterValue,
			                condition.ParameterValues.Single(v => v is DatasetTestParameterValue));

			InstanceConfigurationUtils.AddParameterValue(condition, "limit", paramValue);

			specification.AddElement(condition);

			CreateSchema(model, condition.TestDescriptor, transformerDescriptor,
			             transformerConfig, condition, specification);

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

					var value =
						(ScalarTestParameterValue) readSpecification
						                           .Elements[0].QualityCondition
						                           .ParameterValues
						                           .Single(v => v is ScalarTestParameterValue);

					// Unsolved problem: In order to know the data type the TestFactory is needed
					// -> To allow AO-independent access, the data type probably has to be encoded
					//    in the persisted sring value
					Assert.AreEqual(paramValue, value.PersistedStringValue);
					Assert.AreEqual(paramValue, value.GetDisplayValue());
					Assert.AreEqual(paramValue, value.GetValue(typeof(string)));

					var transformedDatasetParameterValue =
						(DatasetTestParameterValue) readSpecification
						                            .Elements[0].QualityCondition.ParameterValues
						                            .Single(v => v is DatasetTestParameterValue);

					Assert.IsNotNull(transformedDatasetParameterValue.ValueSource);
					Assert.IsNotNull(transformedDatasetParameterValue.ValueSource
						                 .TransformerDescriptor);
					Assert.AreEqual(
						1, transformedDatasetParameterValue.ValueSource.ParameterValues.Count);

					DatasetTestParameterValue originalDatasetParameterValue =
						transformedDatasetParameterValue.ValueSource.ParameterValues[0] as
							DatasetTestParameterValue;

					Assert.NotNull(originalDatasetParameterValue);
					Assert.AreEqual(ds, originalDatasetParameterValue.DatasetValue);
				});
		}

		private void AssertFindSpecification([NotNull] IList<Dataset> datasetList,
		                                     bool excludeHidden,
		                                     [CanBeNull] QualitySpecification expected)
		{
			int expectedCount = expected == null ? 0 : 1;

			IList<QualitySpecification> specList = Repository.Get(datasetList, excludeHidden);
			Assert.AreEqual(expectedCount, specList.Count);

			if (expectedCount == 1)
			{
				Assert.AreEqual(expected, specList[0]);
			}

			// Now with dataset ids:
			var idList = datasetList.Select(dataset => dataset.Id).ToList();

			specList = Repository.Get(idList, excludeHidden);
			Assert.AreEqual(expectedCount, specList.Count);

			if (expectedCount == 1)
			{
				Assert.AreEqual(expected, specList[0]);
			}
		}
	}
}
