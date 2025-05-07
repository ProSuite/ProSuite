using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.InstanceConfig;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public class DataQualityCategoryItem :
		EntityItem<DataQualityCategory, DataQualityCategory>,
		IQualitySpecificationContainer,
		IQualitySpecificationContainerItem,
		IQualityConditionContainer,
		IInstanceConfigurationContainerItem,
		IDataQualityCategoryContainerItem
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		private readonly IDataQualityCategoryContainerItem _containerItem;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static DataQualityCategoryItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.DataQualityCategoryOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(
				Resources.DataQualityCategoryOverlay);
		}

		public DataQualityCategoryItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] DataQualityCategory category,
			[NotNull] IDataQualityCategoryContainerItem containerItem,
			[NotNull] IRepository<DataQualityCategory> repository)
			: base(category, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(containerItem, nameof(containerItem));

			_modelBuilder = modelBuilder;
			_containerItem = containerItem;

			SetDescription(category.Description);
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		/// <summary>
		/// Indicates that the form for editing the category properties should be loaded 
		/// the next time the item is activated.
		/// </summary>
		public bool EditOnce { get; set; }

		/// <summary>
		/// Indicates if the category is currently being edited in the category form.
		/// </summary>
		public bool IsBeingEdited { get; private set; }

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			DataQualityCategory category = Assert.NotNull(GetEntity());

			if (IsNew || EditOnce)
			{
				EditOnce = false;
				IsBeingEdited = true;
				return CreateEntityControl();
			}

			IsBeingEdited = false;

			if (category.CanContainOnlyQualitySpecifications)
			{
				return CreateTableControl(GetQualitySpecificationTableRows, itemNavigation);
			}

			return CreateTableControl(GetTableRows, itemNavigation);
		}

		protected override void OnUnloaded(EventArgs e)
		{
			base.OnUnloaded(e);

			IsBeingEdited = false;
		}

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		[NotNull]
		private IEnumerable<ItemRow> GetTableRows()
		{
			return Children.Select(c => new ItemRow(c));
		}

		private Control CreateEntityControl()
		{
			var result = new EntityControlWrapper<DataQualityCategory>();

			var control = new DataQualityCategoryControl();
			new DataQualityCategoryPresenter(this, control, FindModel);

			result.SetControl(control);
			new WrappedEntityItemPresenter<DataQualityCategory, DataQualityCategory>(this,
				result);

			return result;
		}

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override bool AllowDelete => true;

		protected override void OnSavedChanges(EventArgs e)
		{
			base.OnSavedChanges(e);

			// TODO only when allowed content was changed?
			RefreshChildren();
		}

		protected override void IsValidForPersistenceCore(DataQualityCategory entity,
		                                                  Notification notification)
		{
			base.IsValidForPersistenceCore(entity, notification);

			if (! entity.CanContainQualityConditions)
			{
				if (_modelBuilder.QualityConditions.Get(entity).Count > 0)
				{
					notification.RegisterMessage(
						"The category contains quality conditions, but does not allow them",
						Severity.Error);
				}
			}

			if (! entity.CanContainQualitySpecifications)
			{
				if (_modelBuilder.QualitySpecifications.Get(entity).Count > 0)
				{
					notification.RegisterMessage(
						"The category contains quality specifications, but does not allow them",
						Severity.Error);
				}
			}

			if (! entity.CanContainSubCategories)
			{
				if (entity.SubCategories.Count > 0)
				{
					notification.RegisterMessage(
						"The category contains subcategories, but does not allow them",
						Severity.Error);
				}
			}
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			DataQualityCategory category = _modelBuilder.ReadOnlyTransaction(() => GetEntity());

			if (category.CanContainSubCategories)
			{
				commands.Add(new AddDataQualityCategoryCommand(this, applicationController, this));
			}

			if (category.CanContainOnlyQualitySpecifications)
			{
				commands.Add(new AddQualitySpecificationCommand(this, applicationController, this));
			}

			if (category.CanContainQualitySpecifications || category.CanContainSubCategories)
			{
				commands.Add(new ExportQualitySpecificationsCommand(this, applicationController,
					             this,
					             includeSubCategories: true));
				commands.Add(new ExportDatasetDependenciesCommand(
					             this, applicationController, this,
					             includeSubCategories: true));
			}

			commands.Add(new AssignCategoryToCategoryCommand(this, _containerItem,
			                                                 applicationController));
			commands.Add(new EditDataQualityCategoryCommand(this, applicationController));
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController,
		                                        ICollection<Item> selectedChildren)
		{
			base.CollectCommands(commands, applicationController, selectedChildren);

			List<QualityConditionItem> selectedConditionItems =
				selectedChildren.OfType<QualityConditionItem>().ToList();

			if (selectedConditionItems.Count > 0)
			{
				commands.Add(new AssignQualityConditionsToCategoryCommand(
					             selectedConditionItems, this, applicationController));
				commands.Add(new DeleteSelectedItemsCommand(selectedConditionItems.Cast<Item>()
					                                            .ToList(),
				                                            applicationController));

				return;
			}

			List<QualitySpecificationItem> selectedSpecificationItems =
				selectedChildren.OfType<QualitySpecificationItem>().ToList();

			if (selectedSpecificationItems.Count > 0)
			{
				commands.Add(new AssignQualitySpecificationsToCategoryCommand(
					             selectedSpecificationItems, this, applicationController));
				commands.Add(
					new DeleteSelectedItemsCommand(selectedSpecificationItems.Cast<Item>()
						                               .ToList(),
					                               applicationController));

				return;
			}

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

		public QualitySpecificationItem CreateQualitySpecificationItem(
			IQualitySpecificationContainerItem containerItem)
		{
			DataQualityCategory category = _modelBuilder.ReadOnlyTransaction(() => GetEntity());

			var qualitySpecification = new QualitySpecification(assignUuid: true)
			                           {
				                           Category = category
			                           };

			return new QualitySpecificationItem(
				_modelBuilder, qualitySpecification,
				containerItem, _modelBuilder.QualitySpecifications);
		}

		public IEnumerable<Item> GetQualitySpecificationItems(
			IQualitySpecificationContainerItem containerItem)
		{
			var comparer = new QualitySpecificationListComparer();

			return GetQualitySpecifications()
			       .OrderBy(q => q, comparer)
			       .Select(qspec => new QualitySpecificationItem(
				               _modelBuilder,
				               qspec,
				               containerItem, _modelBuilder.QualitySpecifications))
			       .Cast<Item>();
		}

		public IEnumerable<QualitySpecification> GetQualitySpecifications(
			bool includeSubCategories = false)
		{
			return _modelBuilder.ReadOnlyTransaction(
				() => _modelBuilder.QualitySpecifications.Get(
					Assert.NotNull(GetEntity()), includeSubCategories));
		}

		void IQualitySpecificationContainerItem.AddNewQualitySpecificationItem()
		{
			AddQualitySpecificationItem(CreateQualitySpecificationItem(this));
		}

		public QualityConditionItem CreateQualityConditionItem(
			IInstanceConfigurationContainerItem containerItem)
		{
			DataQualityCategory category = _modelBuilder.ReadOnlyTransaction(() => GetEntity());

			var qualityCondition = new QualityCondition(assignUuids: true)
			                       {
				                       Category = category
			                       };

			return new QualityConditionItem(
				_modelBuilder, qualityCondition,
				containerItem, _modelBuilder.QualityConditions);
		}

		void IInstanceConfigurationContainerItem.AddNewInstanceConfigurationItem()
		{
			AddQualityConditionItem(CreateQualityConditionItem(this));
		}

		void IInstanceConfigurationContainerItem.CreateCopy(QualityConditionItem item)
		{
			QualityCondition copy = (QualityCondition) _modelBuilder.ReadOnlyTransaction(
				() => Assert.NotNull(item.GetEntity()).CreateCopy());

			copy.Name = string.Format("Copy of {0}", copy.Name);

			AddQualityConditionItem(new QualityConditionItem(_modelBuilder, copy, this,
			                                                 _modelBuilder.QualityConditions));
		}

		void IInstanceConfigurationContainerItem.CreateCopy(InstanceConfigurationItem item)
		{
			InstanceConfiguration copy = _modelBuilder.ReadOnlyTransaction(
				() => Assert.NotNull(item.GetEntity()).CreateCopy());

			copy.Name = $"Copy of {copy.Name}";

			AddInstanceConfigurationItem(new InstanceConfigurationItem(
				                             _modelBuilder, copy, this,
				                             _modelBuilder.InstanceConfigurations));
		}

		void IQualitySpecificationContainerItem.CreateCopy(QualitySpecificationItem item)
		{
			QualitySpecificationItem copy = QualitySpecificationContainerUtils.CreateCopy(
				item, _modelBuilder, this, this);

			AddQualitySpecificationItem(copy);
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
			ICollection<string> deletableFiles,
			bool exportMetadata,
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

		void IDataQualityCategoryContainerItem.AddNewDataQualityCategoryItem()
		{
			DataQualityCategory category = _modelBuilder.ReadOnlyTransaction(() => GetEntity());

			var subCategory = new DataQualityCategory(assignUuid: true);
			category.AddSubCategory(subCategory);

			var item = new DataQualityCategoryItem(
				_modelBuilder, subCategory, this,
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
			return _modelBuilder.ReadOnlyTransaction(item.GetEntity);
		}

		bool IInstanceConfigurationContainerItem.AssignToCategory(
			ICollection<QualityConditionItem> items,
			IWin32Window owner,
			out DataQualityCategory category)
		{
			if (! QualityConditionContainerUtils.AssignToCategory(items, _modelBuilder, owner,
				    out category))
			{
				return false;
			}

			RefreshChildren();
			return true;
		}

		bool IInstanceConfigurationContainerItem.AssignToCategory(
			ICollection<InstanceConfigurationItem> items,
			IWin32Window owner,
			out DataQualityCategory category)
		{
			if (! QualityConditionContainerUtils.AssignToCategory(items, _modelBuilder, owner,
				    out category))
			{
				return false;
			}

			RefreshChildren();
			return true;
		}

		InstanceConfiguration IInstanceConfigurationContainerItem.GetInstanceConfiguration<T>(
			EntityItem<T, T> instanceConfigurationItem)
		{
			return _modelBuilder.ReadOnlyTransaction(instanceConfigurationItem.GetEntity);
		}

		public bool AssignToCategory(ICollection<QualitySpecificationItem> items,
		                             IWin32Window owner,
		                             out DataQualityCategory category)
		{
			if (! QualitySpecificationContainerUtils.AssignToCategory(items,
				    _modelBuilder,
				    owner,
				    out category))
			{
				return false;
			}

			RefreshChildren();
			return true;
		}

		QualitySpecification IQualitySpecificationContainerItem.GetQualitySpecification(
			QualitySpecificationItem item)
		{
			return _modelBuilder.ReadOnlyTransaction(() => item.GetEntity());
		}

		[NotNull]
		private IEnumerable<QualitySpecificationInCategoryTableRow>
			GetQualitySpecificationTableRows()
		{
			var comparer = new QualitySpecificationListComparer();

			return GetQualitySpecifications()
			       .OrderBy(q => q, comparer)
			       .Select(qspec => new QualitySpecificationInCategoryTableRow(qspec));
		}

		[CanBeNull]
		private DdxModel FindModel([NotNull] IWin32Window owner,
		                        params ColumnDescriptor[] columns)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));

			IModelRepository models = _modelBuilder.Models;
			IList<DdxModel> allModels = _modelBuilder.ReadOnlyTransaction(() => models.GetAll());

			// alternatively use a listitem (e.g. to display icon etc.)
			// then the currently selected one could be non-selectable in the list
			var finder = new Finder<DdxModel>();
			return finder.ShowDialog(owner, allModels, columns);
		}

		private void AddQualitySpecificationItem([NotNull] QualitySpecificationItem item)
		{
			AddChild(item);

			item.NotifyChanged();
		}

		private void AddQualityConditionItem([NotNull] QualityConditionItem item)
		{
			AddChild(item);

			item.NotifyChanged();
		}

		private void AddInstanceConfigurationItem([NotNull] InstanceConfigurationItem item)
		{
			AddChild(item);

			item.NotifyChanged();
		}

		private void AddDataQualityCategoryItem([NotNull] DataQualityCategoryItem item)
		{
			AddChild(item);

			item.NotifyChanged();
		}

		#region Implementation of IInstanceConfigurationContainer

		public DataQualityCategory Category => Assert.NotNull(GetEntity());

		#endregion
	}
}
