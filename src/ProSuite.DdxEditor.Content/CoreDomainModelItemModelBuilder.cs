using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.AssociationEnds;
using ProSuite.DdxEditor.Content.Associations;
using ProSuite.DdxEditor.Content.AttributeDependencies;
using ProSuite.DdxEditor.Content.Attributes;
using ProSuite.DdxEditor.Content.AttributeTypes;
using ProSuite.DdxEditor.Content.Connections;
using ProSuite.DdxEditor.Content.DatasetCategories;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Content.LinearNetworks;
using ProSuite.DdxEditor.Content.Models;
using ProSuite.DdxEditor.Content.ObjectCategories;
using ProSuite.DdxEditor.Content.QA.Categories;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Content.SimpleTerrains;
using ProSuite.DdxEditor.Content.SpatialRef;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.AttributeDependencies;
using ProSuite.DomainModel.Core.AttributeDependencies.Repositories;
using ProSuite.DomainModel.Core.AttributeDependencies.Xml;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.DataModel.Xml;
using ProSuite.DomainModel.Core.Geodatabase;
using ProSuite.DomainModel.Core.Geodatabase.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.UI.QA;

namespace ProSuite.DdxEditor.Content
{
	public abstract class CoreDomainModelItemModelBuilder : ItemModelBuilderBase
	{
		[NotNull]
		protected IUnitOfWork UnitOfWork { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoreDomainModelItemModelBuilder"/> class.
		/// </summary>
		/// <param name="unitOfWork">The unit of work.</param>
		protected CoreDomainModelItemModelBuilder([NotNull] IUnitOfWork unitOfWork)
			: base(unitOfWork)
		{
			Assert.ArgumentNotNull(unitOfWork, nameof(unitOfWork));

			UnitOfWork = unitOfWork;

			ListQualityConditionsWithDataset = true;
		}

		public bool IncludeDeletedModelElements { get; set; }
		public bool IncludeQualityConditionsBasedOnDeletedDatasets { get; set; }
		public bool ListQualityConditionsWithDataset { get; set; }

		[CanBeNull]
		public virtual string QualitySpecificationReportTemplate => null;

		[CanBeNull]
		public virtual string DefaultTestDescriptorsXmlFile => null;

		[CanBeNull]
		public virtual string DefaultProcessTypesXmlFile => null;

		public abstract ISpatialReferenceDescriptorRepository SpatialReferenceDescriptors { get; }
		public abstract IConnectionProviderRepository ConnectionProviders { get; }
		public abstract IModelRepository Models { get; }
		public abstract IDatasetCategoryRepository DatasetCategories { get; }
		public abstract IAttributeTypeRepository AttributeTypes { get; }
		public abstract IDatasetRepository Datasets { get; }
		public abstract ITestDescriptorRepository TestDescriptors { get; }
		public abstract IInstanceDescriptorRepository InstanceDescriptors { get; }

		public virtual ILinearNetworkRepository LinearNetworks
		{
			get { throw new NotImplementedException(); }
		}

		public virtual ISimpleTerrainDatasetRepository SimpleTerrainDatasets
		{
			get { throw new NotImplementedException(); }
		}

		public virtual IAttributeDependencyRepository AttributeDependencies
		{
			get { throw new NotImplementedException(); }
		}

		public virtual IAttributeValueMappingRepository AttributeValueMappings
		{
			get { throw new NotImplementedException(); }
		}

		public abstract IInstanceConfigurationRepository InstanceConfigurations { get; }
		public abstract IQualityConditionRepository QualityConditions { get; }
		public abstract IQualitySpecificationRepository QualitySpecifications { get; }

		[CanBeNull]
		public virtual IDataQualityCategoryRepository DataQualityCategories => null;

		public virtual IAssociationRepository Associations
		{
			get { throw new NotImplementedException(); }
		}

		public abstract IObjectCategoryRepository ObjectCategoryRepository { get; }

		public abstract IXmlLinearNetworksExporter LinearNetworksExporter { get; }

		public abstract IXmlLinearNetworksImporter LinearNetworksImporter { get; }

		public abstract IXmlSimpleTerrainsExporter SimpleTerrainsExporter { get; }

		public abstract IXmlSimpleTerrainsImporter SimpleTerrainsImporter { get; }

		public abstract IXmlAttributeDependenciesExporter AttributeDependenciesExporter { get; }

		public abstract IXmlAttributeDependenciesImporter AttributeDependenciesImporter { get; }

		public abstract IXmlDataQualityImporter DataQualityImporter { get; }

		public abstract IXmlDataQualityExporter DataQualityExporter { get; }

		public virtual bool SupportsTransformersAndFilters =>
			Environment.Version >= new Version(6, 0);

		public virtual ICollection<string> AlgorithmAssemblyNames => new List<string>
			{
				"ProSuite.QA.Tests.dll",
				"ProSuite.QA.TestFactories.dll"
			};

		public abstract IEnumerable<Item> GetChildren([NotNull] ModelsItemBase modelItem);

		public abstract IEnumerable<Item> GetChildren<E>([NotNull] ModelItemBase<E> modelItem)
			where E : DdxModel;

		public abstract IEnumerable<Item> GetChildren(
			AttributeTypesItem datasetCategoriesItem);

		[NotNull]
		public IEnumerable<Item> GetChildren(
			[NotNull] SpatialReferenceDescriptorsItem spatialReferenceDescriptorsItem)
		{
			Assert.ArgumentNotNull(spatialReferenceDescriptorsItem,
			                       nameof(spatialReferenceDescriptorsItem));

			ISpatialReferenceDescriptorRepository repository = SpatialReferenceDescriptors;

			foreach (SpatialReferenceDescriptor entity in GetAll(repository))
			{
				yield return new SpatialReferenceDescriptorItem(this, entity, repository);
			}
		}

		public IEnumerable<Item> GetChildren(LinearNetworksItem linearNetworksItem)
		{
			Assert.ArgumentNotNull(linearNetworksItem, nameof(linearNetworksItem));

			ILinearNetworkRepository repository = LinearNetworks;

			foreach (LinearNetwork entity in GetAll(repository))
			{
				yield return new LinearNetworkItem(this, entity, repository);
			}
		}

		public IEnumerable<Item> GetChildren(SimpleTerrainDatasetsItem simpleTerrainDatasetItem)
		{
			Assert.ArgumentNotNull(simpleTerrainDatasetItem, nameof(simpleTerrainDatasetItem));

			ISimpleTerrainDatasetRepository repository = SimpleTerrainDatasets;

			foreach (var entity in GetAll(repository))
			{
				yield return new SimpleTerrainDatasetItem(this, entity, repository);
			}
		}

		[NotNull]
		public IEnumerable<Item> GetChildren(
			[NotNull] DatasetCategoriesItem datasetCategoriesItem)
		{
			Assert.ArgumentNotNull(datasetCategoriesItem, nameof(datasetCategoriesItem));

			IDatasetCategoryRepository repository = DatasetCategories;

			foreach (DatasetCategory entity in GetAll(repository))
			{
				yield return new DatasetCategoryItem(this, entity, repository);
			}
		}

		[NotNull]
		public virtual IEnumerable<Item> GetChildren([NotNull] ConnectionProvidersItem parent)
		{
			Assert.ArgumentNotNull(parent, nameof(parent));

			IConnectionProviderRepository repository = ConnectionProviders;

			foreach (ConnectionProvider entity in GetAll(repository))
			{
				yield return CreateConnectionProviderItem(entity, repository);
			}
		}

		public abstract IEnumerable<Item> GetChildren<M>(
			[NotNull] DatasetsItem<M> datasetsItem)
			where M : DdxModel;

		public abstract IEnumerable<Item> GetChildren<T>(
			[NotNull] ObjectDatasetItem<T> objectDatasetItem) where T : ObjectDataset;

		public abstract IEnumerable<Item> GetChildren(
			[NotNull] ObjectTypeItem objectTypeItem,
			[NotNull] DdxModel model);

		public abstract IEnumerable<Item> GetChildren(
			[NotNull] AssociationItem associationItem);

		public abstract IEnumerable<Item> GetChildren<T>(
			[NotNull] ObjectAttributesItem<T> parent)
			where T : ObjectDataset;

		public abstract IEnumerable<Item> GetChildren<E>(
			[NotNull] ObjectCategoriesItem<E> objectCategoriesItem,
			[NotNull] DdxModel model)
			where E : ObjectDataset;

		public abstract IEnumerable<Item> GetChildren<M>(AssociationsItem<M> item)
			where M : DdxModel;

		public abstract IEnumerable<Item> GetChildren<T>(AssociationEndsItem<T> item)
			where T : ObjectDataset;

		[NotNull]
		public IEnumerable<Item> GetChildren([NotNull] AttributeDependenciesItem parent)
		{
			Assert.ArgumentNotNull(parent, nameof(parent));

			IList<AttributeDependency> entities;
			try
			{
				entities = GetAll(AttributeDependencies);
			}
			catch // e.g. no tables in ddx; be silent here and react on parent?
			{
				yield break;
			}

			foreach (AttributeDependency entity in entities)
			{
				yield return new AttributeDependencyItem(this, entity, AttributeDependencies);
			}
		}

		[NotNull]
		public IEnumerable<Item> GetChildren(
			[NotNull] DataQualityCategoryItem item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			return ReadOnlyTransaction(
				delegate
				{
					DataQualityCategory category = Assert.NotNull(item.GetEntity());

					if (category.CanContainOnlyQualitySpecifications)
					{
						var comparer = new QualitySpecificationListComparer();

						return QualitySpecifications.Get(category)
						                            .OrderBy(q => q, comparer)
						                            .Select(
							                            qs => new QualitySpecificationItem(
								                            this, qs,
								                            item,
								                            QualitySpecifications))
						                            .Cast<Item>()
						                            .ToList();
					}

					var result = new List<Item>();

					if (category.CanContainQualitySpecifications)
					{
						result.Add(new QualitySpecificationsItem(this, item));
					}

					if (category.CanContainQualityConditions)
					{
						result.Add(new QualityConditionsItem(this, item));

						if (SupportsTransformersAndFilters)
						{
							result.Add(new TransformerConfigurationsItem(this, item));
							result.Add(new IssueFilterConfigurationsItem(this, item));
						}
					}

					if (category.CanContainSubCategories && DataQualityCategories != null)
					{
						var comparer = new DataQualityCategoryComparer();

						result.AddRange(
							category.SubCategories
							        .OrderBy(c => c, comparer)
							        .Select(c => new DataQualityCategoryItem(this, c, item,
								                DataQualityCategories))
							        .Cast<Item>());
					}

					return result;
				});
		}

		public abstract IEnumerable<Item> GetChildren([NotNull] TestDescriptorsItem parent);

		public abstract IEnumerable<Item> GetChildren<T>(
			[NotNull] InstanceDescriptorsItem<T> parent) where T : InstanceDescriptor;

		[NotNull]
		[Obsolete("No longer called")]
		public virtual IEnumerable<Item> GetChildren([NotNull] QualityConditionsItem parent)
		{
			return new List<Item>();
		}

		[NotNull]
		public ObjectSubtypeItem CreateObjectSubtypeItem(
			[NotNull] ObjectSubtype objectSubtype)
		{
			return CreateObjectSubtypeItem(objectSubtype, ObjectCategoryRepository);
		}

		public abstract IList<DependingItem> GetDependingItems(
			ObjectAttributeType objectAttributeType);

		public abstract IList<DependingItem> GetDependingItems(
			ConnectionProvider connectionProvider);

		[NotNull]
		public virtual IList<DependingItem> GetDependingItems(
			[CanBeNull] DatasetCategory datasetCategory)
		{
			return new List<DependingItem>();
		}

		[NotNull]
		public virtual IList<DependingItem> GetDependingItems([CanBeNull] Dataset dataset)
		{
			return new List<DependingItem>();
		}

		[NotNull]
		public virtual IList<DependingItem> GetDependingItems([CanBeNull] DdxModel model)
		{
			return new List<DependingItem>();
		}

		public abstract ITestParameterDatasetProvider GetTestParameterDatasetProvider();

		public virtual ISqlExpressionBuilder GetSqlExpressionBuilder()
		{
			return null;
		}

		public virtual C Resolve<C>()
		{
			// implement in project-specific subclass based on project registry
			// example: 
			//    return Registry.Resolve<C>();
			throw new NotImplementedException();
		}

		[NotNull]
		public virtual IList<DependingItem> GetDependingItems(
			[NotNull] ObjectSubtype objectSubtype)
		{
			Assert.ArgumentNotNull(objectSubtype, nameof(objectSubtype));

			return new List<DependingItem>
			       {
				       new ObjectSubtypeToObjectTypeDependingItem(objectSubtype.ObjectType,
					       objectSubtype)
			       };
		}

		public abstract IList<DependingItem> GetDependingItems(
			[CanBeNull] SpatialReferenceDescriptor spatialReferenceDescriptor);

		[NotNull]
		public virtual IList<DependingItem> GetDependingItems(
			[CanBeNull] AttributeDependency attributeDependency)
		{
			return new List<DependingItem>();
		}

		[NotNull]
		public virtual IList<DependingItem> GetDependingItems(
			[CanBeNull] QualityCondition qualityCondition)
		{
			return new List<DependingItem>();
		}

		[NotNull]
		public virtual IList<DependingItem> GetDependingItems(
			[CanBeNull] QualitySpecification qualitySpecification)
		{
			return new List<DependingItem>();
		}

		[NotNull]
		public virtual IList<DependingItem> GetDependingItems(
			[CanBeNull] DataQualityCategory category)
		{
			var result = new List<DependingItem>();

			if (category == null)
			{
				return result;
			}

			// get subcategories (recursive)
			List<DataQualityCategory> subCategories = DataQualityCategoryUtils.GetCategoriesTx(
				this, c => c.IsSubCategoryOf(category));

			var allCategories = new List<DataQualityCategory>(subCategories) { category };

			result.AddRange(
				QualitySpecifications.Get(allCategories)
				                     .Select(qs => new RequiredDependingItem(qs, qs.Name))
				                     .Cast<DependingItem>());

			result.AddRange(
				QualityConditions.Get(allCategories)
				                 .Select(qc => new RequiredDependingItem(qc, qc.Name))
				                 .Cast<DependingItem>());

			if (SupportsTransformersAndFilters)
			{
				result.AddRange(
					InstanceConfigurations.Get<TransformerConfiguration>(allCategories)
					                      .Select(tr => new RequiredDependingItem(tr, tr.Name))
					                      .Cast<DependingItem>());

				result.AddRange(
					InstanceConfigurations.Get<IssueFilterConfiguration>(allCategories)
					                      .Select(iF => new RequiredDependingItem(iF, iF.Name))
					                      .Cast<DependingItem>());
			}

			result.AddRange(
				subCategories.Select(c => new DataQualityCategoryDependingItem(
					                     c,
					                     Assert.NotNull(DataQualityCategories),
					                     UnitOfWork))
				             .Cast<DependingItem>());

			return result;
		}

		[NotNull]
		public virtual IList<DependingItem> GetDependingItems(
			[CanBeNull] TestDescriptor testDescriptor)
		{
			return new List<DependingItem>();
		}

		[NotNull]
		public virtual IList<DependingItem> GetDependingItems(
			[CanBeNull] InstanceDescriptor instanceDescriptor)
		{
			return new List<DependingItem>();
		}

		public virtual IEnumerable<DependingItem> GetDependingItems(
			IEnumerable<InstanceConfiguration> dependentConfigurations)
		{
			return new List<DependingItem>();
		}

		[NotNull]
		protected virtual ObjectSubtypeItem CreateObjectSubtypeItem(
			[NotNull] ObjectSubtype objectSubtype,
			[NotNull] IObjectCategoryRepository repository)
		{
			return new ObjectSubtypeItem(this, objectSubtype, repository);
		}

		protected IList<E> GetAll<E>(IRepository<E> repository) where E : Entity
		{
			return ReadOnlyTransaction(repository.GetAll);
		}

		protected IList<E> GetEntities<E>(Func<IList<E>> getRepositoryAction) where E : Entity
		{
			return ReadOnlyTransaction(getRepositoryAction);
		}

		[NotNull]
		protected Item CreateConnectionProviderItem(
			[NotNull] ConnectionProvider descriptor,
			[NotNull] IRepository<ConnectionProvider> repository)
		{
			// do this for all needed item types:

			var sdeDirectDbUserCP = descriptor as SdeDirectDbUserConnectionProvider;
			if (sdeDirectDbUserCP != null)
			{
				return new SdeDirectDbUserConnectionProviderItem(this, sdeDirectDbUserCP,
				                                                 repository);
			}

			var sdeDirectCP = descriptor as SdeDirectConnectionProvider;
			if (sdeDirectCP != null)
			{
				return new SdeDirectConnectionProviderItem<SdeDirectConnectionProvider>(
					this, sdeDirectCP, repository);
			}

			var filePathCP = descriptor as FilePathConnectionProviderBase;
			if (filePathCP != null)
			{
				return new FilePathConnectionProviderItem(
					this, filePathCP,
					repository);
			}

			return new ConnectionProviderItem<ConnectionProvider>(
				this, descriptor, repository);
		}

		public virtual bool CanParticipateInLinearNetwork(IVectorDataset vectorDataset)
		{
			return true;
		}

		public virtual bool CanParticipateInSimpleTerrain(IVectorDataset vectorDataset)
		{
			return true;
		}
	}
}
