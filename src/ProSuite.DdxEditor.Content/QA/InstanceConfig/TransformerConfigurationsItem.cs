using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Content.QA.QSpec;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.UI.QA.BoundTableRows;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class TransformerConfigurationsItem : InstanceConfigurationsItem
	{
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static TransformerConfigurationsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.TransformOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.TransformOverlay);
		}

		public TransformerConfigurationsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                     [NotNull] IQualityConditionContainer container)
			: base(modelBuilder, "Transformer Configurations",
			       "Configured dataset transformers using one or more input datasets",
			       container) { }

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddTransformerConfigurationCommand(this, applicationController, this));
			commands.Add(new DeleteAllChildItemsCommand(this, applicationController));
		}

		#region Overrides of InstanceConfigurationsItem

		public override IEnumerable<InstanceDescriptorTableRow> GetInstanceDescriptorTableRows()
		{
			return InstanceDescriptorItemUtils.GetTransformerDescriptorTableRows(
				ModelBuilder.InstanceDescriptors);
		}

		protected override IEnumerable<InstanceConfigurationDatasetTableRow>
			GetConfigDatasetTableRows(
				DataQualityCategory category)
		{
			return InstanceConfigTableRows
				.GetInstanceConfigurationDatasetTableRows<TransformerConfiguration>(
					ModelBuilder, category);
		}

		protected override IEnumerable<InstanceConfigurationInCategoryTableRow> GetConfigTableRows(
			DataQualityCategory category)
		{
			return InstanceConfigTableRows.GetInstanceConfigs<TransformerConfiguration>(
				ModelBuilder, category);
		}

		protected override InstanceConfigurationItem CreateConfigurationItemCore(
			CoreDomainModelItemModelBuilder modelBuilder,
			InstanceConfiguration configuration,
			IInstanceConfigurationContainerItem containerItem,
			IInstanceConfigurationRepository repository)
		{
			Assert.ArgumentNotNull(configuration, nameof(configuration));

			var item =
				new InstanceConfigurationItem(modelBuilder, configuration, containerItem,
				                              repository);

			return item;
		}

		protected override InstanceConfigurationItem CreateNewItemCore(
			CoreDomainModelItemModelBuilder modelBuilder)
		{
			var transformerConfig = new TransformerConfiguration();

			return new InstanceConfigurationItem(modelBuilder, transformerConfig, this,
			                                     modelBuilder.InstanceConfigurations);
		}

		#endregion
	}
}
