using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.DatasetCategories
{
	public class AddDatasetCategoryCommand : AddItemCommandBase<DatasetCategoriesItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AddDatasetCategoryCommand"/> class.
		/// </summary>
		/// <param name="datasetCategoriesItem">The dataset categories item.</param>
		/// <param name="applicationController">The application controller.</param>
		public AddDatasetCategoryCommand(
			[NotNull] DatasetCategoriesItem datasetCategoriesItem,
			[NotNull] IApplicationController applicationController)
			: base(datasetCategoriesItem, applicationController) { }

		public override string Text => "Add Dataset Category";

		protected override void ExecuteCore()
		{
			Item.AddDatasetCategoryItem();
		}
	}
}
