using System;
using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework
{
	public interface IItemNavigation
	{
		bool GoToItem([NotNull] Item item);

		bool GoToItem([NotNull] Entity entity);

		[CanBeNull]
		Item FindItem([NotNull] Entity entity);

		/// <summary>
		/// Finds all items (which is already loaded in the tree) of a given type.
		/// </summary>
		/// <typeparam name="I">The type of the items to find</typeparam>
		/// <returns></returns>
		[NotNull]
		IEnumerable<I> FindItems<I>() where I : Item;

		/// <summary>
		/// Finds the first item (which is already loaded in the tree) of a given type.
		/// </summary>
		/// <typeparam name="I">The type of the item to find</typeparam>
		/// <returns></returns>
		[CanBeNull]
		I FindFirstItem<I>() where I : Item;

		/// <summary>
		/// Refreshes the first item (which is already expanded in the tree) of a given type.
		/// </summary>
		/// <typeparam name="I">The type of the item to refresh</typeparam>
		/// <exception cref="InvalidOperationException">No expanded item found for the specified type</exception>
		void RefreshFirstItem<I>() where I : Item;

		/// <summary>
		/// Refreshes the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <exception cref="InvalidOperationException">There are pending changes, unable to refresh</exception>
		void RefreshItem([NotNull] Item item);

		/// <summary>
		/// Attempt to refresh the item corresponding to a given entity.
		/// </summary>
		/// <param name="entity">The entity to refresh it's item for</param>
		/// <returns><c>true</c> if the item was found and refreshed;
		/// <c>false</c> if no corresponding item was found for the entity.</returns>
		bool RefreshItem([NotNull] Entity entity);

		void ShowItemHelp([CanBeNull] string title, [NotNull] string html);

		void UpdateItemHelp([CanBeNull] string title, [NotNull] string html);
	}
}
