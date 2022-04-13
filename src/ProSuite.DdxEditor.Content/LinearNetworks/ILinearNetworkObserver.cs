using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	public interface ILinearNetworkObserver : IViewObserver
	{
		void AddTargetClicked();

		void RemoveTargetClicked();

		void OnBoundTo(LinearNetwork entity);

		void TargetSelectionChanged();
	}
}