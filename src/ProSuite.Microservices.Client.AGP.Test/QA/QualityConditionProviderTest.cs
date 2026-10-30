using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using NUnit.Framework;
using ProSuite.Microservices.Client.AGP.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.AGP.Test.QA
{
	[TestFixture]
	public class QualityConditionProviderTest
	{
		// Issue A (qa-server-issues.md): a server that serves several data dictionary
		// environments cannot answer a request that names no environment.

		[Test]
		public void CanSendEnvironmentWhenGettingConditionByName()
		{
			var client = new RecordingDdxClient();

			var provider = new QualityConditionProvider(client)
			               {
				               DdxEnvironmentName = "TOPGISP_DDX"
			               };

			provider.GetCondition("TLM.bauGEB: Simple Geometrie");

			Assert.AreEqual(1, client.ConditionRequests.Count);
			Assert.AreEqual("TOPGISP_DDX", client.ConditionRequests[0].Environment);
			Assert.AreEqual("TLM.bauGEB: Simple Geometrie",
			                client.ConditionRequests[0].ConditionName);
		}

		[Test]
		public async Task CanSendEnvironmentWhenGettingConditionsById()
		{
			var client = new RecordingDdxClient();

			var provider = new QualityConditionProvider(client)
			               {
				               DdxEnvironmentName = "TOPGISP_DDX"
			               };

			await provider.GetConditions(new List<int> { 12, 13 });

			Assert.AreEqual(1, client.ConditionsRequests.Count);
			Assert.AreEqual("TOPGISP_DDX", client.ConditionsRequests[0].Environment);
			Assert.AreEqual(new[] { 12, 13 }, client.ConditionsRequests[0].ConditionIds);
		}

		[Test]
		public void CanSendEmptyEnvironmentIfNoneIsConfigured()
		{
			// A server with exactly one environment accepts the empty environment name.
			var client = new RecordingDdxClient();

			var provider = new QualityConditionProvider(client);

			provider.GetCondition("TLM.bauGEB: Simple Geometrie");

			Assert.AreEqual(string.Empty, client.ConditionRequests[0].Environment);
		}

		/// <summary>
		/// Records the requests and returns empty responses.
		/// </summary>
		private class RecordingDdxClient
			: QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient
		{
			public List<GetConditionRequest> ConditionRequests { get; } =
				new List<GetConditionRequest>();

			public List<GetConditionsRequest> ConditionsRequests { get; } =
				new List<GetConditionsRequest>();

			public override GetConditionResponse GetQualityCondition(
				GetConditionRequest request, CallOptions options)
			{
				ConditionRequests.Add(request);

				return new GetConditionResponse();
			}

			public override AsyncUnaryCall<GetConditionsResponse> GetQualityConditionsAsync(
				GetConditionsRequest request, CallOptions options)
			{
				ConditionsRequests.Add(request);

				return new AsyncUnaryCall<GetConditionsResponse>(
					Task.FromResult(new GetConditionsResponse()),
					Task.FromResult(new Metadata()),
					() => Status.DefaultSuccess,
					() => new Metadata(),
					() => { });
			}
		}
	}
}
