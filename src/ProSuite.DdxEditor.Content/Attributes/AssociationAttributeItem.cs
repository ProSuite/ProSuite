using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public class AssociationAttributeItem : AttributeItem<AssociationAttribute>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public AssociationAttributeItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                                [NotNull] AssociationAttribute attribute,
		                                [NotNull] IRepository<Attribute> repository)
			: base(attribute, repository)
		{
			_modelBuilder = modelBuilder;
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<AssociationAttribute, IViewObserver> compositeControl,
			IItemNavigation itemNavigation)
		{
			// Attribute
			base.AddEntityPanels(compositeControl, itemNavigation);

			// AssociationAttribute
			IEntityPanel<AssociationAttribute> control =
				new AssociationAttributeControl<AssociationAttribute>();

			compositeControl.AddPanel(control);
		}
	}
}
