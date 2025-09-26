using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public class ModelControlPresenter<E> : EntityItemPresenter<E, IModelObserver, DdxModel>,
	                                        IModelObserver
		where E : DdxModel
	{
		private readonly IModelView<E> _view;
		private readonly IItemNavigation _itemNavigation;

		public ModelControlPresenter([NotNull] ModelItemBase<E> item, [NotNull] IModelView<E> view,
		                             [NotNull] IItemNavigation itemNavigation)
			: base(item, view)
		{
			Assert.ArgumentNotNull(itemNavigation, nameof(itemNavigation));

			_view = view;
			_itemNavigation = itemNavigation;

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

			RenderView(item);
		}

		public void HarvestingPreviewClicked()
		{
			// TODO create dummy model
			// TODO set relevant properties
			// TODO harvest (OR: call datasetlistbuilder explicitly?)
			// TODO show results in datagridview
		}

		public void SpatialReferenceDescriptorChanged()
		{
			RenderView(Item);
		}

		public void GoToSpatialReferenceClicked()
		{
			DdxModel model = Assert.NotNull(Item.GetEntity());

			if (model.SpatialReferenceDescriptor != null)
			{
				_itemNavigation.GoToItem(model.SpatialReferenceDescriptor);
			}
		}

		public void UserConnectionProviderChanged()
		{
			RenderView(Item);
		}

		public void GoToUserConnectionClicked()
		{
			DdxModel model = Assert.NotNull(Item.GetEntity());

			if (model.UserConnectionProvider != null)
			{
				_itemNavigation.GoToItem(model.UserConnectionProvider);
			}
		}

		private void RenderView(EntityItem<E, DdxModel> item)
		{
			DdxModel entity = item.GetEntity();

			_view.GoToSpatialReferenceEnabled = entity?.SpatialReferenceDescriptor != null;
			_view.GoToUserConnectionEnabled = entity?.UserConnectionProvider != null;
		}
	}
}
