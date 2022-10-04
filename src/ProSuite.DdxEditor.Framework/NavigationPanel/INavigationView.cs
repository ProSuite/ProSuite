using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public interface INavigationView
	{
		void RenderItems([NotNull] IEnumerable<Item> rootItems);

		[CanBeNull]
		INavigationObserver Observer { get; set; }

		bool GoToItem([NotNull] Item item);

		[NotNull]
		IEnumerable<I> FindItems<I>() where I : Item;

		[CanBeNull]
		I FindFirstItem<I>() where I : Item;

		[CanBeNull]
		Item FindItem([NotNull] Entity entity);
	}
}
