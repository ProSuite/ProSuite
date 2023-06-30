using System.Threading.Tasks;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	/// <summary>
	/// The client endpoint interface to be used to start a verification.
	/// Consider moving to separate project, if referencing Microservices.Client
	/// from the server becomes an issue.
	/// </summary>
	public interface IQualityVerificationClient
	{
		QualityVerificationGrpc.QualityVerificationGrpcClient QaGrpcClient { get; }

		bool CanAcceptCalls(bool allowFailOver = false);

		Task<bool> CanAcceptCallsAsync(bool allowFailOver = false);

		string GetAddress();
	}
}
