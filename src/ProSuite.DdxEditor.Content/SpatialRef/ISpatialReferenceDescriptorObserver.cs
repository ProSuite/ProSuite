using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.SpatialRef
{
	public interface ISpatialReferenceDescriptorObserver : IViewObserver
	{
		void GetFromDatasetClicked();

		void GetFromWorkspaceClicked();

		void CopyClicked();
	}
}
