using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Persistence.Core.Test.QA.Xml
{
	public static class XmlDataQualityImpExpUtils
	{
		public static QualitySpecification GetTestQualitySpecification(
			[CanBeNull] VectorDataset lineDataset = null)
		{
			var cd1 = new ClassDescriptor("EsriDE.ProSuite.QA.Tests.QaMinLength",
			                              "EsriDE.ProSuite.QA.Tests");
			var t1 = new TestDescriptor("test1", cd1, 0);

			var cd2 = new ClassDescriptor("EsriDE.ProSuite.QA.Tests.QaMinArea",
			                              "EsriDE.ProSuite.QA.Tests");
			var t2 = new TestDescriptor("test2", cd2, 0);

			var q1 = new QualityCondition("cond1", t1);

			IInstanceInfo instanceInfo =
				InstanceDescriptorUtils.GetInstanceInfo(q1.TestDescriptor);
			Assert.NotNull(instanceInfo);

			if (lineDataset != null)
			{
				var datasetTestParameterValue = new DatasetTestParameterValue(
					instanceInfo.GetParameter("featureClass"), lineDataset);

				// Set DataType to null to simulate the parameter coming from persistence:
				datasetTestParameterValue.DataType = null;
				q1.AddParameterValue(datasetTestParameterValue);
			}

			var scalarTestParameterValue =
				new ScalarTestParameterValue(instanceInfo.GetParameter("limit"), "2");
			// Set DataType to null to simulate the parameter coming from persistence:
			scalarTestParameterValue.DataType = null;

			q1.AddParameterValue(scalarTestParameterValue);

			var q2 = new QualityCondition("cond2", t1);
			var q3 = new QualityCondition("cond3", t2);

			var qualitySpecification = new QualitySpecification("spec1")
			                           {
				                           Description = "qspec1 description",
				                           Url = "http://blah/blah.html",
				                           TileSize = 1000,
				                           Hidden = true
			                           };
			qualitySpecification.AddElement(q1);
			qualitySpecification.AddElement(q2, true, false);
			qualitySpecification.AddElement(q3, false, true);

			return qualitySpecification;
		}

		public static void GetMockRepositories(
			QualitySpecification qualitySpecification,
			out IInstanceConfigurationRepository instanceConfigurationRepository,
			out IInstanceDescriptorRepository instanceDescriptorRepository,
			out IQualitySpecificationRepository qualitySpecificationRepository,
			out IDataQualityCategoryRepository dataQualityCategoryRepository,
			out IDatasetRepository datasetRepository)
		{
			var configurations =
				qualitySpecification.Elements
				                    .Select(qualitySpecificationElement =>
					                            qualitySpecificationElement.QualityCondition)
				                    .Cast<InstanceConfiguration>().ToList();

			var descriptors =
				qualitySpecification.Elements
				                    .Select(qualitySpecificationElement =>
					                            qualitySpecificationElement.QualityCondition)
				                    .Select(qc => qc.TestDescriptor)
				                    .Cast<InstanceDescriptor>().ToList();

			var specifications = new List<QualitySpecification> { qualitySpecification };

			GetMockRepositories(configurations, descriptors, specifications,
			                    out instanceConfigurationRepository,
			                    out instanceDescriptorRepository,
			                    out qualitySpecificationRepository,
			                    out dataQualityCategoryRepository,
			                    out datasetRepository);
		}

		private static void GetMockRepositories(
			IList<InstanceConfiguration> configurations,
			IList<InstanceDescriptor> descriptors,
			IList<QualitySpecification> specifications,
			out IInstanceConfigurationRepository configurationsList,
			out IInstanceDescriptorRepository descriptorList,
			out IQualitySpecificationRepository specificationList,
			out IDataQualityCategoryRepository categories,
			out IDatasetRepository datasets)
		{
			configurationsList = Substitute.For<IInstanceConfigurationRepository>();
			descriptorList = Substitute.For<IInstanceDescriptorRepository>();
			specificationList = Substitute.For<IQualitySpecificationRepository>();
			datasets = Substitute.For<IDatasetRepository>();
			Substitute.For<IDdxModelRepository>();
			categories = Substitute.For<IDataQualityCategoryRepository>();

			configurationsList.GetAll().Returns(configurations);
			descriptorList.GetAll().Returns(descriptors);
			descriptorList.GetInstanceDescriptors<TestDescriptor>().Returns(
				descriptors.Where(d => d is TestDescriptor).Cast<TestDescriptor>().ToList());
			specificationList.GetAll().Returns(specifications);
			categories.GetAll().Returns(new List<DataQualityCategory>());

			//mocks.ReplayAll();
		}
	}
}