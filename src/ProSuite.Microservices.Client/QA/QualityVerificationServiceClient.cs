using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Grpc.Core;
using Grpc.Health.V1;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.QA
{
	public class QualityVerificationServiceClient
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly string _host;
		private readonly int _port;

		public QualityVerificationGrpc.QualityVerificationGrpcClient QaClient { get; }

		private readonly Health.HealthClient _healthClient;

		private Process _startedProcess;

		public QualityVerificationServiceClient([NotNull] string host = "localhost",
		                                        int port = 5151)
		{
			_host = host;
			_port = port;

			var enoughForLargeGeometries = (int) Math.Pow(1024, 3);

			Channel channel = GrpcUtils.CreateChannel(
				_host, _port, ChannelCredentials.Insecure,
				enoughForLargeGeometries);

			_healthClient = new Health.HealthClient(channel);
			QaClient = new QualityVerificationGrpc.QualityVerificationGrpcClient(channel);
		}

		public void AllowStartingLocalServer(string executable)
		{
			if (! CanAcceptCalls())
			{
				string arguments = $"-h {_host} -p {_port}";

				string exeName = Path.GetFileNameWithoutExtension(executable);
				Process[] runningProcesses = Process.GetProcessesByName(exeName);

				if (runningProcesses.Length > 0)
				{
					_msg.Debug(
						"Background QA microservice is already running (but not serving). It will be killed.");

					foreach (Process process in runningProcesses)
					{
						process.Kill();
					}
				}

				_msg.Debug("Starting QA microservice in background...");

				_startedProcess = ProcessUtils.StartProcess(executable, arguments, false);

				// Drain the output, otherwise the process hangs once the buffer is full:
				_startedProcess.BeginOutputReadLine();
				_startedProcess.BeginErrorReadLine();
			}
		}

		public bool CanAcceptCalls()
		{
			try
			{
				string serviceName = Assert.NotNull(GetType().BaseType).DeclaringType?.Name;

				HealthCheckResponse healthResponse =
					_healthClient.Check(new HealthCheckRequest()
					                    {Service = serviceName});

				return healthResponse.Status == HealthCheckResponse.Types.ServingStatus.Serving;
			}
			catch (Exception e)
			{
				_msg.Debug("Error checking health of qa service", e);
				return false;
			}
		}
	}
}
