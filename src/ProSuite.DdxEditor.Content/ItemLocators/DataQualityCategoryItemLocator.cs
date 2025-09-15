using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.ItemLocators
{
	public class DataQualityCategoryItemLocator : CategorizedQualityEntityBase
	{
		public override bool CanLocate(Entity entity)
		{
			return entity is DataQualityCategory;
		}

		protected override DataQualityCategory GetCategory(Entity entity)
		{
			var qualityCondition = (DataQualityCategory) entity;

			return qualityCondition.ParentCategory;
		}

		protected override IItemTreeNode GetContainerNode(Entity entity,
		                                                  DataQualityCategory category,
		                                                  IItemTreeNode categoryNode)
		{
			return categoryNode;
		}
	}
}
