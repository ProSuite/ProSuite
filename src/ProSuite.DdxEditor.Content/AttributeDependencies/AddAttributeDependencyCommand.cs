using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public class AddAttributeDependencyCommand :
		AddItemCommandBase<AttributeDependenciesItem>
	{
		public AddAttributeDependencyCommand(
			[NotNull] AttributeDependenciesItem attributeDependenciesItem,
			[NotNull] IApplicationController applicationController)
			: base(attributeDependenciesItem, applicationController) { }

		public override string Text => "Add Attribute Dependency";

		protected override void ExecuteCore()
		{
			Item.AddAttributeDependencyItem();
		}
	}
}
