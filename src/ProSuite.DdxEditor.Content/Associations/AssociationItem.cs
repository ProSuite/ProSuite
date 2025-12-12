using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.Associations
{
	public class AssociationItem : SimpleEntityItem<Association, Association>
	{
		private readonly Image _image;
		private readonly string _imageKey;
		private readonly CoreDomainModelItemModelBuilder _modelBuilder;

		public AssociationItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder,
		                       [NotNull] Association association,
		                       [NotNull] IRepository<Association> repository)
			: base(association, repository)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
			_image = AssociationImageLookup.GetImage(association, out _imageKey);
		}

		public override Image Image => _image;

		public override string ImageKey => _imageKey;

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override IWrappedEntityControl<Association> CreateEntityControl(
			IItemNavigation itemNavigation)
		{
			var control = new AssociationControl();
			new AssociationPresenter(this);
			return control;
		}
	}
}
