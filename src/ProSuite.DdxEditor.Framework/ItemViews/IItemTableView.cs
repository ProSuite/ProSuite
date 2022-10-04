using System;
using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.ItemViews
{
	public interface IItemTableView<T> where T : class
	{
		IItemTableObserver<T> Observer { get; set; }

		[NotNull]
		ICollection<T> Rows { get; }

		[NotNull]
		ICollection<T> GetSelectedRows();

		bool RemoveRow([NotNull] T row);

		event EventHandler Disposed;

		void UpdateRows();

		void ShowItemCommands([NotNull] Item item,
		                      [NotNull] IList<Item> selectedChildren,
		                      Point location);
	}
}
