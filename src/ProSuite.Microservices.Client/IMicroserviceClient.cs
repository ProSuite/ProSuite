using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Microservices.Client
{
	/// <summary>
	/// Abstracts the connection to a microservice.
	/// </summary>
	public interface IMicroserviceClient
	{
		string HostName { get; }

		int Port { get; }

		bool UseTls { get; }

		string ServiceName { get; }

		string ServiceDisplayName { get; }

		/// <summary>
		/// Whether the configured channel is a load-balancer channel.
		/// </summary>
		bool ChannelIsLoadBalancer { get; }

		/// <summary>
		/// Whether the client has several addresses that could be used for fail-over.
		/// </summary>
		bool CanFailOver { get; }

		/// <summary>
		/// Whether a server process has been started in the background.
		/// </summary>
		bool ProcessStarted { get; }

		void Disconnect();

		/// <summary>
		/// Checks the health of the peer to determine whether calls can be accepted.
		/// </summary>
		/// <param name="allowFailOver"></param>
		/// <param name="logOnlyIfUnhealthy"></param>
		/// <returns></returns>
		bool CanAcceptCalls(bool allowFailOver = false,
		                    bool logOnlyIfUnhealthy = false);

		/// <summary>
		/// Checks the health of the peer to determine whether calls can be accepted.
		/// </summary>
		/// <param name="allowFailOver"></param>
		/// <returns></returns>
		Task<bool> CanAcceptCallsAsync(bool allowFailOver = false);

		/// <summary>
		/// Starts a local server process if the precondions are met, such as HostName being
		/// "localhost".
		/// </summary>
		/// <param name="executable"></param>
		/// <param name="extraArguments"></param>
		/// <param name="command"></param>
		/// <returns></returns>
		Task<bool> AllowStartingLocalServerAsync(
			[NotNull] string executable,
			[CanBeNull] string extraArguments = null,
			[CanBeNull] string command = null);

		/// <summary>
		/// Starts a local server process if the precondions are met, such as HostName being
		/// "localhost".
		/// </summary>
		/// <param name="executable"></param>
		/// <param name="extraArguments"></param>
		/// <param name="command"></param>
		/// <returns></returns>
		bool AllowStartingLocalServer([NotNull] string executable,
		                              [CanBeNull] string extraArguments = null,
		                              [CanBeNull] string command = null);

		bool TryRestart();

		string GetAddress();

		Task<int> GetWorkerServiceCountAsync();
	}
}
