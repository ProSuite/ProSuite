using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public class CheckAttributeDependenciesCommand :
		ItemCommandBase<AttributeDependenciesItem>
	{
		public CheckAttributeDependenciesCommand(
			[NotNull] AttributeDependenciesItem attributeDependenciesItem,
			[NotNull] IApplicationController applicationController)
			: base(attributeDependenciesItem)
		{
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));
		}

		#region Overrides of CommandBase

		public override string Text => "Check Attribute Dependencies";

		protected override void ExecuteCore()
		{
			Item.CheckAttributeDependencies();
		}

		#endregion
	}
}
