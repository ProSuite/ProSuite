using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public abstract class ModelsItemBase : EntityTypeItem<DdxModel>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static ModelsItemBase()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.ModelOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.ModelOverlay);
		}

		protected ModelsItemBase([NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base("Data Models", "Data models")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override bool AllowDeleteSelectedChildren => true;

		protected override bool SortChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		[NotNull]
		private IEnumerable<ModelTableRow> GetTableRows()
		{
			return _modelBuilder.Models.GetAll().Select(model => new ModelTableRow(model));
		}

		protected void AddModelItem(Item modelItem)
		{
			Assert.ArgumentNotNull(modelItem, nameof(modelItem));

			AddChild(modelItem);

			modelItem.NotifyChanged();
		}

		public IList<ModelTableRow> GetModelTableRows()
		{
			return _modelBuilder.ReadOnlyTransaction(
				() => GetTableRows().ToList());
		}
	}
}
