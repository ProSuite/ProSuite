using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.ItemLocators
{
	public class QualityConditionItemLocator : CategorizedQualityEntityBase
	{
		public override bool CanLocate(Entity entity)
		{
			return entity is QualityCondition;
		}

		protected override DataQualityCategory GetCategory(Entity entity)
		{
			var qualityCondition = (QualityCondition) entity;

			return qualityCondition.Category;
		}

		protected override IItemTreeNode GetContainerNode(DataQualityCategory category,
		                                                  IItemTreeNode categoryNode)
		{
			if (category != null && category.CanContainOnlyQualityConditions)
			{
				return categoryNode;
			}

			// search beneath QualityConditionsItem child node
			return FindNode(categoryNode,
			                node =>
				                node.IsBasedOnEntityType<QualityCondition>());
		}
	}
}