using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	internal static class InstanceDescriptorItemUtils
	{
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
