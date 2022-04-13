using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Datasets;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class ObjectCategoriesItem<T> : EntityTypeItem<ObjectCategory>
		where T : ObjectDataset
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private readonly ObjectDatasetItem<T> _parent;

		public ObjectCategoriesItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                            [NotNull] ObjectDatasetItem<T> parent)
			: base("Object Categories",
			       "Object class subtypes (=Object Types) and 'sub-subtypes' (=Object Subtypes)"
			)
		{
			_modelBuilder = modelBuilder;
			_parent = parent;
		}

		public ObjectDataset Dataset => _parent.GetEntity();

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this, _parent.GetModel());
		}
	}
}
