using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.Geodatabase;

namespace ProSuite.DdxEditor.Content.Connections
{
	public class ConnectionProviderItem<E> : SubclassedEntityItem<E, ConnectionProvider>
		where E : ConnectionProvider
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public ConnectionProviderItem(CoreDomainModelItemModelBuilder modelBuilder,
		                              E descriptor,
		                              IRepository<ConnectionProvider> repository)
			: base(descriptor, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => Resources.ConnectionProviderItem;

		protected override bool AllowDelete => true;

		protected override void IsValidForPersistenceCore(E entity,
		                                                  Notification notification)
		{
			base.IsValidForPersistenceCore(entity, notification);

			if (entity.Name == null)
			{
				return;
			}

			ConnectionProvider other = _modelBuilder.ConnectionProviders.Get(entity.Name);

			if (other != null && other.Id != entity.Id)
			{
				notification.RegisterMessage("Name",
				                             "Another connection provider with the same name already exists",
				                             Severity.Error);
			}
		}

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		protected override void AttachPresenter(
			ICompositeEntityControl<E, IViewObserver> control)
		{
			// if needed, override and use specific subclass
			new ConnectionProviderPresenter<E>(this, control);
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<E, IViewObserver> compositeControl,
			IItemNavigation itemNavigation)
		{
			compositeControl.AddPanel(new ConnectionProviderControl<E>(this));
		}
	}
}
