using System.Collections.Generic;
using ProSuite.Commons.DomainModels;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	public interface IInstanceConfigurationRepository : IRepository<InstanceConfiguration>
	{
		IList<TransformerConfiguration> GetTransformerConfigurations();

		IList<RowFilterConfiguration> GetRowFilterConfigurations();

		IList<IssueFilterConfiguration> GetIssueFilterConfigurations();

		IList<T> Get<T>(InstanceDescriptor descriptor) where T : InstanceConfiguration;
	}
}
