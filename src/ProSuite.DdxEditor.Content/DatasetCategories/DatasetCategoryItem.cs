using System.Collections.Generic;
using System.Drawing;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DdxEditor.Content.DatasetCategories
{
	public class DatasetCategoryItem : SimpleEntityItem<DatasetCategory, DatasetCategory>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;

		static DatasetCategoryItem()
		{
			_image = Resources.DatasetCategoryItem;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetCategoryItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="datasetCategory">The dataset category.</param>
		/// <param name="repository">The repository.</param>
		public DatasetCategoryItem(CoreDomainModelItemModelBuilder modelBuilder,
		                           DatasetCategory datasetCategory,
		                           IRepository<DatasetCategory> repository)
			: base(datasetCategory, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		protected override bool AllowDelete => true;

		protected override IWrappedEntityControl<DatasetCategory> CreateEntityControl(
			IItemNavigation itemNavigation)
		{
			var control = new DatasetCategoryControl();
			new DatasetCategoryPresenter(control, this);
			return control;
		}

		protected override void IsValidForPersistenceCore(DatasetCategory entity,
		                                                  Notification notification)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));
			Assert.ArgumentNotNull(notification, nameof(notification));

			IDatasetCategoryRepository repository = _modelBuilder.DatasetCategories;

			// null Name already reported
			if (entity.Name != null)
			{
				// check if another entity with the same name exists
				DatasetCategory existing = repository.Get(entity.Name);

				if (existing != null && existing.Id != entity.Id)
				{
					notification.RegisterMessage("Name",
					                             "A dataset category with the same name already exists",
					                             Severity.Error);
				}
			}

			// null Abbreviation already reported
			if (entity.Abbreviation != null)
			{
				// check if another entity with the same abbreviation exists
				DatasetCategory existing = repository.GetByAbbreviation(entity.Abbreviation);

				if (existing != null && existing.Id != entity.Id)
				{
					notification.RegisterMessage("Abbreviation",
					                             "A dataset category with the same abbreviation already exists",
					                             Severity.Error);
				}
			}
		}
	}
}
