using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.AGP.QA;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.AGP.QA
{
	public class DdxSpecificationReferencesProvider : IQualitySpecificationReferencesProvider
	{
		[NotNull] private readonly IMapBasedSessionContext _sessionContext;
		[NotNull] private readonly QualityVerificationServiceClient _client;

		public DdxSpecificationReferencesProvider([NotNull] IMapBasedSessionContext sessionContext,
		                                          [NotNull] QualityVerificationServiceClient client)
		{
			_sessionContext = sessionContext;
			_client = client;
		}

		public bool IncludeHiddenSpecifications { get; set; }

		public string BackendDisplayName => _client.HostName;

		public bool CanGetSpecifications()
		{
			return _sessionContext.ProjectWorkspace != null &&
			       ! _sessionContext.DdxAccessDisabled;
		}

		public async Task<IList<IQualitySpecificationReference>> GetQualitySpecifications()
		{
			var result = new List<IQualitySpecificationReference>();

			ProjectWorkspace projectWorkspace = _sessionContext.ProjectWorkspace;

			if (projectWorkspace == null)
			{
				return result;
			}

			return await DdxUtils.LoadSpecificationsRpcAsync(projectWorkspace.GetDatasetIds(),
			                                                 IncludeHiddenSpecifications,
			                                                 Assert.NotNull(_client.DdxClient));
		}
	}
}
