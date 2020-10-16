using Grpc.Core;
using ProSuite.ProtobufClasses;
using System.Threading.Tasks;

namespace ProSuite.GrpcServer
{
	class VerifyQualityGrpcService : VerifyQualityService.VerifyQualityServiceBase
	{
		public override async Task PerformQualityVerification(VerifyQualityRequest request, IServerStreamWriter<VerifyQualityResponse> responseStream, ServerCallContext context)
		{

			// simulate quality verification and progress
			for (int i = 1; i < 100; i++)
			{
				await responseStream.WriteAsync(
					new VerifyQualityResponse
					{
						RequestId = request.RequestId,
						Status = VerifyQualityResponse.Types.ProgressStatus.Progress,
						StepsDone = i,
						StepsTotal = 100
					});
			}

			await responseStream.WriteAsync(
				new VerifyQualityResponse
				{
					RequestId = request.RequestId,
					Status = VerifyQualityResponse.Types.ProgressStatus.Done,
					StepsDone = 100,
					StepsTotal = 100,
					Result = new VerifyQualityResult
					{

					}
				});

		}
	}
}


