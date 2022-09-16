using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public class ModelControlPresenter<E> : EntityItemPresenter<E, IModelObserver, Model>,
	                                        IModelObserver
		where E : Model
	{
		public ModelControlPresenter(ModelItemBase<E> item, IModelView<E> view)
			: base(item, view)
		{
			view.Observer = this;

			view.FindSpatialReferenceDescriptorDelegate =
				() => item.FindSpatialReferenceDescriptor(view);

			view.FindUserConnectionProviderDelegate =
				() => item.FindUserConnectionProvider(view);

			view.FindSchemaOwnerConnectionProviderDelegate =
				() => item.FindSchemaOwnerConnectionProvider(view);

			view.FindRepositoryOwnerConnectionProviderDelegate =
				() => item.FindSdeRepositoryOwnerConnectionProvider(view);

			view.FindAttributeConfiguratorFactoryDelegate =
				() => item.FindAttributeConfiguratorFactory(view);

			view.FindDatasetListBuilderFactoryDelegate =
				() => item.FindDatasetListBuilderFactory(view);
		}

		public void HarvestingPreviewClicked()
		{
			// TODO create dummy model
			// TODO set relevant properties
			// TODO harvest (OR: call datasetlistbuilder explicitly?)
			// TODO show results in datagridview
		}
	}
}
