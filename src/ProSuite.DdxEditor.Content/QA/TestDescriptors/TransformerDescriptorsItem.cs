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
	public class TransformerDescriptorsItem : InstanceDescriptorsItem<TransformerDescriptor>
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static TransformerDescriptorsItem()
		{
			// Static  initializer:
			_image = ItemUtils.GetGroupItemImage(Resources.TransformOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.TransformOverlay);
		}

		public TransformerDescriptorsItem([NotNull] CoreDomainModelItemModelBuilder modelBuider) :
			base("Transformer Descriptors", "Transformer algorithm implementations",
			     modelBuider) { }

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		#region Overrides of InstanceDescriptorsItem<TransformerDescriptor>

		protected override string DescriptorTypeDisplayName => "transformer descriptor";

		protected override T CreateDescriptor<T>(Type type, int constructor)
		{
			InstanceDescriptor result = new TransformerDescriptor(
				InstanceFactoryUtils.GetDefaultDescriptorName(type, constructor),
				new ClassDescriptor(type), constructor);

			return (T) result;
		}

		protected override IEnumerable<InstanceDescriptorTableRow> GetTableRows()
		{
			IInstanceDescriptorRepository repository = ModelBuilder.InstanceDescriptors;

			return InstanceDescriptorItemUtils.GetTransformerDescriptorTableRows(repository);
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
				         assembly, typeof(ITableTransformer), includeObsolete,
				         includeInternallyUsed))
			{
				foreach (int constructorIndex in
				         InstanceFactoryUtils.GetConstructorIndexes(instanceType,
					         includeObsolete,
					         includeInternallyUsed))
				{
					count++;
					newDescriptors.Add(
						CreateDescriptor<TransformerDescriptor>(instanceType, constructorIndex));
				}
			}

			_msg.InfoFormat("The assembly contains {0} {1}s", count, DescriptorTypeDisplayName);

			applicationController.GoToItem(this);

			TryAddInstanceDescriptors(newDescriptors);
		}

		#endregion
	}
}
