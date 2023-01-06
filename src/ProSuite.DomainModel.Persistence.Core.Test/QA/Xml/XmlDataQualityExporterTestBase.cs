using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainModel.Persistence.Core.QA.Xml;

namespace ProSuite.DomainModel.Persistence.Core.Test.QA.Xml
{
	[TestFixture]
	public abstract class XmlDataQualityExporterTestBase
	{
		private IRepositoryTestController _controller;

		protected abstract IRepositoryTestController GetController();

		protected IUnitOfWork UnitOfWork => _controller.UnitOfWork;

		[OneTimeSetUp]
		public virtual void TestFixtureSetUp()
		{
			_controller = GetController();
			_controller.Configure();
		}

		[OneTimeTearDown]
		public virtual void TestFixtureTearDown()
		{
			_controller.ReleaseLicenses();
		}

		[Test]
		public void CanExport()
		{
			var qualitySpecification = XmlDataQualityImpExpUtils.GetTestQualitySpecification();

			XmlDataQualityImpExpUtils.GetMockRepositories(
				qualitySpecification,
				out IInstanceConfigurationRepository instanceConfigurationRepository,
				out IInstanceDescriptorRepository instanceDescriptorRepository,
				out IQualitySpecificationRepository qualitySpecificationRepository,
				out IDataQualityCategoryRepository dataQualityCategoryRepository,
				out IDatasetRepository datasetRepository);

			var exporter =
				new XmlDataQualityExporter(instanceConfigurationRepository,
				                           instanceDescriptorRepository,
				                           qualitySpecificationRepository,
				                           dataQualityCategoryRepository,
				                           datasetRepository,
				                           UnitOfWork,
				                           new BasicXmlWorkspaceConverter());

			Export(qualitySpecification, exporter);
		}

		private void Export([NotNull] QualitySpecification qualitySpecification,
		                    [NotNull] IXmlDataQualityExporter exporter)
		{
			string xmlFilePath = GetType().FullName + ".CanExport.xml";

			if (File.Exists(xmlFilePath))
			{
				File.Delete(xmlFilePath);
			}

			const bool exportMetadata = true;
			const bool exportAllTestDescriptors = true;
			const bool exportAllCategories = true;
			const bool exportNotes = true;
			bool? exportWorkspaceConnectionStrings = null;
			const bool exportSdeConnectionFilePaths = false;
			exporter.Export(qualitySpecification, xmlFilePath,
			                exportMetadata,
			                exportWorkspaceConnectionStrings,
			                exportSdeConnectionFilePaths,
			                exportAllTestDescriptors,
			                exportAllCategories,
			                exportNotes);

			PrintContent(xmlFilePath);

			File.Delete(xmlFilePath);
		}

		private static void PrintContent(string xmlFilePath)
		{
			var msg = new StringBuilder();
			foreach (string line in File.ReadAllLines(xmlFilePath))
			{
				Console.Out.WriteLine(line);
				msg.AppendLine(line);
			}
		}
	}
}
