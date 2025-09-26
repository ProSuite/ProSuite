using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public class ObjectAttributesItem<T> : EntityTypeItem<ObjectAttribute> where T : ObjectDataset
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		private readonly ObjectDatasetItem<T> _parent;

		public ObjectAttributesItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                            [NotNull] ObjectDatasetItem<T> parent)
			: base("Attributes", "Attributes of the object dataset")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(parent, nameof(parent));

			_modelBuilder = modelBuilder;
			_parent = parent;
		}

		public ObjectDataset Dataset => _parent.GetEntity();

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		[NotNull]
		private IEnumerable<ObjectAttributeTableRow> GetTableRows()
		{
			return Dataset.GetAttributes(_modelBuilder.IncludeDeletedModelElements)
			              .Select(attribute => new ObjectAttributeTableRow(attribute));
		}

		protected override bool SortChildren => false;
	}
}
