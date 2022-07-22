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
	public class IssueFilterConfigurationsItem : InstanceConfigurationsItem
	{
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static IssueFilterConfigurationsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.IssueFilterOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.IssueFilterOverlay);
		}

		public IssueFilterConfigurationsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                     [NotNull] IQualityConditionContainer container)
			: base(modelBuilder, "Issue Filter Configurations",
			       "Configured filter algorithms to filter the issues resulting from a verified quality condition",
			       container) { }

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddIssueFilterConfigurationCommand(this, applicationController, this));
			commands.Add(new DeleteAllChildItemsCommand(this, applicationController));
		}

		#region Overrides of InstanceConfigurationsItem

		public override IEnumerable<InstanceDescriptorTableRow> GetInstanceDescriptorTableRows()
		{
			return InstanceDescriptorItemUtils.GetIssueFilterDescriptorTableRows(
				ModelBuilder.InstanceDescriptors);
		}

		protected override IEnumerable<InstanceConfigurationDatasetTableRow>
			GetConfigDatasetTableRows(DataQualityCategory category)
		{
			return QualityConditionContainerUtils
				.GetInstanceConfigurationDatasetTableRows<IssueFilterConfiguration>(
					ModelBuilder, category);
		}

		protected override IEnumerable<InstanceConfigurationInCategoryTableRow> GetConfigTableRows(
			DataQualityCategory category)
		{
			return InstanceConfigTableRows.GetInstanceConfigs<IssueFilterConfiguration>(
				ModelBuilder, category);
		}

		protected override InstanceConfigurationItem CreateConfigurationItemCore(
			CoreDomainModelItemModelBuilder modelBuilder,
			InstanceConfiguration configuration,
			IInstanceConfigurationContainerItem containerItem,
			IInstanceConfigurationRepository repository)
		{
			Assert.ArgumentNotNull(configuration, nameof(configuration));

			var item = new InstanceConfigurationItem(modelBuilder, configuration, containerItem,
			                                         repository);

			return item;
		}

		protected override InstanceConfigurationItem CreateNewItemCore(
			CoreDomainModelItemModelBuilder modelBuilder)
		{
			var issueFilterConfig = new IssueFilterConfiguration();

			return new InstanceConfigurationItem(modelBuilder, issueFilterConfig, this,
			                                     modelBuilder.InstanceConfigurations);
		}

		#endregion
	}
}
