using System.Drawing;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.UI.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.AssociationEnds
{
	public class AssociationEndItem : SimpleEntityItem<AssociationEnd, AssociationEnd>
	{
		private readonly Image _image;
		private readonly string _imageKey;

		/// <summary>
		/// Initializes a new instance of the <see cref="AssociationEndItem"/> class.
		/// </summary>
		/// <param name="modelBuilder">The model builder.</param>
		/// <param name="associationEnd">The associationEnd.</param>
		/// <param name="repository">The repository.</param>
		public AssociationEndItem(CoreDomainModelItemModelBuilder modelBuilder,
		                          AssociationEnd associationEnd,
		                          IRepository<AssociationEnd>
			                          repository)
			: base(associationEnd, repository)
		{
			_image = AssociationEndImageLookup.GetImage(associationEnd, out _imageKey);
		}

		public override Image Image => _image;

		public override string ImageKey => _imageKey;

		protected override IWrappedEntityControl<AssociationEnd>
			CreateEntityControl(IItemNavigation itemNavigation)
		{
			var control = new AssociationEndControl();
			new AssociationEndPresenter(this, control);
			return control;
		}
	}
}
