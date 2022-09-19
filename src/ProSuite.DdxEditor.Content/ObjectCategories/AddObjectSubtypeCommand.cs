using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class AddObjectSubtypeCommand : AddItemCommandBase<ObjectTypeItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AddObjectSubtypeCommand"/> class.
		/// </summary>
		/// <param name="objectTypeItem">The object type item.</param>
		/// <param name="applicationController">The application controller.</param>
		public AddObjectSubtypeCommand([NotNull] ObjectTypeItem objectTypeItem,
		                               [NotNull] IApplicationController
			                               applicationController)
			: base(objectTypeItem, applicationController) { }

		public override string Text => "Add Object Subtype";

		protected override void ExecuteCore()
		{
			Item.AddObjectSubtypeItem();
		}
	}
}
