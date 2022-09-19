using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public interface IItemLocator
	{
		bool CanLocate([NotNull] Entity entity);

		[CanBeNull]
		Item Locate([NotNull] Entity entity, [NotNull] IEnumerable<IItemTreeNode> rootNodes);
	}
}
