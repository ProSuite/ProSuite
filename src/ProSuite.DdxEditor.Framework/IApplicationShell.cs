using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Search;

namespace ProSuite.DdxEditor.Framework
{
	public interface IApplicationShell : IWin32Window
	{
		IApplicationShellObserver Observer { get; set; }

		string Title { get; set; }

		bool SaveEnabled { get; set; }

		bool DiscardChangesEnabled { get; set; }

		bool GoBackEnabled { get; set; }

		bool GoForwardEnabled { get; set; }

		bool ShowOptionsVisible { get; set; }

		/// <summary>
		/// Loads the specified item into the content pane
		/// </summary>
		/// <param name="item"></param>
		/// <param name="itemNavigation"></param>
		/// <remarks>Called within a domain transaction</remarks>
		void LoadContent([NotNull] Item item, [NotNull] IItemNavigation itemNavigation);

		bool GoToItem([NotNull] Item item);

		/// <summary>
		/// Finds all items (which are already loaded) of a given type.
		/// </summary>
		/// <typeparam name="I"></typeparam>
		/// <returns></returns>
		[NotNull]
		IEnumerable<I> FindItems<I>() where I : Item;

		/// <summary>
		/// Finds the first item which (which is already loaded) of a given type.
		/// </summary>
		/// <typeparam name="I"></typeparam>
		/// <returns></returns>
		[CanBeNull]
		I FindFirstItem<I>() where I : Item;

		[CanBeNull]
		Item FindItem([NotNull] Entity entity);

		void SetCommandButtons(IEnumerable<ICommand> commands);

		void UpdateCommandButtonAppearance();

		bool ConfirmItemDeletion(
			[NotNull] ICollection<ItemDeletionCandidate> deletableItems,
			[NotNull] ICollection<ItemDeletionCandidate> nonDeletableItems);

		void AddHelpMenuItems([NotNull] IEnumerable<Help.IHelpProvider> helpProviders);

		void AddSearchMenuItems([NotNull] IEnumerable<ISearchProvider> searchProviders,
		                        [NotNull] IItemNavigation itemNavigation);
	}
}
