using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.Commands
{
	/// <summary>
	/// Base class for commands that act on a single item.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class ItemCommandBase<T> : CommandBase where T : Item
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ItemCommandBase&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="applicationController">The application controller.</param>
		protected ItemCommandBase([NotNull] T item,
		                          [NotNull] IApplicationController applicationController)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));

			Item = item;
			ApplicationController = applicationController;
		}

		[NotNull]
		protected T Item { get; }

		[NotNull]
		protected IApplicationController ApplicationController { get; }
	}
}
