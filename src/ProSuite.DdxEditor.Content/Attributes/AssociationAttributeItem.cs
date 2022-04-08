using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

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
			ICompositeEntityControl<AssociationAttribute, IViewObserver> compositeControl)
		{
			// Attribute
			base.AddEntityPanels(compositeControl);

			// AssociationAttribute
			IEntityPanel<AssociationAttribute> control =
				new AssociationAttributeControl<AssociationAttribute>();

			compositeControl.AddPanel(control);
		}
	}
}
