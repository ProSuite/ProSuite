using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.AttributeTypes
{
	public class AttributeTypesItem : EntityTypeItem<AttributeType>
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public AttributeTypesItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base("Attribute Types", "Attribute Types")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override bool SortChildren => true;
	}
}
