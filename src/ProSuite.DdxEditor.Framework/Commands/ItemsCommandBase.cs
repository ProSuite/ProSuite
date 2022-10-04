using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.Commands
{
	/// <summary>
	/// Base class for commands that act on a collection of items.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class ItemsCommandBase<T> : CommandBase where T : Item
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsCommandBase&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="items">The items.</param>
		protected ItemsCommandBase([NotNull] ICollection<T> items)
		{
			Assert.ArgumentNotNull(items, nameof(items));

			Items = items;
		}

		[NotNull]
		protected ICollection<T> Items { get; }
	}
}
