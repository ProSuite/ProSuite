using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.Models
{
	public interface IModelObserver : IViewObserver
	{
		void HarvestingPreviewClicked();

		void SpatialReferenceDescriptorChanged();

		void GoToSpatialReferenceClicked();

		void UserConnectionProviderChanged();

		void GoToUserConnectionClicked();
	}
}
