using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.ItemLocators
{
	public class QualitySpecificationItemLocator : CategorizedQualityEntityBase
	{
		public override bool CanLocate(Entity entity)
		{
			return entity is QualitySpecification;
		}

		protected override DataQualityCategory GetCategory(Entity entity)
		{
			var qualitySpecification = (QualitySpecification) entity;

			return qualitySpecification.Category;
		}

		protected override IItemTreeNode GetContainerNode(Entity entity,
		                                                  DataQualityCategory category,
		                                                  IItemTreeNode categoryNode)
		{
			if (category != null && category.CanContainOnlyQualitySpecifications)
			{
				return categoryNode;
			}

			// search beneath QualitySpecificationsItem child node
			return FindNode(categoryNode, node => node.IsBasedOnEntityType<QualitySpecification>());
		}
	}
}
