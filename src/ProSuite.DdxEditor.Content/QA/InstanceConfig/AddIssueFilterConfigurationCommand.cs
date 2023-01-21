using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class AddIssueFilterConfigurationCommand : AddItemCommandBase<Item>
	{
		private readonly IInstanceConfigurationContainerItem _containerItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddIssueFilterConfigurationCommand"/> class.
		/// </summary>
		/// <param name="item">The parent item.</param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="containerItem">The container item for the transformer configurations</param>
		public AddIssueFilterConfigurationCommand(
			[NotNull] Item item,
			[NotNull] IApplicationController applicationController,
			[NotNull] IInstanceConfigurationContainerItem containerItem)
			: base(item, applicationController)
		{
			Assert.ArgumentNotNull(containerItem, nameof(containerItem));

			_containerItem = containerItem;
		}

		public override string Text => "Add Issue Filter Configuration";

		protected override void ExecuteCore()
		{
			_containerItem.AddNewInstanceConfigurationItem();
		}
	}
}
