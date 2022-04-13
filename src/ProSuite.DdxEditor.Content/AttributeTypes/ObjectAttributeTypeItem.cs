using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Dependencies;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.AttributeTypes
{
	public class ObjectAttributeTypeItem<T> : AttributeTypeItem<T>
		where T : ObjectAttributeType
	{
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public ObjectAttributeTypeItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                               [NotNull] T attributeType,
		                               [NotNull] IRepository<AttributeType> repository)
			: base(attributeType, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<T, IViewObserver> compositeControl)
		{
			// Attribute
			base.AddEntityPanels(compositeControl);

			// AssociationAttribute
			var control = new ObjectAttributeTypeControl<T>();

			compositeControl.AddPanel(control);
		}

		public override IList<DependingItem> GetDependingItems()
		{
			return _modelBuilder.GetDependingItems(GetEntity());
		}

		protected override bool AllowDelete => true;
	}
}
