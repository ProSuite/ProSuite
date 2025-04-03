using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Health.V1;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using Quaestor.ServiceDiscovery;

namespace ProSuite.Microservices.Client
{
	public abstract class MicroserviceClientBase : IMicroserviceClient
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _localhost = "localhost";

		private Health.HealthClient _healthClient;
		private Process _startedProcess;

		[CanBeNull] private readonly IList<IClientChannelConfig> _allChannelConfigs;

		private string _executable;
		private string _executableArguments;

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
				OpenChannel(useTls, clientCertificate);
			}
			else
			{
				_msg.DebugFormat(
					"Microservice client initialized with port {0}. No channel opened yet.", Port);
			}
		}

		protected MicroserviceClientBase([NotNull] IClientChannelConfig channelConfig)
			: this(channelConfig.HostName, channelConfig.Port, channelConfig.UseTls,
			       channelConfig.ClientCertificate) { }

		protected MicroserviceClientBase([NotNull] IList<IClientChannelConfig> channelConfigs)
			: this(channelConfigs[0])
		{
			if (channelConfigs.Count > 1)
			{
				_allChannelConfigs = channelConfigs;
			}
		}

		public string HostName { get; private set; }

		public int Port { get; private set; }

		public bool UseTls { get; private set; }

		protected string ClientCertificate { get; private set; }

		[CanBeNull]
		protected ChannelBase Channel { get; private set; }

		public bool ChannelIsLoadBalancer { get; private set; }

		[NotNull]
		public abstract string ServiceName { get; }

		[NotNull]
		public abstract string ServiceDisplayName { get; }

		[NotNull]
		protected string ChannelServiceName =>
			ChannelIsLoadBalancer ? nameof(ServiceDiscoveryGrpc) : ServiceName;

		public bool CanFailOver => _allChannelConfigs?.Count > 1;

		public bool ProcessStarted => _startedProcess != null && ! _startedProcess.HasExited;

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
				_msg.Debug($"Error killing the started service process {_startedProcess}", e);
			}
		}

		public async Task<bool> AllowStartingLocalServerAsync(
			string executable,
			string extraArguments = null)
		{
			// Remember in case we need to switch to different channel or re-start later on
			_executable = executable;
			_executableArguments = extraArguments;

			if (! HostName.Equals(_localhost, StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}

			bool canAcceptCalls = await CanAcceptCallsAsync().ConfigureAwait(false);

			if (canAcceptCalls)
			{
				return false;
			}

			StartLocalServer(executable, extraArguments);

			return true;
		}

		public bool AllowStartingLocalServer(string executable,
		                                     string extraArguments = null)
		{
			// Remember in case we need to switch to different channel or re-start later on
			_executable = executable;
			_executableArguments = extraArguments;

			if (! HostName.Equals(_localhost, StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}

			if (CanAcceptCalls(false))
			{
				return false;
			}

			StartLocalServer(executable, extraArguments);

			return true;
		}

		public bool TryRestart()
		{
			if (_executable == null)
			{
				return false;
			}

			StartLocalServer(_executable, _executableArguments);

			return true;
		}

		public bool CanAcceptCalls(bool allowFailOver = false,
		                           bool logOnlyIfUnhealthy = false)
		{
			if (! TryGetHealthClient(out Health.HealthClient healthClient))
			{
				return false;
			}

			string serviceName = ChannelServiceName;

			Stopwatch stopwatch = Stopwatch.StartNew();

			bool result =
				GrpcClientUtils.IsServing(healthClient, serviceName, out StatusCode statusCode);

			if (! result || ! logOnlyIfUnhealthy)
			{
				LogHealthStatus(statusCode, stopwatch.ElapsedMilliseconds);
			}

			if (! result && allowFailOver)
			{
				result = TryOpenOtherChannel();
			}

			return result;
		}

		public async Task<bool> CanAcceptCallsAsync(bool allowFailOver = false)
		{
			if (! TryGetHealthClient(out Health.HealthClient healthClient))
			{
				return false;
			}

			string serviceName = ChannelServiceName;

			Stopwatch stopwatch = Stopwatch.StartNew();

			StatusCode statusCode = await GrpcClientUtils.IsServingAsync(healthClient, serviceName)
			                                             .ConfigureAwait(false);

			LogHealthStatus(statusCode, stopwatch.ElapsedMilliseconds);

			if (statusCode != StatusCode.OK && allowFailOver)
			{
				return TryOpenOtherChannel();
			}

			return statusCode == StatusCode.OK;
		}

		public async Task<int> GetWorkerServiceCountAsync()
		{
			if (! ChannelIsLoadBalancer)
			{
				return 1;
			}

			ServiceDiscoveryGrpc.ServiceDiscoveryGrpcClient lbClient =
				new ServiceDiscoveryGrpc.ServiceDiscoveryGrpcClient(Channel);

			string serviceName = ServiceName;

			var discoverServicesRequest = new DiscoverServicesRequest
			                              {
				                              ServiceName = serviceName
			                              };

			DiscoverServicesResponse lbResponse = await lbClient.DiscoverServicesAsync(
				                                      discoverServicesRequest);

			return lbResponse.ServiceLocations.Count;
		}

		public abstract string GetAddress();

		public abstract string GetChannelState();

		protected void OpenChannel(bool useTls,
		                           string clientCertificate = null)
		{
			if (string.IsNullOrEmpty(HostName))
			{
				_msg.DebugFormat("{0}: Host name is null or empty. No channel opened.",
				                 ServiceDisplayName);
				return;
			}

			ChannelCredentials credentials =
				GrpcClientUtils.CreateChannelCredentials(useTls, clientCertificate);

			var enoughForLargeGeometries = (int) Math.Pow(1024, 3);

			ChannelBase channel =
				OpenChannelCore(HostName, Port, credentials, enoughForLargeGeometries);

			bool assumeLoadBalancer =
				! HostName.Equals(_localhost, StringComparison.InvariantCultureIgnoreCase);

			if (assumeLoadBalancer)
			{
				_msg.Debug("Checking if the specified channel is a load-balancer channel " +
				           "(because the host name is not 'localhost')...");
				ChannelIsLoadBalancer = IsLoadBalancerEndpoint(channel, credentials, ServiceName,
				                                               enoughForLargeGeometries);
			}
			else
			{
				// Could be failing-over back to non-LB channel:
				ChannelIsLoadBalancer = false;
			}

			Channel = channel;

			_msg.DebugFormat("Created grpc channel to {0} on port {1} for {2}", HostName, Port,
			                 ServiceDisplayName);

			_healthClient = new Health.HealthClient(Channel);

			ChannelOpenedCore(Channel);
		}

		protected abstract ChannelBase OpenChannelCore(
			[NotNull] string host,
			int port,
			[NotNull] ChannelCredentials credentials,
			int maxMessageLength);

		protected bool TryOpenOtherChannel()
		{
			if (_allChannelConfigs == null)
			{
				_msg.DebugFormat(
					"No fail-over connections defined, trying the same channel again...");

				return RetrySameChannel();
			}

			string currentHost = HostName;
			int currentPort = Port;

			foreach (IClientChannelConfig otherChannelConfig in _allChannelConfigs)
			{
				if (otherChannelConfig.HostName == currentHost &&
				    otherChannelConfig.Port == currentPort)
				{
					// This is the one currently being used. We want to check the others only.
					continue;
				}

				_msg.DebugFormat("Trying alternate channel {0}...", otherChannelConfig);

				HostName = otherChannelConfig.HostName;
				Port = otherChannelConfig.Port;
				UseTls = otherChannelConfig.UseTls;
				ClientCertificate = otherChannelConfig.ClientCertificate;

				OpenChannel(otherChannelConfig.UseTls, otherChannelConfig.ClientCertificate);

				// In case of localhost and known server exe:
				if (! string.IsNullOrEmpty(_executable) &&
				    AllowStartingLocalServer(_executable, _executableArguments))
				{
					return true;
				}

				if (CanAcceptCalls(allowFailOver: false))
				{
					return true;
				}
			}

			return false;
		}

		protected abstract void ChannelOpenedCore(ChannelBase channel);

		protected ChannelBase TryGetChannelFromLoadBalancer(ChannelBase lbChannel,
		                                                    ChannelCredentials credentials,
		                                                    string serviceName,
		                                                    int maxMessageLength)
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

				ChannelBase result = OpenChannelCore(serviceLocation.HostName, serviceLocation.Port,
				                                     credentials, maxMessageLength);

				_msg.DebugFormat("The load balancer is suggesting {0} for the {1}",
				                 result.Target, ServiceDisplayName);

				return result;
			}

			// Assumption: A load balancer is never also serving real requests -> lets not use it at all!
			_msg.DebugFormat("The load balancer has no service locations available for the {0}.",
			                 ServiceDisplayName);

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
						"Background microservice {0} is already running (but not serving). " +
						"It will be killed.", exeName);

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

			_msg.DebugFormat("Started microservice {0} in background. Arguments: {1}",
			                 Path.GetFileNameWithoutExtension(executable), arguments);
		}

		private bool IsLoadBalancerEndpoint(
			[NotNull] ChannelBase channel,
			[NotNull] ChannelCredentials credentials,
			string serviceName,
			int enoughForLargeGeometries)
		{
			// TODO: Consider renaming to DetermineEndpointType() that directly sets both the
			// properties IsLoadBalancer and IsServingEndpoint which can be checked during the
			// very first actual request and, if necessary re-evaluate DetermineEndpointType().
			// Or better, a single property (enum EndpointType {LoadBalancer,Service,Unknown}).
			// This would make the start-up sequence more robust.
			var channelHealth = new Health.HealthClient(channel);

			bool isServingEndpoint = GrpcClientUtils.IsServing(channelHealth, serviceName, out _);

			if (isServingEndpoint)
			{
				return false;
			}

			bool isLoadBalancer = GrpcClientUtils.IsServing(
				channelHealth, nameof(ServiceDiscoveryGrpc),
				out StatusCode lbStatusCode);

			if (isLoadBalancer)
			{
				_msg.DebugFormat("{0} is a load balancer address.", channel.Target);

				ChannelBase suggestedLocation =
					TryGetChannelFromLoadBalancer(channel, credentials, serviceName,
					                              enoughForLargeGeometries);

				if (suggestedLocation != null)
				{
					_msg.DebugFormat("Using serving load balancer at {0} to connect to {1}",
					                 channel.Target, ServiceDisplayName);
				}
				else
				{
					// Let's hope the load balancer will pick up a few service addresses until the first request!
					_msg.WarnFormat(
						"The load balancer at {0} has no service locations available for {1}. " +
						"Unless it can pick up some service locations in the mean time, all " +
						"requests will fail!", channel.Target, ServiceDisplayName);
				}

				return true;
			}

			_msg.DebugFormat("No {0} service and no serving load balancer at {1}. Error code: {2}",
			                 serviceName, channel.Target, lbStatusCode);

			// We don't know yet. However, it could be working later on (see TODO above).
			return false;
		}

		private bool RetrySameChannel()
		{
			// TEST for TOP-5412 (re-use same channel or always create new channel?)

			if (CanAcceptCalls(allowFailOver: false))
			{
				// TODO: Consider changing the time-out
				_msg.DebugFormat("Second try worked!");

				return true;
			}

			_msg.DebugFormat(
				"Second try failed as well, creating a new channel and trying for the third (and last) time.");

			OpenChannel(UseTls, ClientCertificate);

			if (CanAcceptCalls(allowFailOver: false))
			{
				_msg.DebugFormat("Third try worked!");

				return true;
			}

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

		private void LogHealthStatus(StatusCode statusCode,
		                             long latencyMilliseconds)
		{
			string address = GetAddress();

			_msg.DebugFormat("Health status for service {0} at {1}: {2}. " +
			                 "Channel state: {3}. Latency: {4}ms",
			                 ChannelServiceName, address, statusCode, GetChannelState(),
			                 latencyMilliseconds);
		}

		private int GetFreeTcpPort()
		{
			TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);

			tcpListener.Start();

			int port = ((IPEndPoint) tcpListener.LocalEndpoint).Port;

			tcpListener.Stop();

			_msg.DebugFormat("Using ephemeral port {0} to connect to {1}", port,
			                 ServiceDisplayName);

			return port;
		}
	}
}
