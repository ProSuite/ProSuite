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
using ProSuite.QA.Core;

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

			//TODO: Save to common UnitTestFolder?
			//e.g. "C:\git\Swisstopo.Topgis\bin\Debug\Swisstopo.Topgis.Persistence.Test.QA.Xml.XmlDataQualityImporterTest.CanImport.qa.xml"
			string xmlFilePath = GetType().FullName + ".CanImport.qa.xml";

			Export(qualitySpecification, xmlFilePath);

			ImportTx(xmlFilePath);

			// Retrieve and check:
			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					var readSpecification = QualitySpecificationRepository.GetAll().Single();

					Assert.AreEqual(qualitySpecification.Elements.Count,
					                readSpecification.Elements.Count);

					QualityCondition cond1 = qualitySpecification.Elements
					                                             .Select(e => e.QualityCondition)
					                                             .Single(c => c.Name == "cond1");

					var readCond1 = readSpecification.Elements
					                                 .Select(e => e.QualityCondition)
					                                 .Single(c => c.Name == "cond1");

					Assert.IsNotNull(readCond1);
					Assert.AreEqual(cond1.TestDescriptor, readCond1.TestDescriptor);
					Assert.AreEqual(cond1.ParameterValues.Count,
					                readCond1.ParameterValues.Count);

					var datasetParamValue =
						(DatasetTestParameterValue) cond1.ParameterValues[0];
					var readDatasetParamValue =
						(DatasetTestParameterValue) readCond1.ParameterValues[0];

					Assert.AreEqual(datasetParamValue.DatasetValue,
					                readDatasetParamValue.DatasetValue);

					var readCond2 = readSpecification.Elements
					                                 .Select(e => e.QualityCondition)
					                                 .Single(c => c.Name == "cond2");

					Assert.AreEqual(1, readCond2.IssueFilterConfigurations.Count);
					Assert.AreEqual("issueFilter1", readCond2.IssueFilterConfigurations[0].Name);

					TransformerConfiguration readTransformer =
						readCond2.ParameterValues[0].ValueSource;
					Assert.IsNotNull(readTransformer);
					Assert.AreEqual("transformer1", readTransformer.Name);
				});
		}

		[Test]
		public void CanUpdateSpecificationWithChangedInstanceConfigurationNames()
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

			//TODO: Save to common UnitTestFolder?
			//e.g. "C:\git\Swisstopo.Topgis\bin\Debug\Swisstopo.Topgis.Persistence.Test.QA.Xml.XmlDataQualityImporterTest.CanImport.qa.xml"
			string xmlFilePath = GetType().FullName + ".CanImport.qa.xml";

			Export(qualitySpecification, xmlFilePath);

			ImportTx(xmlFilePath);

			// NOW: Update some instance configuration names in the XML and re-import
			// Import with changed name (but same UUID)
			QualityCondition cond1 = qualitySpecification.Elements
			                                             .Select(e => e.QualityCondition)
			                                             .Single(c => c.Name == "cond1");
			cond1.Name = "cond1_newName";

			QualityCondition cond2 = qualitySpecification.Elements
			                                             .Select(e => e.QualityCondition)
			                                             .Single(c => c.Name == "cond2");
			cond2.IssueFilterConfigurations[0].Name = "issueFilter1_newName";
			cond2.ParameterValues[0].ValueSource.Name = "transformer1_newName";

			//TODO: Save to common UnitTestFolder?
			//e.g. "C:\git\Swisstopo.Topgis\bin\Debug\Swisstopo.Topgis.Persistence.Test.QA.Xml.XmlDataQualityImporterTest.CanUpdate.qa.xml"
			string xmlFilePath2 = GetType().FullName + ".CanUpdate.qa.xml";

			Export(qualitySpecification, xmlFilePath2);

			ImportTx(xmlFilePath2);

			// Retrieve and check:
			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					var readSpecification = QualitySpecificationRepository.GetAll().Single();

					Assert.AreEqual(qualitySpecification.Elements.Count,
					                readSpecification.Elements.Count);

					var readCond1 = readSpecification.Elements
					                                 .Select(e => e.QualityCondition)
					                                 .Single(c => c.Name == "cond1_newName");

					Assert.IsNotNull(readCond1);
					Assert.AreEqual(cond1.TestDescriptor, readCond1.TestDescriptor);
					Assert.AreEqual(cond1.ParameterValues.Count,
					                readCond1.ParameterValues.Count);

					var datasetParamValue =
						(DatasetTestParameterValue) cond1.ParameterValues[0];
					var readDatasetParamValue =
						(DatasetTestParameterValue) readCond1.ParameterValues[0];

					Assert.AreEqual(datasetParamValue.DatasetValue,
					                readDatasetParamValue.DatasetValue);

					var readCond2 = readSpecification.Elements
					                                 .Select(e => e.QualityCondition)
					                                 .Single(c => c.Name == "cond2");

					Assert.AreEqual(1, readCond2.IssueFilterConfigurations.Count);
					Assert.AreEqual("issueFilter1_newName",
					                readCond2.IssueFilterConfigurations[0].Name);

					TransformerConfiguration readTransformer =
						readCond2.ParameterValues[0].ValueSource;

					Assert.IsNotNull(readTransformer);
					Assert.AreEqual("transformer1_newName", readTransformer.Name);
				});
		}

		[Test]
		public void CanUpdateSpecificationWithChangedInstanceDescriptor()
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

			//TODO: Save to common UnitTestFolder?
			//e.g. "C:\git\Swisstopo.Topgis\bin\Debug\Swisstopo.Topgis.Persistence.Test.QA.Xml.XmlDataQualityImporterTest.CanImport.qa.xml"
			string xmlFilePath = GetType().FullName + ".CanImport.qa.xml";

			Export(qualitySpecification, xmlFilePath);

			ImportTx(xmlFilePath);

			// NOW: Update some instance configuration descriptors in the XML and re-import
			// Import with same UUID but change instance descriptor
			QualityCondition cond1 = qualitySpecification.Elements
			                                             .Select(e => e.QualityCondition)
			                                             .Single(c => c.Name == "cond1");

			var t1 = new TestDescriptor("test1_newDesc", cond1.TestDescriptor.TestClass, 2);

			cond1.TestDescriptor = t1;
			IInstanceInfo instanceInfo =
				InstanceDescriptorUtils.GetInstanceInfo(cond1.TestDescriptor);
			Assert.NotNull(instanceInfo);

			var scalarTestParameterValue =
				new ScalarTestParameterValue(instanceInfo.GetParameter("is3D"), true);
			// Set DataType to null to simulate the parameter coming from persistence:
			scalarTestParameterValue.DataType = null;

			cond1.AddParameterValue(scalarTestParameterValue);

			//TODO: Save to common UnitTestFolder?
			//e.g. "C:\git\Swisstopo.Topgis\bin\Debug\Swisstopo.Topgis.Persistence.Test.QA.Xml.XmlDataQualityImporterTest.CanUpdate.qa.xml"
			string xmlFilePath2 = GetType().FullName + ".CanUpdate.qa.xml";

			Export(qualitySpecification, xmlFilePath2);

			ImportTx(xmlFilePath2);

			// Retrieve and check:
			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					var readSpecification = QualitySpecificationRepository.GetAll().Single();

					Assert.AreEqual(qualitySpecification.Elements.Count,
					                readSpecification.Elements.Count);

					var readCond1 = readSpecification.Elements
					                                 .Select(e => e.QualityCondition)
					                                 .Single(c => c.Name == "cond1");

					Assert.IsNotNull(readCond1);
					Assert.AreEqual(cond1.TestDescriptor, readCond1.TestDescriptor);
					Assert.AreEqual("test1_newDesc", readCond1.TestDescriptor.Name);
					Assert.AreEqual(cond1.ParameterValues.Count,
					                readCond1.ParameterValues.Count);
					Assert.AreEqual(3, readCond1.ParameterValues.Count);
				});
		}

		#region Private members

		private void Export(QualitySpecification qualitySpecification, string xmlFilePath)
		{
			var exporter =
				new XmlDataQualityExporter(InstanceConfigurationRepository,
				                           InstanceDescriptorRepository,
				                           QualitySpecificationRepository,
				                           DataQualityCategoryRepository,
				                           DatasetRepository,
				                           UnitOfWork, new BasicXmlWorkspaceConverter());

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
		}

		private void ImportTx(string xmlFilePath)
		{
			UnitOfWork.NewTransaction(
				delegate
				{
					Assert.IsFalse(UnitOfWork.HasChanges);

					XmlDataQualityImporter importer = new XmlDataQualityImporter(
						InstanceConfigurationRepository,
						InstanceDescriptorRepository,
						QualitySpecificationRepository,
						DataQualityCategoryRepository,
						DatasetRepository,
						ModelRepository,
						UnitOfWork, new BasicXmlWorkspaceConverter(),
						null);

					IList<QualitySpecification> importedSpecifications =
						importer.Import(xmlFilePath,
						                importType: QualitySpecificationImportType.UpdateOrAdd,
						                false, true, true);

					Assert.AreEqual(1, importedSpecifications.Count);
				});
		}

		private IInstanceConfigurationRepository InstanceConfigurationRepository =>
			Resolve<IInstanceConfigurationRepository>();

		private IInstanceDescriptorRepository InstanceDescriptorRepository =>
			Resolve<IInstanceDescriptorRepository>();

		private IQualitySpecificationRepository QualitySpecificationRepository =>
			Resolve<IQualitySpecificationRepository>();

		private IDataQualityCategoryRepository DataQualityCategoryRepository =>
			Resolve<IDataQualityCategoryRepository>();

		private IDatasetRepository DatasetRepository => Resolve<IDatasetRepository>();

		private IModelRepository ModelRepository =>	Resolve<IModelRepository>();

		#endregion
	}
}
