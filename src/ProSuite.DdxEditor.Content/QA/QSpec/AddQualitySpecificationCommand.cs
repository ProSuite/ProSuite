using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class AddQualitySpecificationCommand : AddItemCommandBase<Item>
	{
		[NotNull] private readonly IQualitySpecificationContainerItem _containerItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddQualitySpecificationCommand"/> class.
		/// </summary>
		/// <param name="item">The quality conditions item.</param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="containerItem">The container for the quality specifications</param>
		public AddQualitySpecificationCommand(
			[NotNull] Item item,
			[NotNull] IApplicationController applicationController,
			[NotNull] IQualitySpecificationContainerItem containerItem)
			: base(item, applicationController)
		{
			Assert.ArgumentNotNull(containerItem, nameof(containerItem));

			_containerItem = containerItem;
		}

		public override string Text => "Add Quality Specification";

		protected override void ExecuteCore()
		{
			_containerItem.AddNewQualitySpecificationItem();
		}
	}
}
