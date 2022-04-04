using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework
{
	public interface IApplicationController : IItemNavigation
	{
		bool CanLoadItem([NotNull] Item item);

		bool PrepareItemSelection([NotNull] Item item);

		void ReloadCurrentItem();

		void LoadItem([NotNull] Item item);

		bool HasPendingChanges { get; }

		[CanBeNull]
		Item CurrentItem { get; }

		[NotNull]
		IWin32Window Window { get; }

		bool CanDeleteItem([NotNull] Item item);

		bool DeleteItems([NotNull] IEnumerable<Item> items);

		bool DeleteItem([NotNull] Item item);

		[CanBeNull]
		T ReadInTransaction<T>([NotNull] Func<T> function);
	}
}
