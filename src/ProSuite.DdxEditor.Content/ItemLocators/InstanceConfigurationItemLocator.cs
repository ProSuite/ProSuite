using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.ItemLocators
{
	public class InstanceConfigurationItemLocator<T> : CategorizedQualityEntityBase
		where T : InstanceConfiguration
	{
		public override bool CanLocate(Entity entity)
		{
			return entity is T;
		}

		protected override DataQualityCategory GetCategory(Entity entity)
		{
			var instanceConfig = (T) entity;

			return instanceConfig.Category;
		}

		protected override IItemTreeNode GetContainerNode(DataQualityCategory category,
		                                                  IItemTreeNode categoryNode)
		{
			if (category != null && category.CanContainOnlyQualityConditions)
			{
				return categoryNode;
			}

			// search beneath QualityConditionsItem (or respective item) child node
			return FindNode(categoryNode,
			                node =>
				                node.IsBasedOnEntityType<T>());
		}
	}
}
