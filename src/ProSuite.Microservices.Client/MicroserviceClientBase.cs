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
	public abstract class MicroserviceClientBase
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
		protected Channel Channel { get; private set; }

		public bool ChannelIsLoadBalancer { get; private set; }

		[NotNull]
		public abstract string ServiceName { get; }

		[NotNull]
		public abstract string ServiceDisplayName { get; }

		[NotNull]
		private string ChannelServiceName =>
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
			[NotNull] string executable,
			[CanBeNull] string extraArguments = null)
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

		public bool AllowStartingLocalServer([NotNull] string executable,
		                                     [CanBeNull] string extraArguments = null)
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

		public bool CanAcceptCalls(bool allowFailOver = false)
		{
			if (! TryGetHealthClient(out Health.HealthClient healthClient))
			{
				return false;
			}

			string serviceName = ChannelServiceName;

			bool result = GrpcUtils.IsServing(healthClient, serviceName, out StatusCode statusCode);

			LogHealthStatus(statusCode);

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

			StatusCode statusCode = await GrpcUtils.IsServingAsync(healthClient, serviceName)
			                                       .ConfigureAwait(false);

			LogHealthStatus(statusCode);

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
				GrpcUtils.CreateChannelCredentials(useTls, clientCertificate);

			var enoughForLargeGeometries = (int) Math.Pow(1024, 3);

			Channel channel = GrpcUtils.CreateChannel(
				HostName, Port, credentials, enoughForLargeGeometries);

			bool assumeLoadBalancer =
				! HostName.Equals(_localhost, StringComparison.InvariantCultureIgnoreCase);

			if (assumeLoadBalancer)
			{
				_msg.Debug(
					"Checking if the specified channel is a load-balancer channel (host is not localhost)...");
				ChannelIsLoadBalancer =
					IsServingLoadBalancerEndpoint(channel, credentials, ServiceName,
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

			foreach (IClientChannelConfig otherChannel in _allChannelConfigs)
			{
				if (otherChannel.HostName == currentHost &&
				    otherChannel.Port == currentPort)
				{
					// This is the one currently being used. We want to check the others only.
					continue;
				}

				_msg.DebugFormat("Trying alternate channel {0}...", otherChannel);

				HostName = otherChannel.HostName;
				Port = otherChannel.Port;
				UseTls = otherChannel.UseTls;
				ClientCertificate = otherChannel.ClientCertificate;

				OpenChannel(otherChannel.UseTls, otherChannel.ClientCertificate);

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

		protected abstract void ChannelOpenedCore(Channel channel);

		protected Channel TryGetChannelFromLoadBalancer(Channel lbChannel,
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

				Channel result = GrpcUtils.CreateChannel(serviceLocation.HostName,
				                                         serviceLocation.Port, credentials,
				                                         maxMessageLength);

				_msg.DebugFormat("The load balancer is suggesting {0} for the {1}",
				                 result.ResolvedTarget, ServiceDisplayName);

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

		private bool IsServingLoadBalancerEndpoint(
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
				_msg.DebugFormat("{0} is a load balancer address.", channel.ResolvedTarget);

				Channel suggestedLocation =
					TryGetChannelFromLoadBalancer(channel, credentials, serviceName,
					                              enoughForLargeGeometries);

				if (suggestedLocation != null)
				{
					_msg.DebugFormat("Using serving load balancer at {0} to connect to {1}",
					                 channel.ResolvedTarget, ServiceDisplayName);
					return true;
				}

				// Assumption: A load balancer is never also serving real requests -> lets not use it at all!
				_msg.DebugFormat(
					"The load balancer has no service locations available for {0}. It will not be used.",
					ServiceDisplayName);

				return false;
			}

			_msg.DebugFormat("No {0} service and no serving load balancer at {1}. Error code: {2}",
			                 serviceName, channel.ResolvedTarget, lbStatusCode);

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

		private void LogHealthStatus(StatusCode statusCode)
		{
			// In shutdown state, the ResolvedTarget property throws for certain:
			string address = "<none>";
			if (Channel?.State != ChannelState.Shutdown)
			{
				try
				{
					address = Channel?.ResolvedTarget;
				}
				catch (Exception e)
				{
					_msg.Debug($"Error resolving target address for {ChannelServiceName}", e);
				}
			}

			_msg.DebugFormat("Health status for service {0} at {1}: {2}. Channel state: {3}",
			                 ChannelServiceName, address, statusCode, Channel?.State);
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
