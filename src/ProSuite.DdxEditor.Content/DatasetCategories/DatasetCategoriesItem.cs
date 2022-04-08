using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.DdxEditor.Content.DatasetCategories
{
	public class DatasetCategoriesItem : EntityTypeItem<DatasetCategory>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static DatasetCategoriesItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.DatasetCategoryOverlay);
			_selectedImage =
				ItemUtils.GetGroupItemSelectedImage(Resources.DatasetCategoryOverlay);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetCategoriesItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		public DatasetCategoriesItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base("Dataset Categories", "Thematic dataset categories")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override bool AllowDeleteSelectedChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		protected virtual IEnumerable<DatasetCategoryTableRow> GetTableRows()
		{
			IDatasetCategoryRepository repository = _modelBuilder.DatasetCategories;
			if (repository == null)
			{
				yield break;
			}

			foreach (DatasetCategory datasetCategory in repository.GetAll())
			{
				yield return new DatasetCategoryTableRow(datasetCategory);
			}
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController
			                                        applicationController)
		{
			base.CollectCommands(commands, applicationController);

			if (_modelBuilder.DatasetCategories == null)
			{
				return;
			}

			commands.Add(new AddDatasetCategoryCommand(this, applicationController));
		}

		public DatasetCategoryItem AddDatasetCategoryItem()
		{
			var datasetCategory = new DatasetCategory();

			var item =
				new DatasetCategoryItem(_modelBuilder,
				                        datasetCategory,
				                        _modelBuilder.DatasetCategories);

			// TODO: how to add to children?
			AddChild(item);

			// - event ChildAdded? Handled by tree node? --> add to child nodes, select child node (how?)
			// TODO: set child to dirty after adding/selecting it OR: check for Dirty when selecting
			item.NotifyChanged();

			// TODO DELETION: needs to notify PARENT --> keep parent reference

			return item;
		}

		protected override bool SortChildren => true;
	}
}
