using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	public class AddLinearNetworkCommand : AddItemCommandBase<LinearNetworksItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AddLinearNetworkCommand"/> class.
		/// </summary>
		/// <param name="linearNetworksItem">The linear Networks item.</param>
		/// <param name="applicationController">The application controller.</param>
		public AddLinearNetworkCommand(
			[NotNull] LinearNetworksItem linearNetworksItem,
			[NotNull] IApplicationController applicationController)
			: base(linearNetworksItem, applicationController) { }

		public override string Text => "Add Linear Network";

		protected override void ExecuteCore()
		{
			Item.AddLinearNetworkItem();
		}
	}
}
