using System.Collections.Generic;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IInstanceDescriptorRepository : IRepository<InstanceDescriptor>
	{
		IList<TransformerDescriptor> GetTransformerDescriptors();

		IList<IssueFilterDescriptor> GetIssueFilterDescriptors();

		IList<RowFilterDescriptor> GetRowFilterDescriptors();

		InstanceDescriptor Get(string name);

		InstanceDescriptor GetWithSameImplementation(InstanceDescriptor entity);

		IDictionary<int, int> GetReferencingConfigurationCount<T>() where T : InstanceConfiguration;

		IList<T> GetInstanceDescriptors<T>() where T : InstanceDescriptor;
	}
}
