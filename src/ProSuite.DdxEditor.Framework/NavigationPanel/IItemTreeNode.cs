using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public interface IItemTreeNode
	{
		[NotNull]
		IEnumerable<IItemTreeNode> ChildNodes { get; }

		[NotNull]
		Item Item { get; }

		bool IsBasedOnEntityType<T>() where T : Entity;

		bool IsBasedOnEntityType([NotNull] Entity entity);

		bool IsBasedOnEntity([NotNull] Entity entity);
	}
}
