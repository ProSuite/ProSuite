using System;
using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Container;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class TransformerDescriptorsItem : InstanceDescriptorsItem<TransformerDescriptor>
	{
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static TransformerDescriptorsItem()
		{
			// Static  initializer:
			_image = ItemUtils.GetGroupItemImage(Resources.TransformOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.TransformOverlay);
		}

		public TransformerDescriptorsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder) :
			base("Transformer Descriptors", "Dataset transformer algorithm implementations",
			     modelBuilder) { }

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		#region Overrides of InstanceDescriptorsItem<TransformerDescriptor>

		protected override string DescriptorTypeDisplayName => "Transformer Descriptor";

		protected override Type GetInstanceType()
		{
			return typeof(ITableTransformer);
		}

		protected override InstanceDescriptor CreateDescriptor(Type type, int constructor)
		{
			InstanceDescriptor result = new TransformerDescriptor(
				InstanceFactoryUtils.GetDefaultDescriptorName(type, constructor),
				new ClassDescriptor(type), constructor);

			return result;
		}

		protected override IEnumerable<InstanceDescriptorTableRow> GetTableRows()
		{
			IInstanceDescriptorRepository repository = ModelBuilder.InstanceDescriptors;

			return InstanceDescriptorItemUtils.GetTransformerDescriptorTableRows(repository);
		}

		#endregion
	}
}
