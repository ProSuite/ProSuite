using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.InstanceDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Container;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
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

		protected override T CreateDescriptor<T>(Type type, int constructor)
		{
			InstanceDescriptor result = new IssueFilterDescriptor(
				InstanceFactoryUtils.GetDefaultDescriptorName(type, constructor),
				new ClassDescriptor(type), constructor);

			return (T) result;
		}

		protected override IEnumerable<InstanceDescriptorTableRow> GetTableRows()
		{
			IInstanceDescriptorRepository repository = ModelBuilder.InstanceDescriptors;

			return InstanceDescriptorItemUtils.GetIssueFilterDescriptorTableRows(repository);
		}

		protected override void AddInstanceDescriptorsCore(
			IApplicationController applicationController,
			Assembly assembly)
		{
			var newDescriptors = new List<InstanceDescriptor>();

			const bool includeObsolete = false;
			const bool includeInternallyUsed = false;

			// TODO allow specifying naming convention
			// TODO optionally use alternate display name 
			// TODO allow selection of types/constructors
			// TODO optionally change properties of existing descriptors with same definition
			var count = 0;

			foreach (Type instanceType in InstanceFactoryUtils.GetClasses(
				         assembly, typeof(IIssueFilter), includeObsolete,
				         includeInternallyUsed))
			{
				foreach (int constructorIndex in
				         InstanceFactoryUtils.GetConstructorIndexes(instanceType,
					         includeObsolete,
					         includeInternallyUsed))
				{
					count++;
					newDescriptors.Add(
						CreateDescriptor<IssueFilterDescriptor>(instanceType, constructorIndex));
				}
			}

			_msg.InfoFormat("The assembly contains {0} {1}s", count, DescriptorTypeDisplayName);

			applicationController.GoToItem(this);

			TryAddInstanceDescriptors(newDescriptors);
		}

		#endregion
	}
}
