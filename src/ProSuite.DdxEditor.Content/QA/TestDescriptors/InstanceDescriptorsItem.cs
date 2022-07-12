using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public abstract class InstanceDescriptorsItem<T> : EntityTypeItem<T>
		where T : InstanceDescriptor
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		protected CoreDomainModelItemModelBuilder ModelBuilder { get; }

		protected InstanceDescriptorsItem([NotNull] string text,
		                                  [CanBeNull] string description,
		                                  [NotNull] CoreDomainModelItemModelBuilder modelBuider)
			: base(text, description)
		{
			Assert.ArgumentNotNull(modelBuider, nameof(modelBuider));

			ModelBuilder = modelBuider;
		}

		protected override bool AllowDeleteSelectedChildren => true;

		protected override bool SortChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return ModelBuilder.GetChildren(this);
		}

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			// TODO: Consider automatically detect the relevant assemblies and load ALL transformers etc.
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

		protected abstract T CreateDescriptor<T>(Type type, int constructor)
			where T : InstanceDescriptor;

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

		protected abstract string DescriptorTypeDisplayName { get; }

		[NotNull]
		protected abstract IEnumerable<InstanceDescriptorTableRow> GetTableRows();

		protected void TryAddInstanceDescriptors(
			[NotNull] IEnumerable<InstanceDescriptor> descriptors)
		{
			Assert.ArgumentNotNull(descriptors, nameof(descriptors));

			IInstanceDescriptorRepository repository = ModelBuilder.InstanceDescriptors;

			var addedCount = 0;
			ModelBuilder.NewTransaction(
				delegate
				{
					Dictionary<string, InstanceDefinition> definitions =
						repository.GetAll()
						          .Select(descriptor => new InstanceDefinition(descriptor))
						          .ToDictionary(definition => definition.Name);

					foreach (InstanceDescriptor descriptor in descriptors)
					{
						var definition = new InstanceDefinition(descriptor);

						// Note daro: hack for TOP-5464
						// In DDX schema there is an unique constraint on NAME
						// and
						// FCTRY_TYPENAME, FCTRY_ASSEMBLYNAME, TEST_TYPENAME, TEST_ASSEMBLYNAME, TEST_CTROID

						// 1st check: name
						if (definitions.ContainsKey(definition.Name))
						{
							_msg.InfoFormat(
								"An {0} with the same definition as '{1}' is already registered",
								descriptor.TypeDisplayName, descriptor.Name);
						}
						// 2nd check: equality with rest of object
						else if (! definitions.ContainsValue(definition))
						{
							_msg.DebugFormat("Registering new {0} {1}", descriptor.TypeDisplayName,
							                 descriptor);

							definitions.Add(definition.Name, definition);

							repository.Save(descriptor);

							addedCount++;
						}
					}
				});

			_msg.InfoFormat("{0} instance descriptor(s) added", addedCount);

			RefreshChildren();
		}

		public void AddInstanceDescriptors(string dllFilePath,
		                                   IApplicationController applicationController)
		{
			using (_msg.IncrementIndentation(
				       "Adding {0} from assembly {1}", DescriptorTypeDisplayName, dllFilePath))
			{
				Assembly assembly = Assembly.LoadFile(dllFilePath);

				AddInstanceDescriptorsCore(applicationController, assembly);
			}
		}

		protected abstract void AddInstanceDescriptorsCore(
			IApplicationController applicationController,
			Assembly assembly);
	}
}
