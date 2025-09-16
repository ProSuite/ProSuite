using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.ItemLocators
{
	public class InstanceConfigurationItemLocator : CategorizedQualityEntityBase
	{
		public override bool CanLocate(Entity entity)
		{
			return entity is InstanceConfiguration;
		}

		protected override DataQualityCategory GetCategory(Entity entity)
		{
			var instanceConfig = (InstanceConfiguration) entity;

			return instanceConfig.Category;
		}

		protected override IItemTreeNode GetContainerNode(Entity entity,
		                                                  DataQualityCategory category,
		                                                  IItemTreeNode categoryNode)
		{
			// search beneath QualityConditionsItem (or respective item) child node
			return FindNode(categoryNode, node => node.IsBasedOnEntityType(entity));
		}
	}
}
