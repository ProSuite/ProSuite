using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Models;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.NavigationPanel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ItemLocators
{
	public class DatasetItemLocator : ItemLocatorBase
	{
		public override bool CanLocate(Entity entity)
		{
			return entity is Dataset;
		}

		public override Item Locate(Entity entity, IEnumerable<IItemTreeNode> rootNodes)
		{
			var dataset = (Dataset) entity;

			DdxModel model = dataset.Model;

			if (model == null)
			{
				return null;
			}

			IItemTreeNode modelsNode = FindNode(rootNodes, n => n.Item is ModelsItemBase, true);

			if (modelsNode == null)
			{
				return null;
			}

			// Matched by identifier, not by IsBasedOnEntity: a dataset or model referenced
			// from another entity can be a lazy-loading proxy of the base type, which the
			// type check of the entity items rejects.
			IItemTreeNode modelNode = FindNode(modelsNode, n => HasEntityId(n, model.Id));

			IItemTreeNode datasetsNode =
				modelNode == null
					? null
					: FindNode(modelNode, n => n.IsBasedOnEntityType<Dataset>());

			return datasetsNode == null
				       ? null
				       : FindItem(datasetsNode, n => HasEntityId(n, dataset.Id));
		}

		private static bool HasEntityId([NotNull] IItemTreeNode node, int entityId)
		{
			return node.Item is IEntityItem entityItem && entityItem.EntityId == entityId;
		}
	}
}
