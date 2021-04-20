using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Health.V1;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using Quaestor.ServiceDiscovery;

namespace ProSuite.Microservices.Client
{
	public abstract class MicroserviceClientBase
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private const string _localhost = "localhost";

		private Health.HealthClient _healthClient;
		private Process _startedProcess;

		protected MicroserviceClientBase([NotNull] string host = _localhost,
		                                 int port = 5151,
		                                 bool useTls = false,
		                                 string clientCertificate = null)
		{
			HostName = host;
			Port = port;
			UseTls = useTls;
			ClientCertificate = clientCertificate;

			if (Port >= 0)
			{
				bool assumeLoadBalancer = host != _localhost;
				OpenChannel(useTls, clientCertificate, assumeLoadBalancer);
			}
			else
			{
				_msg.DebugFormat(
					"Microservice client initialized with port {0}. No channel opened yet.", Port);
			}
		}

		protected MicroserviceClientBase([NotNull] ClientChannelConfig channelConfig)
			: this(channelConfig.HostName, channelConfig.Port, channelConfig.UseTls,
			       channelConfig.ClientCertificate) { }

		public string HostName { get; }

		public int Port { get; private set; }

		protected bool UseTls { get; }
		protected string ClientCertificate { get; }

		[CanBeNull]
		protected Channel Channel { get; set; }

		[NotNull]
		protected abstract string ServiceName { get; }

		public void Disconnect()
		{
			Channel?.ShutdownAsync();

			try
			{
				if (_startedProcess != null && ! _startedProcess.HasExited)
				{
					_startedProcess?.Kill();
				}
			}
			catch (Exception e)
			{
				_msg.Debug($"Error killing the started microserver process {_startedProcess}", e);
			}
		}

		public async Task<bool> AllowStartingLocalServerAsync(
			[NotNull] string executable,
			[CanBeNull] string extraArguments = null)
		{
			if (! HostName.Equals(_localhost, StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}

			bool canAcceptCalls = await CanAcceptCallsAsync();

			if (canAcceptCalls)
			{
				return false;
			}

			StartLocalServer(executable, extraArguments);

			return true;
		}

		public bool AllowStartingLocalServer([NotNull] string executable,
		                                     [CanBeNull] string extraArguments = null)
		{
			if (! HostName.Equals(_localhost, StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}

			if (CanAcceptCalls())
			{
				return false;
			}

			StartLocalServer(executable, extraArguments);

			return true;
		}

		public bool CanAcceptCalls()
		{
			if (! TryGetHealthClient(out Health.HealthClient healthClient))
			{
				return false;
			}

			bool result = GrpcUtils.IsServing(healthClient, ServiceName, out StatusCode statusCode);

			_msg.DebugFormat("Service {0} is serving: {1}. Status: {2}", ServiceName, result,
			                 statusCode);

			return result;
		}

		public async Task<bool> CanAcceptCallsAsync()
		{
			if (! TryGetHealthClient(out Health.HealthClient healthClient))
			{
				return false;
			}

			try
			{
				HealthCheckResponse healthResponse =
					await healthClient.CheckAsync(new HealthCheckRequest()
					                              {Service = ServiceName});

				return healthResponse.Status == HealthCheckResponse.Types.ServingStatus.Serving;
			}
			catch (Exception e)
			{
				_msg.Debug($"Error checking health of service {ServiceName}", e);
				return false;
			}
		}

		protected void OpenChannel(bool useTls,
		                           string clientCertificate = null,
		                           bool assumeLoadBalancer = false)
		{
			if (string.IsNullOrEmpty(HostName))
			{
				_msg.Debug("Host name is null or empty. No channel opened.");
				return;
			}

			ChannelCredentials credentials =
				GrpcUtils.CreateChannelCredentials(useTls, clientCertificate);

			var enoughForLargeGeometries = (int) Math.Pow(1024, 3);

			Channel channel = GrpcUtils.CreateChannel(
				HostName, Port, credentials, enoughForLargeGeometries);

			if (assumeLoadBalancer)
			{
				ChannelIsLoadBalancer =
					IsServingLoadBalancerEndpoint(channel, credentials, ServiceName,
					                              enoughForLargeGeometries);
			}

			Channel = channel;

			_msg.DebugFormat("Created grpc channel to {0} on port {1}", HostName, Port);

			_healthClient = new Health.HealthClient(Channel);

			ChannelOpenedCore(Channel);
		}

		public bool ChannelIsLoadBalancer { get; set; }

		protected abstract void ChannelOpenedCore(Channel channel);

		protected static Channel TryGetChannelFromLoadBalancer(Channel lbChannel,
		                                                       ChannelCredentials credentials,
		                                                       string serviceName,
		                                                       int maxMesssageLength)
		{
			ServiceDiscoveryGrpc.ServiceDiscoveryGrpcClient lbClient =
				new ServiceDiscoveryGrpc.ServiceDiscoveryGrpcClient(lbChannel);

			DiscoverServicesResponse lbResponse = lbClient.DiscoverTopServices(
				new DiscoverServicesRequest
				{
					ServiceName = serviceName,
					MaxCount = 1
				});

			if (lbResponse.ServiceLocations.Count > 0)
			{
				ServiceLocationMsg serviceLocation = lbResponse.ServiceLocations[0];

				Channel result = GrpcUtils.CreateChannel(serviceLocation.HostName,
				                                         serviceLocation.Port, credentials,
				                                         maxMesssageLength);

				_msg.DebugFormat("The load balancer is suggesting {0}", result.ResolvedTarget);

				return result;
			}

			// Assumption: A load balancer is never also serving real requests -> lets not use it at all!
			_msg.Debug("The load balancer has no service locations available.");

			return null;
		}

		private void StartLocalServer(string executable, string extraArguments)
		{
			if (Port < 0)
			{
				// Get next ephemeral port, reopen the channel
				Port = GetFreeTcpPort();
				OpenChannel(false);
			}
			else
			{
				// Kill unhealthy server processes:
				string exeName = Path.GetFileNameWithoutExtension(executable);
				Process[] runningProcesses = Process.GetProcessesByName(exeName);

				if (runningProcesses.Length > 0)
				{
					_msg.DebugFormat(
						"Background microservice {0} is already running (but not " +
						"serving). It will be killed.", exeName);

					foreach (Process process in runningProcesses)
					{
						if (process == Process.GetCurrentProcess())
						{
							// No suicide!
							continue;
						}

						process.Kill();
					}
				}
			}

			string arguments = $"-h {HostName} -p {Port}";

			if (! string.IsNullOrEmpty(extraArguments))
			{
				arguments += $" {extraArguments}";
			}

			_msg.DebugFormat("Starting microservice {0} in background...", executable);

			// TOP-5321: Avoid keeping shared version lock because the child process somehow
			// keeps the lock alive (despite no edit session in the child process) if it is
			// started with useShellExecute == false.
			const bool useShellExecute = true;
			_startedProcess =
				ProcessUtils.StartProcess(executable, arguments, useShellExecute, true);

			_msg.DebugFormat("Started microservice in background. Arguments: {0}", arguments);
		}

		private static bool IsServingLoadBalancerEndpoint(
			[NotNull] Channel channel,
			[NotNull] ChannelCredentials credentials,
			string serviceName,
			int enoughForLargeGeometries)
		{
			var channelHealth = new Health.HealthClient(channel);

			bool isServingEndpoint = GrpcUtils.IsServing(channelHealth, serviceName, out _);

			if (isServingEndpoint)
			{
				return false;
			}

			bool isLoadBalancer = GrpcUtils.IsServing(channelHealth, nameof(ServiceDiscoveryGrpc),
			                                          out StatusCode lbStatusCode);

			if (isLoadBalancer)
			{
				Channel suggestedLocation =
					TryGetChannelFromLoadBalancer(channel, credentials, serviceName,
					                              enoughForLargeGeometries);

				if (suggestedLocation != null)
				{
					_msg.DebugFormat("Using serving load balancer at {0}", channel.ResolvedTarget);
					return true;
				}

				// Assumption: A load balancer is never also serving real requests -> lets not use it at all!
				_msg.Debug(
					"The load balancer has no service locations available. It will not be used.");

				return false;
			}

			_msg.DebugFormat("No {0} service and no serving load balancer at {1}. Error code: {2}",
			                 serviceName, channel.ResolvedTarget, lbStatusCode);

			return false;
		}

		private bool TryGetHealthClient(out Health.HealthClient healthClient)
		{
			healthClient = null;

			if (Port < 0)
			{
				// Avoid waiting for the timeout of the health check, if possible.
				return false;
			}

			if (_healthClient == null)
			{
				return false;
			}

			healthClient = _healthClient;

			return true;
		}

		private static int GetFreeTcpPort()
		{
			TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);

			tcpListener.Start();

			int port = ((IPEndPoint) tcpListener.LocalEndpoint).Port;

			tcpListener.Stop();

			_msg.DebugFormat("Using ephemeral port {0}", port);

			return port;
		}
	}
}
