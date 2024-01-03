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
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Core;

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

		public void AddInstanceDescriptors(string dllFilePath, IItemNavigation itemNavigation)
		{
			// NOTE: This method could profit from a re-unification with TestDescriptorsItem

			using (_msg.IncrementIndentation(
				       "Adding {0}s from assembly {1}", DescriptorTypeDisplayName, dllFilePath))
			{
				Assembly assembly = Assembly.LoadFile(dllFilePath);

				AddInstanceDescriptors(itemNavigation, assembly);
			}
		}

		protected void AddInstanceDescriptors(IItemNavigation itemNavigation, Assembly assembly)
		{
			const bool includeObsolete = false;
			const bool includeInternallyUsed = false;

			Type instanceBaseType = GetInstanceType();

			IEnumerable<Type> instanceTypes = InstanceFactoryUtils.GetClasses(
				assembly, instanceBaseType, includeObsolete, includeInternallyUsed);

			var newDescriptors = new List<InstanceDescriptor>();

			// TODO allow specifying naming convention
			// TODO optionally use alternate display name 
			// TODO allow selection of types/constructors
			// TODO optionally change properties of existing descriptors with same definition
			var count = 0;

			foreach (Type instanceType in instanceTypes)
			{
				foreach (int constructorIndex in
				         InstanceUtils.GetConstructorIndexes(instanceType))
				{
					count++;
					newDescriptors.Add(CreateDescriptor(instanceType, constructorIndex));
				}
			}

			_msg.InfoFormat("The assembly contains {0} {1}s", count, DescriptorTypeDisplayName);

			itemNavigation.GoToItem(this);

			TryAddInstanceDescriptors(newDescriptors, ModelBuilder.InstanceDescriptors);
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

			// TODO: Consider automatically detecting the relevant assemblies and load ALL transformers etc.
			//commands.Add(new ImportTestDescriptorsCommand(
			//				 this, applicationController,
			//				 _modelBuilder.DefaultTestDescriptorsXmlFile));
			//commands.Add(new AddTestDescriptorCommand(this, applicationController));
			commands.Add(CreateAddFromAssemblyCommand(applicationController));
			//commands.Add(new CreateReportForAssemblyTestsCommand(this, applicationController));
			//commands.Add(
			//	new CreateReportForRegisteredTestsCommand(this, applicationController));
			commands.Add(new DeleteAllChildItemsCommand(this, applicationController));
		}

		protected abstract InstanceDescriptor CreateDescriptor(Type type, int constructor);

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		private AddInstanceDescriptorsFromAssemblyCommand<T> CreateAddFromAssemblyCommand(
			IApplicationController applicationController)
		{
			return new AddInstanceDescriptorsFromAssemblyCommand<T>(
				this, applicationController, DescriptorTypeDisplayName);
		}

		[NotNull]
		protected abstract IEnumerable<InstanceDescriptorTableRow> GetTableRows();

		private void TryAddInstanceDescriptors(
			[NotNull] IEnumerable<InstanceDescriptor> descriptors,
			IInstanceDescriptorRepository repository)
		{
			Assert.ArgumentNotNull(descriptors, nameof(descriptors));

			var addedCount = 0;
			ModelBuilder.NewTransaction(
				delegate
				{
					addedCount =
						InstanceDescriptorItemUtils.TryAddInstanceDescriptorsTx(
							descriptors, repository);
				});

			_msg.InfoFormat("{0} {1}s added", addedCount, DescriptorTypeDisplayName);

			RefreshChildren();
		}
	}
}
