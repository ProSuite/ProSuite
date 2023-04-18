using System.Collections.Generic;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IInstanceDescriptorRepository : IRepository<InstanceDescriptor>
	{
		IList<T> GetInstanceDescriptors<T>() where T : InstanceDescriptor;

		InstanceDescriptor Get(string name);

		InstanceDescriptor GetWithSameImplementation(InstanceDescriptor entity);

		IDictionary<int, int> GetReferencingConfigurationCount<T>() where T : InstanceConfiguration;

		bool SupportsTransformersAndFilters { get; }
	}
}
