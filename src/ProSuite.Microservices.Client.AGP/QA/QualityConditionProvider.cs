using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.AGP.QA
{
	public class QualityConditionProvider : IQualityConditionProvider
	{
		private readonly QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient _client;

		public QualityConditionProvider(
			QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient client)
		{
			_client = client;
		}

		public ISupportedInstanceDescriptors KnownInstanceDescriptors { get; set; }

		public QualityCondition GetCondition(string qualityConditionName)
		{
			Assert.ArgumentNotNullOrEmpty(qualityConditionName, nameof(qualityConditionName));

			GetConditionRequest request = new GetConditionRequest()
			                              {
				                              ConditionName = qualityConditionName
			                              };

			GetConditionResponse response = _client.GetQualityCondition(request);

			QualityCondition condition =
				DdxUtils.CreateQualityCondition(response, KnownInstanceDescriptors);

			return condition;
		}

		public async Task<IList<QualityCondition>> GetConditions(IList<int> conditionIds)
		{
			Assert.ArgumentNotNull(conditionIds, nameof(conditionIds));

			IList<QualityCondition> conditions =
				await DdxUtils.LoadQualityConditions(conditionIds, KnownInstanceDescriptors,
				                                     _client);

			return conditions ?? new List<QualityCondition>(0);
		}
	}
}
