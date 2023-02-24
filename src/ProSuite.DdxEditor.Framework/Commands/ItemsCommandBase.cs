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
		/// /// <param name="applicationController">The application controller.</param>
		protected ItemsCommandBase([NotNull] ICollection<T> items,
		                           [NotNull] IApplicationController applicationController)
		{
			Assert.ArgumentNotNull(items, nameof(items));
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));

			Items = items;
			ApplicationController = applicationController;
		}

		[NotNull]
		protected ICollection<T> Items { get; }

		[NotNull]
		protected IApplicationController ApplicationController { get; }
	}
}
