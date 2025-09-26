using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Models;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Associations
{
	public class AssociationsItem<M> : EntityTypeItem<Association> where M : DdxModel
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		private readonly ModelItemBase<M> _parent;

		public AssociationsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                        [NotNull] ModelItemBase<M> parent)
			: base("Associations",
			       "Associations between object datasets (relationship classes)")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(parent, nameof(parent));

			_modelBuilder = modelBuilder;
			_parent = parent;
		}

		[NotNull]
		public DdxModel Model => Assert.NotNull(_parent.GetEntity());

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override bool SortChildren => true;

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		protected virtual IEnumerable<AssociationTableRow> GetTableRows()
		{
			return Model.GetAssociations(_modelBuilder.IncludeDeletedModelElements)
			            .Select(GetAssociationTableRow);
		}

		[NotNull]
		protected virtual AssociationTableRow GetAssociationTableRow(Association association)
		{
			return new AssociationTableRow(association);
		}
	}
}
