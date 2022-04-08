using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public class AddDataQualityCategoryCommand : AddItemCommandBase<Item>
	{
		[NotNull] private readonly IDataQualityCategoryContainerItem _containerItem;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddDataQualityCategoryCommand"/> class.
		/// </summary>
		/// <param name="item">The parent item.</param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="containerItem">The category container</param>
		public AddDataQualityCategoryCommand(
			[NotNull] Item item,
			[NotNull] IApplicationController applicationController,
			[NotNull] IDataQualityCategoryContainerItem containerItem)
			: base(item, applicationController)
		{
			Assert.ArgumentNotNull(containerItem, nameof(containerItem));

			_containerItem = containerItem;
		}

		public override string Text => "Add Category...";

		protected override void ExecuteCore()
		{
			_containerItem.AddNewDataQualityCategoryItem();
		}
	}
}
