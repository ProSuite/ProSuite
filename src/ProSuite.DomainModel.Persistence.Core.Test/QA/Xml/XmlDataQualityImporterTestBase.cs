using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.DomainModels;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainModel.Persistence.Core.QA.Xml;

namespace ProSuite.DomainModel.Persistence.Core.Test.QA.Xml
{
	[TestFixture]
	public abstract class XmlDataQualityImporterTestBase
	{
		protected abstract DdxModel CreateModel();

		protected abstract VectorDataset CreateVectorDataset(string name);

		private IRepositoryTestController _controller;

		[OneTimeSetUp]
		public virtual void TestFixtureSetUp()
		{
			_controller = GetController();
			_controller.CheckOutLicenses();
			_controller.Configure();
		}

		[OneTimeTearDown]
		public virtual void TestFixtureTearDown()
		{
			_controller.ReleaseLicenses();
		}

		protected abstract IRepositoryTestController GetController();

		protected IUnitOfWork UnitOfWork => _controller.UnitOfWork;

		protected abstract T Resolve<T>();

		protected void CreateSchema(params Entity[] entities)
		{
			_controller.CreateSchema(entities);
		}

		[Test]
		public void CanImportSpecification()
		{
			// Create the DDX:
			DdxModel model = CreateModel();

			VectorDataset lineDataset = model.AddDataset(CreateVectorDataset("Lines"));

			CreateSchema(model);

			// Export an in-memory specification to be imported later:
			var qualitySpecification =
				XmlDataQualityImpExpUtils.GetTestQualitySpecification(lineDataset);

			var instanceConfigurationRepository = Resolve<IInstanceConfigurationRepository>();
			var instanceDescriptorRepository = Resolve<IInstanceDescriptorRepository>();
			var qualitySpecificationRepository = Resolve<IQualitySpecificationRepository>();
			var dataQualityCategoryRepository = Resolve<IDataQualityCategoryRepository>();
			var datasetRepository = Resolve<IDatasetRepository>();

			// Consider registering the IDdxModelRepository as well and use it directly 
			var modelRepository = Resolve<IModelRepository>();

			var exporter =
				new XmlDataQualityExporter(instanceConfigurationRepository,
				                           instanceDescriptorRepository,
				                           qualitySpecificationRepository,
				                           dataQualityCategoryRepository,
				                           datasetRepository,
				                           UnitOfWork,
				                           new BasicXmlWorkspaceConverter());

			string xmlFilePath = GetType().FullName + ".CanImport.qa.xml";

			const bool exportMetadata = true;
			const bool exportAllTestDescriptors = false;
			const bool exportAllCategories = true;
			const bool exportNotes = true;
			const bool exportWorkspaceConnectionStrings = false;
			const bool exportSdeConnectionFilePaths = false;
			exporter.Export(qualitySpecification, xmlFilePath,
			                exportMetadata,
			                exportWorkspaceConnectionStrings,
			                exportSdeConnectionFilePaths,
			                exportAllTestDescriptors,
			                exportAllCategories,
			                exportNotes);

			ImportTx(instanceConfigurationRepository, instanceDescriptorRepository,
			         qualitySpecificationRepository, dataQualityCategoryRepository,
			         datasetRepository, modelRepository, xmlFilePath);

			// Retrieve and check:
			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					var readSpecification = qualitySpecificationRepository.GetAll().Single();

					Assert.AreEqual(qualitySpecification.Elements.Count,
					                readSpecification.Elements.Count);

					QualityCondition condition = qualitySpecification.Elements
						.Select(e => e.QualityCondition)
						.Single(c => c.Name == "cond1");

					var readCondition = readSpecification.Elements
					                                     .Select(e => e.QualityCondition)
					                                     .Single(c => c.Name == "cond1");

					Assert.AreEqual(condition.TestDescriptor, readCondition.TestDescriptor);

					Assert.AreEqual(condition.ParameterValues.Count,
					                readCondition.ParameterValues.Count);

					var datasetParamValue =
						(DatasetTestParameterValue) condition.ParameterValues[0];
					var readDatasetParamValue =
						(DatasetTestParameterValue) readCondition.ParameterValues[0];

					Assert.AreEqual(datasetParamValue.DatasetValue,
					                readDatasetParamValue.DatasetValue);
				});
		}

		[Test]
		public void CanReImportSpecificationWithChangedConditionName()
		{
			// The first part of this test is identical to CanImportSpecification()
			// to produce existing content in the DDX that can be updated.

			// Create the DDX:
			DdxModel model = CreateModel();

			VectorDataset lineDataset = model.AddDataset(CreateVectorDataset("Lines"));

			CreateSchema(model);

			// Export an in-memory specification to be imported later:
			var qualitySpecification =
				XmlDataQualityImpExpUtils.GetTestQualitySpecification(lineDataset);

			var instanceConfigurationRepository = Resolve<IInstanceConfigurationRepository>();
			var instanceDescriptorRepository = Resolve<IInstanceDescriptorRepository>();
			var qualitySpecificationRepository = Resolve<IQualitySpecificationRepository>();
			var dataQualityCategoryRepository = Resolve<IDataQualityCategoryRepository>();
			var datasetRepository = Resolve<IDatasetRepository>();
			var modelRepository = Resolve<IModelRepository>();

			var exporter =
				new XmlDataQualityExporter(instanceConfigurationRepository,
				                           instanceDescriptorRepository,
				                           qualitySpecificationRepository,
				                           dataQualityCategoryRepository,
				                           datasetRepository,
				                           UnitOfWork, new BasicXmlWorkspaceConverter());

			string xmlFilePath = GetType().FullName + ".CanImport.qa.xml";

			const bool exportMetadata = true;
			const bool exportAllTestDescriptors = false;
			const bool exportAllCategories = true;
			const bool exportNotes = true;
			const bool exportWorkspaceConnectionStrings = false;
			const bool exportSdeConnectionFilePaths = false;
			exporter.Export(qualitySpecification, xmlFilePath,
			                exportMetadata,
			                exportWorkspaceConnectionStrings,
			                exportSdeConnectionFilePaths,
			                exportAllTestDescriptors,
			                exportAllCategories,
			                exportNotes);

			ImportTx(instanceConfigurationRepository, instanceDescriptorRepository,
			         qualitySpecificationRepository, dataQualityCategoryRepository,
			         datasetRepository, modelRepository, xmlFilePath);

			// NOW: Update the condition name in the XML and re-import
			// Import with changed condition name (but same UUID)
			QualityCondition condition = qualitySpecification.Elements
			                                                 .Select(e => e.QualityCondition)
			                                                 .Single(c => c.Name == "cond1");
			condition.Name = "cond1_newName";

			exporter.Export(qualitySpecification, xmlFilePath,
			                exportMetadata,
			                exportWorkspaceConnectionStrings,
			                exportSdeConnectionFilePaths,
			                exportAllTestDescriptors,
			                exportAllCategories,
			                exportNotes);

			ImportTx(instanceConfigurationRepository, instanceDescriptorRepository,
			         qualitySpecificationRepository, dataQualityCategoryRepository,
			         datasetRepository, modelRepository, xmlFilePath);

			// Retrieve and check:
			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					var readSpecification = qualitySpecificationRepository.GetAll().Single();

					Assert.AreEqual(qualitySpecification.Elements.Count,
					                readSpecification.Elements.Count);

					var readCondition = readSpecification.Elements
					                                     .Select(e => e.QualityCondition)
					                                     .Single(c => c.Name == "cond1_newName");

					Assert.AreEqual(condition.TestDescriptor, readCondition.TestDescriptor);

					Assert.AreEqual(condition.ParameterValues.Count,
					                readCondition.ParameterValues.Count);

					var datasetParamValue =
						(DatasetTestParameterValue) condition.ParameterValues[0];
					var readDatasetParamValue =
						(DatasetTestParameterValue) readCondition.ParameterValues[0];

					Assert.AreEqual(datasetParamValue.DatasetValue,
					                readDatasetParamValue.DatasetValue);
				});
		}

		private void ImportTx(IInstanceConfigurationRepository instanceConfigurationRepository,
		                      IInstanceDescriptorRepository instanceDescriptorRepository,
		                      IQualitySpecificationRepository qualitySpecificationRepository,
		                      IDataQualityCategoryRepository dataQualityCategoryRepository,
		                      IDatasetRepository datasetRepository,
		                      IDdxModelRepository modelRepository,
		                      string xmlFilePath)
		{
			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					XmlDataQualityImporter importer = new XmlDataQualityImporter(
						instanceConfigurationRepository,
						instanceDescriptorRepository,
						qualitySpecificationRepository,
						dataQualityCategoryRepository,
						datasetRepository,
						modelRepository,
						UnitOfWork,
						new BasicXmlWorkspaceConverter(),
						null);

					IList<QualitySpecification> importedSpecifications =
						importer.Import(xmlFilePath,
						                importType: QualitySpecificationImportType.UpdateOrAdd,
						                false, true, true);

					Assert.AreEqual(1, importedSpecifications.Count);
				});
		}
	}
}
