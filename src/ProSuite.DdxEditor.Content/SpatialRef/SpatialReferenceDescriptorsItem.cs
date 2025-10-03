using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.SpatialRef
{
	public class SpatialReferenceDescriptorsItem :
		EntityTypeItem<SpatialReferenceDescriptor>
	{
		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static SpatialReferenceDescriptorsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.SpatialReferenceOverlay);
			_selectedImage =
				ItemUtils.GetGroupItemSelectedImage(Resources.SpatialReferenceOverlay);
		}

		public SpatialReferenceDescriptorsItem(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base("Spatial Reference Descriptors",
			       "Registered spatial reference definitions")
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override bool AllowDeleteSelectedChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController
			                                        applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddSpatialReferenceDescriptorCommand(this, applicationController));
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		protected virtual IEnumerable<SpatialReferenceDescriptorTableRow> GetTableRows()
		{
			return _modelBuilder.SpatialReferenceDescriptors.GetAll()
			                    .Select(entity =>
				                            new SpatialReferenceDescriptorTableRow(
					                            entity));
		}

		public SpatialReferenceDescriptorItem AddSpatialReferenceDescriptorItem()
		{
			var spatialReferenceDescriptor = new SpatialReferenceDescriptor();

			var item = new SpatialReferenceDescriptorItem(_modelBuilder,
			                                              spatialReferenceDescriptor,
			                                              _modelBuilder
				                                              .SpatialReferenceDescriptors);

			AddChild(item);

			item.NotifyChanged();

			return item;
		}
	}
}
