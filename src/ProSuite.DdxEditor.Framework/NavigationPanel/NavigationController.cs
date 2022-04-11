using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	public class NavigationController : INavigationObserver
	{
		private readonly IApplicationController _applicationController;

		/// <summary>
		/// Initializes a new instance of the <see cref="NavigationController"/> class.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="applicationController">The application controller.</param>
		/// <param name="modelBuilder">The model builder.</param>
		public NavigationController([NotNull] INavigationView view,
		                            [NotNull] IApplicationController applicationController,
		                            [NotNull] IItemModelBuilder modelBuilder)
		{
			Assert.ArgumentNotNull(view, nameof(view));
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_applicationController = applicationController;

			view.RenderItems(modelBuilder.GetRootItems());
			view.Observer = this;
		}

		#region INavigationObserver Members

		public void HandleItemSelected(Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));
			Assert.True(_applicationController.CanLoadItem(item),
			            "Unable to change the selection");

			_applicationController.LoadItem(item);
		}

		public bool PrepareItemSelection(Item item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			return _applicationController.PrepareItemSelection(item);
		}

		#endregion
	}
}
