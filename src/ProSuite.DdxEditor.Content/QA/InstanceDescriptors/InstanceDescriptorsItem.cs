using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public abstract class InstanceDescriptorsItem<T> : EntityTypeItem<T>
		where T : InstanceDescriptor
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		protected CoreDomainModelItemModelBuilder ModelBuilder { get; }

		protected InstanceDescriptorsItem([NotNull] string text,
		                                  [CanBeNull] string description,
		                                  [NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base(text, description)
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));

			ModelBuilder = modelBuilder;
		}

		protected override bool AllowDeleteSelectedChildren => true;

		protected override bool SortChildren => true;

		[NotNull]
		public InstanceDescriptorItem AddInstanceDescriptorItem()
		{
			var instanceDescriptor = CreateDescriptor();
			var item = new InstanceDescriptorItem(ModelBuilder, instanceDescriptor,
			                                      ModelBuilder.InstanceDescriptors);

			AddChild(item);

			item.NotifyChanged();

			return item;
		}

		public void AddInstanceDescriptors(string dllFilePath, IItemNavigation itemNavigation)
		{
			using (_msg.IncrementIndentation(
				       "Adding {0}s from assembly {1}", DescriptorTypeDisplayName, dllFilePath))
			{
				Assembly assembly = Assembly.LoadFile(dllFilePath);

				IList<InstanceDescriptor> newDescriptors =
					InstanceDescriptorItemUtils.CreateInstanceDescriptors(
						assembly, GetInstanceType(), DescriptorTypeDisplayName,
						CreateDescriptor);

				itemNavigation.GoToItem(this);

				InstanceDescriptorItemUtils.RegisterDescriptors(
					ModelBuilder, newDescriptors, ModelBuilder.InstanceDescriptors,
					DescriptorTypeDisplayName);

				RefreshChildren();
			}
		}

		protected abstract string DescriptorTypeDisplayName { get; }

		protected abstract Type GetInstanceType();

		protected override IEnumerable<Item> GetChildren()
		{
			return ModelBuilder.GetChildren(this);
		}

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new AddInstanceDescriptorCommand<T>(
				             this, applicationController, DescriptorTypeDisplayName));
			commands.Add(new AddInstanceDescriptorsFromAssemblyCommand<T>(
				             this, applicationController, DescriptorTypeDisplayName));
			commands.Add(new DeleteAllChildItemsCommand(this, applicationController));
		}

		protected abstract InstanceDescriptor CreateDescriptor();

		protected abstract InstanceDescriptor CreateDescriptor(Type type, int constructor);

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		[NotNull]
		protected abstract IEnumerable<InstanceDescriptorTableRow> GetTableRows();
	}
}
