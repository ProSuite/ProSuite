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

namespace ProSuite.Microservices.Client
{
	public abstract class MicroserviceClientBase
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private const string _localhost = "localhost";

		private readonly string _host;
		private int _port;
		private Health.HealthClient _healthClient;
		private Process _startedProcess;

		protected MicroserviceClientBase([NotNull] string host = _localhost,
		                                 int port = 5151,
		                                 bool useTls = false,
		                                 string clientCertificate = null)
		{
			_host = host;
			_port = port;

			if (_port >= 0)
			{
				OpenChannel(useTls, clientCertificate);
			}
			else
			{
				_msg.DebugFormat(
					"Microservice client initialized with port {0}. No channel opened yet.", _port);
			}
		}

		protected MicroserviceClientBase([NotNull] ClientChannelConfig channelConfig)
			: this(channelConfig.HostName, channelConfig.Port, channelConfig.UseTls,
			       channelConfig.ClientCertificate) { }

		[CanBeNull]
		protected Channel Channel { get; set; }

		[CanBeNull]
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

		public async Task<bool> AllowStartingLocalServerAsync([NotNull] string executable,
		                                                      [CanBeNull] string extraArguments =
			                                                      null)
		{
			if (! _host.Equals(_localhost, StringComparison.InvariantCultureIgnoreCase))
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

		public void AllowStartingLocalServer([NotNull] string executable,
		                                     [CanBeNull] string extraArguments = null)
		{
			if (! _host.Equals(_localhost, StringComparison.InvariantCultureIgnoreCase))
			{
				return;
			}

			if (CanAcceptCalls())
			{
				return;
			}

			StartLocalServer(executable, extraArguments);
		}

		public bool CanAcceptCalls()
		{
			if (! TryGetHealthClient(out Health.HealthClient healthClient))
			{
				return false;
			}

			try
			{
				HealthCheckResponse healthResponse =
					healthClient.Check(new HealthCheckRequest()
					                   {Service = ServiceName});

				return healthResponse.Status == HealthCheckResponse.Types.ServingStatus.Serving;
			}
			catch (Exception e)
			{
				_msg.Debug($"Error checking health of service {ServiceName}", e);
				return false;
			}
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

		protected void OpenChannel(bool useTls, string clientCertificate = null)
		{
			if (string.IsNullOrEmpty(_host))
			{
				_msg.Debug("Host name is null or empty. No channel opened.");
				return;
			}

			var enoughForLargeGeometries = (int) Math.Pow(1024, 3);

			ChannelCredentials credentials =
				GrpcUtils.CreateChannelCredentials(useTls, clientCertificate);

			Channel = GrpcUtils.CreateChannel(
				_host, _port, credentials, enoughForLargeGeometries);

			_msg.DebugFormat("Created grpc channel to {0} on port {1}", _host, _port);

			_healthClient = new Health.HealthClient(Channel);

			ChannelOpenedCore(Channel);
		}

		protected abstract void ChannelOpenedCore(Channel channel);

		private void StartLocalServer(string executable, string extraArguments)
		{
			if (_port < 0)
			{
				// Get next ephemeral port, reopen the channel
				_port = GetFreeTcpPort();
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
						process.Kill();
					}
				}
			}

			string arguments = $"-h {_host} -p {_port}";

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

		private bool TryGetHealthClient(out Health.HealthClient healthClient)
		{
			healthClient = null;

			if (_port < 0)
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
