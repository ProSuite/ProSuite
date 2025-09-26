using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class ObjectDatasetItem<T> : DatasetItem<T> where T : ObjectDataset
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		private readonly DdxModel _model;

		public ObjectDatasetItem(CoreDomainModelItemModelBuilder modelBuilder, T dataset,
		                         IRepository<Dataset> repository, DdxModel model)
			: base(modelBuilder, dataset, repository)
		{
			_modelBuilder = modelBuilder;
			_model = model;
		}

		protected override IEnumerable<Item> GetChildren()
		{
			// TODO: how to aggregate children?

			return _modelBuilder.GetChildren(this);
		}

		public DdxModel GetModel()
		{
			return _model;
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<T, IViewObserver> compositeControl,
			IItemNavigation itemNavigation)
		{
			base.AddEntityPanels(compositeControl, itemNavigation);

			// could attach panel-specific presenter here, if needed
			IEntityPanel<T> control = new ObjectDatasetControl<T>();

			compositeControl.AddPanel(control);
		}
	}
}
