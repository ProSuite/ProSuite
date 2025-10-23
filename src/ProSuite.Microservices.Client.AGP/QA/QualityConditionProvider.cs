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
			GetConditionRequest request = new GetConditionRequest()
			                              {
				                              ConditionName = qualityConditionName
			                              };

			GetConditionResponse response = _client.GetQualityCondition(request);

			QualityCondition condition =
				DdxUtils.CreateQualityCondition(response, KnownInstanceDescriptors);

			return condition;
		}
	}
}
