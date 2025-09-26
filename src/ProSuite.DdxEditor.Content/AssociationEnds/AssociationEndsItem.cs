using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.AssociationEnds
{
	public class AssociationEndsItem<T> : EntityTypeItem<T> where T : ObjectDataset
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		private readonly ObjectDatasetItem<T> _parent;

		public AssociationEndsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                           [NotNull] ObjectDatasetItem<T> parent)
			: base("Association Ends", "Properties for ends of associations")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(parent, nameof(parent));

			_modelBuilder = modelBuilder;
			_parent = parent;
		}

		public ObjectDataset ObjectDataset => _parent.GetEntity();

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		[NotNull]
		protected virtual IEnumerable<AssociationEndTableRow> GetTableRows()
		{
			return ObjectDataset.GetAssociationEnds(_modelBuilder.IncludeDeletedModelElements)
			                    .Select(end => new AssociationEndTableRow(end));
		}

		protected override bool SortChildren => true;
	}
}
