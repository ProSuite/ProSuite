using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.Categories;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA
{
	public class QAItem : EntityTypeItem<DataQualityCategory>,
	                      IQualitySpecificationContainer,
	                      IQualityConditionContainer,
	                      IDataQualityCategoryContainerItem
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static QAItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.QAItemOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.QAItemOverlay);
		}

		public QAItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base("Data Quality", "Quality Assurance Configuration")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override IEnumerable<Item> GetChildren()
		{
			if (_modelBuilder.SupportsTransformersAndFilters)
			{
				foreach (Item item in GetChildrenDdxCore())
				{
					yield return item;
				}

				yield break;
			}

			// Legacy setup:
			yield return RegisterChild(new QualitySpecificationsItem(_modelBuilder, this));

			yield return RegisterChild(new QualityConditionsItem(_modelBuilder, this));

			yield return RegisterChild(new AlgorithmDescriptorsItem(_modelBuilder));
			//yield return RegisterChild(new TestDescriptorsItem(_modelBuilder));

			foreach (Item categoryItem in GetCategoryItems())
			{
				yield return categoryItem;
			}
		}

		private IEnumerable<Item> GetChildrenDdxCore()
		{
			yield return RegisterChild(new AlgorithmDescriptorsItem(_modelBuilder));

			yield return RegisterChild(new QualitySpecificationsItem(_modelBuilder, this));

			yield return RegisterChild(new QualityConditionsItem(_modelBuilder, this));

			yield return RegisterChild(new TransformerConfigurationsItem(_modelBuilder, this));
			yield return RegisterChild(new IssueFilterConfigurationsItem(_modelBuilder, this));

			foreach (Item categoryItem in GetCategoryItems())
			{
				yield return categoryItem;
			}
		}

		private IEnumerable<Item> GetCategoryItems()
		{
			IDataQualityCategoryRepository categoryRepository =
				_modelBuilder.DataQualityCategories;

			if (categoryRepository != null)
			{
				var comparer = new DataQualityCategoryComparer();

				foreach (DataQualityCategory category in
				         _modelBuilder.ReadOnlyTransaction(
					                      () => categoryRepository.GetTopLevelCategories())
				                      .OrderBy(c => c, comparer))
				{
					yield return RegisterChild(
						new DataQualityCategoryItem(_modelBuilder, category, this,
						                            categoryRepository));
				}
			}
		}

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			if (_modelBuilder.DataQualityCategories != null)
			{
				commands.Add(new AddDataQualityCategoryCommand(this, applicationController, this));

				commands.Add(new ImportQualitySpecificationsCommand(this, applicationController,
					             this));
				commands.Add(new ExportQualitySpecificationsCommand(this, applicationController,
					             this,
					             includeSubCategories: true));
				commands.Add(new ExportDatasetDependenciesCommand(
					             this, applicationController, this,
					             includeSubCategories: true));
			}
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController,
		                                        ICollection<Item> selectedChildren)
		{
			base.CollectCommands(commands, applicationController, selectedChildren);

			List<DataQualityCategoryItem> selectedCategoryItems =
				selectedChildren.OfType<DataQualityCategoryItem>().ToList();

			if (selectedCategoryItems.Count > 0 &&
			    selectedChildren.Count == selectedCategoryItems.Count)
			{
				// only category items are selected
				commands.Add(
					new DeleteSelectedItemsCommand(selectedCategoryItems.Cast<Item>()
						                               .ToList(),
					                               applicationController));
			}
		}

		void IDataQualityCategoryContainerItem.AddNewDataQualityCategoryItem()
		{
			var category = new DataQualityCategory(assignUuid: true);

			var item = new DataQualityCategoryItem(
				_modelBuilder, category, this,
				Assert.NotNull(_modelBuilder.DataQualityCategories));

			AddDataQualityCategoryItem(item);
		}

		bool IDataQualityCategoryContainerItem.AssignToCategory(
			DataQualityCategoryItem item, IWin32Window owner,
			out DataQualityCategory category)
		{
			if (! DataQualityCategoryContainerUtils.AssignToCategory(item,
				    _modelBuilder,
				    owner,
				    out category))
			{
				return false;
			}

			RefreshChildren();
			return true;
		}

		DataQualityCategory IDataQualityCategoryContainerItem.GetDataQualityCategory(
			DataQualityCategoryItem item)
		{
			return _modelBuilder.ReadOnlyTransaction(() => item.GetEntity());
		}

		QualitySpecificationItem IQualitySpecificationContainer.
			CreateQualitySpecificationItem(
				IQualitySpecificationContainerItem containerItem)
		{
			return new QualitySpecificationItem(
				_modelBuilder,
				new QualitySpecification(assignUuid: true),
				containerItem,
				_modelBuilder.QualitySpecifications);
		}

		public IEnumerable<QualitySpecification> GetQualitySpecifications(
			bool includeSubCategories = false)
		{
			IQualitySpecificationRepository repository = _modelBuilder.QualitySpecifications;

			return _modelBuilder.ReadOnlyTransaction(
				() => repository.Get((DataQualityCategory) null, includeSubCategories));
		}

		IEnumerable<Item> IQualitySpecificationContainer.GetQualitySpecificationItems(
			IQualitySpecificationContainerItem containerItem)
		{
			var comparer = new QualitySpecificationListComparer();

			return GetQualitySpecifications().OrderBy(q => q, comparer)
			                                 .Select(spec =>
				                                         new QualitySpecificationItem(
					                                         _modelBuilder, spec, containerItem,
					                                         _modelBuilder.QualitySpecifications))
			                                 .Cast<Item>();
		}

		public DataQualityCategory Category => null;

		QualityConditionItem IQualityConditionContainer.CreateQualityConditionItem(
			IInstanceConfigurationContainerItem containerItem)
		{
			return new QualityConditionItem(
				_modelBuilder,
				new QualityCondition(assignUuids: true),
				containerItem,
				_modelBuilder.QualityConditions);
		}

		void IQualitySpecificationContainer.ExportDatasetDependencies(
			ICollection<KeyValuePair<string, ICollection<QualitySpecification>>>
				qualitySpecificationsByFileName,
			IEnumerable<string> deletableFiles,
			ExportDatasetDependenciesOptions options)
		{
			QualitySpecificationsItemUtils.ExportDatasetDependencies(
				qualitySpecificationsByFileName, deletableFiles,
				options, _modelBuilder);
		}

		void IQualitySpecificationContainer.ExportQualitySpecifications(
			IDictionary<string, ICollection<QualitySpecification>> specificationsByFileName,
			ICollection<string> deletableFiles, bool exportMetadata,
			bool? exportWorkspaceConnections,
			bool exportConnectionFilePaths,
			bool exportAllDescriptors,
			bool exportAllCategories,
			bool exportNotes)
		{
			QualitySpecificationsItemUtils.ExportQualitySpecifications(
				specificationsByFileName,
				deletableFiles,
				exportMetadata,
				exportWorkspaceConnections,
				exportConnectionFilePaths,
				exportAllDescriptors,
				exportAllCategories,
				exportNotes,
				_modelBuilder);
		}

		void IQualitySpecificationContainer.ImportQualitySpecifications(
			string fileName,
			bool ignoreConditionsForUnknownDatasets,
			bool updateDescriptorNames,
			bool updateDescriptorProperties)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			QualitySpecificationsItemUtils.ImportQualitySpecifications(
				fileName,
				ignoreConditionsForUnknownDatasets,
				updateDescriptorNames,
				updateDescriptorProperties,
				_modelBuilder.DataQualityImporter);
		}

		private void AddDataQualityCategoryItem([NotNull] DataQualityCategoryItem item)
		{
			AddChild(item);

			item.NotifyChanged();
		}
	}
}
