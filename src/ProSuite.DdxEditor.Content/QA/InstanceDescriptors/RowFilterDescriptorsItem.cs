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
	public class RowFilterDescriptorsItem : InstanceDescriptorsItem<RowFilterDescriptor>
	{
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static RowFilterDescriptorsItem()
		{
			// Static  initializer:
			_image = ItemUtils.GetGroupItemImage(Resources.RowFilterOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.RowFilterOverlay);
		}

		public RowFilterDescriptorsItem([NotNull] CoreDomainModelItemModelBuilder modelBuider) :
			base("Row Filter Descriptors", "Row Filter algorithm implementations",
			     modelBuider) { }

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		#region Overrides of InstanceDescriptorsItem<RowFilterDescriptor>

		protected override string DescriptorTypeDisplayName => "row filter descriptor";

		protected override Type GetInstanceType()
		{
			return typeof(IRowFilter);
		}

		protected override InstanceDescriptor CreateDescriptor(Type type, int constructor)
		{
			InstanceDescriptor result = new RowFilterDescriptor(
				InstanceFactoryUtils.GetDefaultDescriptorName(type, constructor),
				new ClassDescriptor(type), constructor);

			return result;
		}

		protected override IEnumerable<InstanceDescriptorTableRow> GetTableRows()
		{
			IInstanceDescriptorRepository repository = ModelBuilder.InstanceDescriptors;

			return InstanceDescriptorItemUtils.GetRowFilterDescriptorTableRows(repository);
		}

		#endregion
	}
}
