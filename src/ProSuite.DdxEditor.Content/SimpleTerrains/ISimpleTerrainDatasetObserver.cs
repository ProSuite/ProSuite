using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	public interface ISimpleTerrainDatasetObserver : IViewObserver
	{
		void AddTargetClicked();

		void RemoveTargetClicked();

		void OnBoundTo(SimpleTerrainDataset entity);

		void TargetSelectionChanged();
	}
}
