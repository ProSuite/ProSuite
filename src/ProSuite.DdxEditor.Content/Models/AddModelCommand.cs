using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.Models
{
	public abstract class AddModelCommand : AddItemCommandBase<ModelsItemBase>
	{
		private readonly string _text;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddModelCommand"/> class.
		/// </summary>
		/// <param name="parentItem">The parent item.</param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="text">The text.</param>
		protected AddModelCommand([NotNull] ModelsItemBase parentItem,
		                          [NotNull] IApplicationController applicationController,
		                          [NotNull] string text)
			: base(parentItem, applicationController)
		{
			Assert.ArgumentNotNullOrEmpty(text, nameof(text));
			_text = text;
		}

		public override string Text => _text;
	}
}
