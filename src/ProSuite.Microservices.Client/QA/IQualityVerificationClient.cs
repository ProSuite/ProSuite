using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	/// <summary>
	/// The client endpoint interface to be used to start a verification.
	/// Consider moving to separate project, if referencing Microservices.Client
	/// from the server becomes an issue.
	/// </summary>
	public interface IQualityVerificationClient : IMicroserviceClient
	{
		[CanBeNull]
		QualityVerificationGrpc.QualityVerificationGrpcClient QaGrpcClient { get; }

		[CanBeNull]
		QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient DdxClient { get; }

		/// <summary>
		/// Gets the number of running requests on the server / worker.
		/// </summary>
		/// <param name="timeOut"></param>
		/// <param name="runningRequestCount"></param>
		/// <returns></returns>
		bool TryGetRunningRequestCount(TimeSpan timeOut, out int runningRequestCount);

		/// <summary>
		/// Returns a list of worker clients to be used for parallel processing. The address of
		/// the client must be a load balancer address.
		/// </summary>
		/// <param name="desiredNewWorkerCount"></param>
		/// <returns></returns>
		IEnumerable<IQualityVerificationClient> GetWorkerClients(int desiredNewWorkerCount);
	}
}
