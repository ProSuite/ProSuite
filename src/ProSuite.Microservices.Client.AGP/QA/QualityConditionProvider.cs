using System.Collections.Generic;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
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

		/// <summary>
		/// The name of the DDX environment (for multi-DDX setups). A server that serves
		/// several environments rejects requests without environment name.
		/// </summary>
		[CanBeNull]
		public string DdxEnvironmentName { get; set; }

		public QualityCondition GetCondition(string qualityConditionName)
		{
			Assert.ArgumentNotNullOrEmpty(qualityConditionName, nameof(qualityConditionName));

			GetConditionRequest request = new GetConditionRequest()
			                              {
				                              Environment =
					                              ProtobufGeomUtils.NullToEmpty(DdxEnvironmentName),
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
				                                     _client, DdxEnvironmentName);

			return conditions ?? new List<QualityCondition>(0);
		}
	}
}
