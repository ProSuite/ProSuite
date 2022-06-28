using System.Collections.Generic;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IInstanceDescriptorRepository : IRepository<InstanceDescriptor>
	{
		IList<TransformerDescriptor> GetTransformerDescriptors();

		IList<IssueFilterDescriptor> GetIssueFilterDescriptors();

		IList<RowFilterDescriptor> GetRowFilterDescriptors();
	}
}
