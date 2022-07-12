using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public abstract class InstanceConfigurationsItem : EntityTypeItem<InstanceConfiguration>,
	                                                   IInstanceConfigurationContainerItem
	{
		[NotNull] private readonly IQualityConditionContainer _container;

		protected InstanceConfigurationsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                     [NotNull] string text,
		                                     [CanBeNull] string description,
		                                     [NotNull] IQualityConditionContainer container)
			: base(text, description)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(container, nameof(container));

			ModelBuilder = modelBuilder;
			_container = container;
		}

		[NotNull]
		protected CoreDomainModelItemModelBuilder ModelBuilder { get; }

		protected override bool AllowDeleteSelectedChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _container.GetInstanceConfigurationItems(this);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			var category = _container.Category;

			return ModelBuilder.ListQualityConditionsWithDataset
				       // ReSharper disable once RedundantCast because it is needed in C# 7.3
				       ? (Control) CreateTableControl(
					       () => GetConfigDatasetTableRows(category), itemNavigation)
				       : CreateTableControl(
					       () => GetConfigTableRows(category), itemNavigation);
		}

		[NotNull]
		public abstract IEnumerable<InstanceDescriptorTableRow> GetInstanceDescriptorTableRows();

		protected abstract IEnumerable<InstanceConfigurationDatasetTableRow>
			GetConfigDatasetTableRows([CanBeNull] DataQualityCategory category);

		protected abstract IEnumerable<InstanceConfigurationInCategoryTableRow> GetConfigTableRows(
			[CanBeNull] DataQualityCategory category);

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
			}
		}

		[NotNull]
		public InstanceConfigurationItem AddConfigurationItem(
			[NotNull] InstanceConfiguration configuration)
		{
			Assert.ArgumentNotNull(configuration, nameof(configuration));

			InstanceConfigurationItem item = CreateConfigurationItemCore(
				ModelBuilder, configuration, this, ModelBuilder.InstanceConfigurations);

			AddConfigurationItem(item);

			return item;
		}

		protected abstract InstanceConfigurationItem CreateConfigurationItemCore(
			CoreDomainModelItemModelBuilder modelBuilder, InstanceConfiguration configuration,
			IInstanceConfigurationContainerItem containerItem,
			IInstanceConfigurationRepository repository);

		void IInstanceConfigurationContainerItem.AddNewInstanceConfigurationItem()
		{
			InstanceConfigurationItem item = CreateNewItemCore(ModelBuilder);

			AddChild(item);

			item.NotifyChanged();
		}

		protected abstract InstanceConfigurationItem CreateNewItemCore(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder);

		void IInstanceConfigurationContainerItem.CreateCopy(QualityConditionItem item)
		{
			throw new NotImplementedException();

			//QualityCondition copy = _modelBuilder.ReadOnlyTransaction(
			//	() => Assert.NotNull(item.GetEntity()).CreateCopy());

			//copy.Name = string.Format("Copy of {0}", copy.Name);

			//AddConfigurationItem(new QualityConditionItem(_modelBuilder, copy, this,
			//                                                 _modelBuilder.QualityConditions));
		}

		void IInstanceConfigurationContainerItem.CreateCopy(InstanceConfigurationItem item)
		{
			InstanceConfiguration copy = ModelBuilder.ReadOnlyTransaction(
				() => Assert.NotNull(item.GetEntity()).CreateCopy());

			copy.Name = $"Copy of {copy.Name}";

			AddConfigurationItem(new InstanceConfigurationItem(
				                     ModelBuilder, copy, this,
				                     ModelBuilder.InstanceConfigurations));
		}

		public bool AssignToCategory(ICollection<QualityConditionItem> items,
		                             IWin32Window owner,
		                             out DataQualityCategory category)
		{
			throw new NotImplementedException();
		}

		bool IInstanceConfigurationContainerItem.AssignToCategory(
			ICollection<InstanceConfigurationItem> items,
			IWin32Window owner,
			out DataQualityCategory category)
		{
			if (! QualityConditionContainerUtils.AssignToCategory(items, ModelBuilder, owner,
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
			return ModelBuilder.ReadOnlyTransaction(instanceConfigurationItem.GetEntity);
		}

		private void AddConfigurationItem(InstanceConfigurationItem item)
		{
			AddChild(item);

			item.NotifyChanged();
		}
	}
}
