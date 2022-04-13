using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.AttributeTypes
{
	public class AttributeTypeItem<E> : SubclassedEntityItem<E, AttributeType>
		where E : AttributeType
	{
		public AttributeTypeItem([NotNull] E attributeType,
		                         [NotNull] IRepository<AttributeType> repository)
			: base(attributeType, repository) { }

		protected override void AttachPresenter(
			ICompositeEntityControl<E, IViewObserver> control)
		{
			new AttributeTypePresenter<E>(this, control);
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<E, IViewObserver> compositeControl)
		{
			compositeControl.AddPanel(new AttributeTypeControl<E>());
		}
	}
}