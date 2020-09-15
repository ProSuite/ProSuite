using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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

		private readonly string _host;
		private readonly int _port;
		private readonly Health.HealthClient _healthClient;
		private Process _startedProcess;

		protected MicroserviceClientBase([NotNull] string host = "localhost",
		                                 int port = 5151,
		                                 string serverCertificatePath = null)
		{
			_host = host;
			_port = port;

			var enoughForLargeGeometries = (int) Math.Pow(1024, 3);

			ChannelCredentials credentials =
				string.IsNullOrEmpty(serverCertificatePath)
					? ChannelCredentials.Insecure
					: new SslCredentials(File.ReadAllText(serverCertificatePath));

			Channel = GrpcUtils.CreateChannel(
				_host, _port, credentials, enoughForLargeGeometries);

			_msg.DebugFormat("Created grpc channel to {0} on port {1}", _host, _port);

			_healthClient = new Health.HealthClient(Channel);
		}

		protected MicroserviceClientBase([NotNull] ClientChannelConfig channelConfig)
			: this(channelConfig.HostName, channelConfig.Port,
			       channelConfig.ServerCertificateFile) { }

		protected Channel Channel { get; }

		public void Disconnect()
		{
			Channel.ShutdownAsync();

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

		public void AllowStartingLocalServer(string executable)
		{
			if (_host.Equals("localhost", StringComparison.InvariantCultureIgnoreCase) &&
			    ! CanAcceptCalls())
			{
				string arguments = $"-h {_host} -p {_port}";

				string exeName = Path.GetFileNameWithoutExtension(executable);
				Process[] runningProcesses = Process.GetProcessesByName(exeName);

				if (runningProcesses.Length > 0)
				{
					_msg.DebugFormat("Background microservice {0} is already running (but not " +
					                 "serving). It will be killed.", exeName);

					foreach (Process process in runningProcesses)
					{
						process.Kill();
					}
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
		}

		public bool CanAcceptCalls()
		{
			string serviceName = null;

			try
			{
				serviceName = ServiceName;

				HealthCheckResponse healthResponse =
					_healthClient.Check(new HealthCheckRequest()
					                    {Service = serviceName});

				return healthResponse.Status == HealthCheckResponse.Types.ServingStatus.Serving;
			}
			catch (Exception e)
			{
				_msg.Debug($"Error checking health of service {serviceName}", e);
				return false;
			}
		}

		protected abstract string ServiceName { get; }
	}
}
