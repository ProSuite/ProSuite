using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.DdxEditor.Framework.ContentPanel
{
	public class ContentController : IContentObserver
	{
		private readonly IApplicationController _applicationController;

		public ContentController(IContentView view,
		                         IApplicationController applicationController)
		{
			Assert.ArgumentNotNull(view, nameof(view));
			Assert.ArgumentNotNull(applicationController, nameof(applicationController));

			_applicationController = applicationController;

			view.Observer = this;
		}
	}
}
