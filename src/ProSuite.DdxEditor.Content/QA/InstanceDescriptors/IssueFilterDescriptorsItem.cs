using System;
using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Container;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class IssueFilterDescriptorsItem : InstanceDescriptorsItem<IssueFilterDescriptor>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static IssueFilterDescriptorsItem()
		{
			// Static  initializer:
			_image = ItemUtils.GetGroupItemImage(Resources.IssueFilterOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.IssueFilterOverlay);
		}

		public IssueFilterDescriptorsItem([NotNull] CoreDomainModelItemModelBuilder modelBuider) :
			base("Issue Filter Descriptors", "Issue Filter algorithm implementations",
			     modelBuider) { }

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		#region Overrides of InstanceDescriptorsItem<IssueFilterDescriptor>

		protected override string DescriptorTypeDisplayName => "issue filter descriptor";

		protected override Type GetInstanceType()
		{
			return typeof(IIssueFilter);
		}

		protected override InstanceDescriptor CreateDescriptor(Type type, int constructor)
		{
			InstanceDescriptor result = new IssueFilterDescriptor(
				InstanceFactoryUtils.GetDefaultDescriptorName(type, constructor),
				new ClassDescriptor(type), constructor);

			return result;
		}

		protected override IEnumerable<InstanceDescriptorTableRow> GetTableRows()
		{
			IInstanceDescriptorRepository repository = ModelBuilder.InstanceDescriptors;

			return InstanceDescriptorItemUtils.GetIssueFilterDescriptorTableRows(repository);
		}

		#endregion
	}
}
