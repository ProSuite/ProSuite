using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Finder;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class DatasetItem<E> : SubclassedEntityItem<E, Dataset>
		where E : Dataset
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		private readonly Image _image;
		private readonly string _imageKey;

		public DatasetItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                   [NotNull] E dataset,
		                   [NotNull] IRepository<Dataset> repository)
			: base(dataset, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;

			_image = DatasetTypeImageLookup.GetImage(dataset);
			_image.Tag = DatasetTypeImageLookup.GetDefaultSortIndex(dataset);

			_imageKey = string.Format("{0}#{1}",
			                          base.ImageKey,
			                          DatasetTypeImageLookup.GetImageKey(dataset));
		}

		protected override void IsValidForPersistenceCore(E entity,
		                                                  Notification notification)
		{
			base.IsValidForPersistenceCore(entity, notification);

			if (entity.Abbreviation != null)
			{
				Dataset other =
					_modelBuilder.Datasets.GetByAbbreviation(entity.Model, entity.Abbreviation);

				if (other != null && other.Id != entity.Id)
				{
					notification.RegisterMessage("Abbreviation",
					                             "Another dataset with the same abbreviation already exists in the same model",
					                             Severity.Error);
				}
			}
		}

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		protected override string GetText(E entity)
		{
			return entity.Name;
		}

		protected override string GetDescription(E entity)
		{
			return entity.Description;
		}

		public override Image Image => _image;

		public override string ImageKey => _imageKey;

		protected override void AttachPresenter(
			ICompositeEntityControl<E, IViewObserver> control)
		{
			// if needed, override and use specific subclass
			new DatasetPresenter<E>(this, control);
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<E, IViewObserver> compositeControl,
			IItemNavigation itemNavigation)
		{
			var control = new DatasetControl<E>();
			new DatasetControlPresenter(control, FindDatasetCategory);

			compositeControl.AddPanel(control);
		}

		private DatasetCategory FindDatasetCategory(
			IWin32Window owner, params ColumnDescriptor[] columns)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));

			IDatasetCategoryRepository repository = _modelBuilder.DatasetCategories;
			if (repository == null)
			{
				return null;
			}

			IList<DatasetCategory> all = _modelBuilder.ReadOnlyTransaction(
				() => repository.GetAll());

			var finder = new Finder<DatasetCategory>();
			return finder.ShowDialog(owner, all, columns);
		}
	}
}
