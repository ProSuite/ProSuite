using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainModel.Core.QA.Xml;

namespace ProSuite.DomainModel.Persistence.Core.QA.Xml
{
	public abstract class XmlDataQualityExchangeBase
	{
		protected XmlDataQualityExchangeBase(
			[NotNull] IInstanceConfigurationRepository instanceConfigurations,
			[NotNull] IInstanceDescriptorRepository instanceDescriptors,
			[NotNull] IQualitySpecificationRepository qualitySpecifications,
			[CanBeNull] IDataQualityCategoryRepository categories,
			[NotNull] IDatasetRepository datasets,
			[NotNull] IUnitOfWork unitOfWork,
			[NotNull] IXmlWorkspaceConverter workspaceConverter)
		{
			Assert.ArgumentNotNull(instanceConfigurations, nameof(instanceConfigurations));
			Assert.ArgumentNotNull(instanceDescriptors, nameof(instanceDescriptors));
			Assert.ArgumentNotNull(qualitySpecifications, nameof(qualitySpecifications));
			Assert.ArgumentNotNull(datasets, nameof(datasets));
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			InstanceConfigurations = instanceConfigurations;
			InstanceDescriptors = instanceDescriptors;
			QualitySpecifications = qualitySpecifications;
			Datasets = datasets;
			UnitOfWork = unitOfWork;
			Categories = categories;

			WorkspaceConverter = workspaceConverter;
		}

		[NotNull]
		protected IXmlWorkspaceConverter WorkspaceConverter { get; }

		[NotNull]
		protected IInstanceConfigurationRepository InstanceConfigurations { get; }

		[NotNull]
		protected IInstanceDescriptorRepository InstanceDescriptors { get; }

		[NotNull]
		protected IQualitySpecificationRepository QualitySpecifications { get; }

		[CanBeNull]
		public IDataQualityCategoryRepository Categories { get; }

		[NotNull]
		protected IDatasetRepository Datasets { get; }

		[NotNull]
		protected IUnitOfWork UnitOfWork { get; }

		protected void Reattach([NotNull] IEnumerable<QualitySpecification> qualitySpecifications)
		{
			foreach (QualitySpecification qualitySpecification in qualitySpecifications)
			{
				if (qualitySpecification.IsPersistent)
				{
					UnitOfWork.Reattach(qualitySpecification);
				}
			}
		}

		protected IList<InstanceDescriptor> GetAllInstanceDescriptors()
		{
			if (! InstanceDescriptors.SupportsTransformersAndFilters)
			{
				// Prevent missing table exception:
				return InstanceDescriptors.GetInstanceDescriptors<TestDescriptor>()
				                          .Cast<InstanceDescriptor>()
				                          .ToList();
			}

			return InstanceDescriptors.GetAll();
		}

		protected IList<InstanceConfiguration> GetAllInstanceConfigurations()
		{
			// If InstanceDescriptors do not support transformers, neither does the InstanceConfiguration repo.
			if (! InstanceDescriptors.SupportsTransformersAndFilters)
			{
				// Prevent missing table exception:
				return InstanceConfigurations.GetInstanceConfigurations<QualityCondition>()
				                             .Cast<InstanceConfiguration>()
				                             .ToList();
			}

			return InstanceConfigurations.GetAll();
		}
	}
}
