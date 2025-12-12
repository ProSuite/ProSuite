using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public class AttributeItem<E> : SubclassedEntityItem<E, Attribute>
		where E : Attribute
	{
		[NotNull] private readonly string _imageKey;
		[NotNull] private readonly Image _image;

		public AttributeItem([NotNull] E attribute,
		                     [NotNull] IRepository<Attribute> repository)
			: base(attribute, repository)
		{
			_image = FieldTypeImageLookup.GetImage(attribute, out _imageKey);
		}

		public override Image Image => _image;

		public override string ImageKey => _imageKey;

		protected override void AttachPresenter(
			ICompositeEntityControl<E, IViewObserver> control)
		{
			// if needed, override and use specific subclass
			new AttributePresenter<E>(this, control);
		}

		protected override void AddEntityPanels(
			ICompositeEntityControl<E, IViewObserver> compositeControl,
			IItemNavigation itemNavigation)
		{
			compositeControl.AddPanel(new AttributeControl<E>());
		}
	}
}
