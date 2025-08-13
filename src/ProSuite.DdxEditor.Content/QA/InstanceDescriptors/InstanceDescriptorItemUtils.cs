using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	internal static class InstanceDescriptorItemUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static IEnumerable<InstanceDescriptorTableRow> GetIssueFilterDescriptorTableRows(
			[NotNull] IInstanceDescriptorRepository repository)
		{
			return GetInstanceDescriptorTableRows<IssueFilterDescriptor, IssueFilterConfiguration>(
				repository);
		}

		public static IEnumerable<InstanceDescriptorTableRow> GetTransformerDescriptorTableRows(
			IInstanceDescriptorRepository instanceDescriptors)
		{
			return GetInstanceDescriptorTableRows<TransformerDescriptor, TransformerConfiguration>(
				instanceDescriptors);
		}

		public static int TryAddInstanceDescriptorsTx<T>(IEnumerable<T> descriptors,
		                                                 IRepository<T> repository)
			where T : InstanceDescriptor
		{
			int addedCount = 0;

			Dictionary<string, InstanceDefinition> definitions =
				repository.GetAll()
				          .Select(InstanceDefinition.CreateFrom)
				          .ToDictionary(definition => definition.Name);

			foreach (T descriptor in descriptors)
			{
				var definition = InstanceDefinition.CreateFrom(descriptor);

				// Note daro: hack for TOP-5464
				// In DDX schema there is an unique constraint on NAME
				// and
				// FCTRY_TYPENAME, FCTRY_ASSEMBLYNAME, TEST_TYPENAME, TEST_ASSEMBLYNAME, TEST_CTROID

				// 1st check: name
				if (definitions.ContainsKey(definition.Name))
				{
					_msg.InfoFormat(
						"{0} with the same definition as '{1}' is already registered",
						descriptor.TypeDisplayName, descriptor.Name);
				}
				// 2nd check: equality with rest of object
				else if (! definitions.ContainsValue(definition))
				{
					_msg.InfoFormat("Registering new {0} {1}", descriptor.TypeDisplayName,
					                descriptor);

					definitions.Add(definition.Name, definition);

					repository.Save(descriptor);

					addedCount++;
				}
			}

			return addedCount;
		}

		public static void ValidateDescriptorAgainstDuplicateName(InstanceDescriptor entity,
			InstanceDescriptor descriptorWithSameName,
			Notification notification)
		{
			if (descriptorWithSameName != null &&
			    descriptorWithSameName.Id != entity.Id)
			{
				notification.RegisterMessage(
					"Name",
					$"A {descriptorWithSameName.TypeDisplayName} with the same name already exists",
					Severity.Error);
			}
		}

		public static void ValidateDescriptorAgainstDuplicateImplementation(
			[NotNull] InstanceDescriptor entity,
			[CanBeNull] InstanceDescriptor descriptorWithSameImplementation,
			[NotNull] Notification notification)
		{
			if (descriptorWithSameImplementation == null ||
			    descriptorWithSameImplementation.Id == entity.Id)
			{
				return;
			}

			string typeDisplayName = descriptorWithSameImplementation.TypeDisplayName;

			notification.RegisterMessage(
				$"{typeDisplayName} {descriptorWithSameImplementation.Name} " +
				"already has the same implementation as the current instance (factory or class/constructor). " +
				$"Please use the existing {typeDisplayName}.",
				Severity.Error);
		}

		private static IEnumerable<InstanceDescriptorTableRow> GetInstanceDescriptorTableRows<D, C>(
			IInstanceDescriptorRepository repository)
			where D : InstanceDescriptor
			where C : InstanceConfiguration
		{
			IList<D> transformerDescriptors = repository.GetInstanceDescriptors<D>();

			IDictionary<int, int> refCountMap =
				repository.GetReferencingConfigurationCount<C>();

			return CreateInstanceDescriptorTableRows(transformerDescriptors, refCountMap);
		}

		private static IEnumerable<InstanceDescriptorTableRow> CreateInstanceDescriptorTableRows(
			[NotNull] IEnumerable<InstanceDescriptor> descriptors,
			[NotNull] IDictionary<int, int> refCountById)
		{
			foreach (InstanceDescriptor descriptor in descriptors)
			{
				if (! refCountById.TryGetValue(descriptor.Id, out int refCount))
				{
					refCount = 0;
				}

				yield return new InstanceDescriptorTableRow(descriptor, refCount);
			}
		}
	}
}
