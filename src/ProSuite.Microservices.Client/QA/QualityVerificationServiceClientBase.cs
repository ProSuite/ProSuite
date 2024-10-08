using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Definitions.QA;
using Quaestor.LoadReporting;

namespace ProSuite.Microservices.Client.QA
{
	public abstract class QualityVerificationServiceClientBase : MicroserviceClientBase,
	                                                             IQualityVerificationClient
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private QualityVerificationGrpc.QualityVerificationGrpcClient _staticQaClient;
		private QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient _staticDdxClient;

		protected QualityVerificationServiceClientBase([NotNull] IClientChannelConfig channelConfig)
			: base(channelConfig) { }

		protected QualityVerificationServiceClientBase(
			[NotNull] IList<IClientChannelConfig> channelConfigs)
			: base(channelConfigs) { }

		protected QualityVerificationServiceClientBase([NotNull] string host,
		                                               int port = 5151,
		                                               bool useTls = false,
		                                               string clientCertificate = null)
			: base(host, port, useTls, clientCertificate) { }

		public override string ServiceName => nameof(QualityVerificationGrpc);

		public override string ServiceDisplayName => "Quality Verification Service";

		public QualityVerificationGrpc.QualityVerificationGrpcClient QaGrpcClient
		{
			get
			{
				if (_staticQaClient != null)
				{
					return _staticQaClient;
				}

				if (ChannelIsLoadBalancer)
				{
					ChannelBase actualChannel = GetBalancedChannel();

					return new QualityVerificationGrpc.QualityVerificationGrpcClient(actualChannel);
				}

				throw new InvalidOperationException(
					"Neither a static channel nor a load balancer channel has been opened.");
			}
		}

		public QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient DdxClient
		{
			get
			{
				if (_staticDdxClient != null)
				{
					return _staticDdxClient;
				}

				if (ChannelIsLoadBalancer)
				{
					ChannelBase actualChannel = GetBalancedChannel();

					return new QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient(
						actualChannel);
				}

				throw new InvalidOperationException(
					"Neither a static channel nor a load balancer channel has been opened.");
			}
		}

		protected override void ChannelOpenedCore(ChannelBase channel)
		{
			// In case of fail-over from a fixed address to a load-balancer:
			if (ChannelIsLoadBalancer)
			{
				_staticQaClient = null;
				_staticDdxClient = null;
			}
			else
			{
				_staticQaClient =
					new QualityVerificationGrpc.QualityVerificationGrpcClient(channel);
				_staticDdxClient =
					new QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient(channel);
			}
		}

		public bool TryGetRunningRequestCount(TimeSpan timeOut, out int runningRequestCount)
		{
			runningRequestCount = -1;

			if (Channel == null)
			{
				return false;
			}

			try
			{
				Task<LoadReportResponse> loadReport =
					GetLoadReport(Channel, ServiceName, null, timeOut);

				runningRequestCount = loadReport.Result.ServerStats.CurrentRequests;
			}
			catch (TimeoutException)
			{
				_msg.DebugFormat(
					"Service location {0} took longer than {timeout}s. Cannot get running request count.",
					GetAddress(), timeOut.TotalSeconds);

				return false;
			}
			catch (Exception e)
			{
				_msg.Debug(
					$"Service location {GetAddress()} failed. Cannot get running request count.",
					e);

				return false;
			}

			return true;
		}

		public IEnumerable<IQualityVerificationClient> GetWorkerClients(
			int maxCount)
		{
			Assert.True(ChannelIsLoadBalancer,
			            "The client must have a channel to a load balancer.");

			ChannelBase lbChannel = Assert.NotNull(Channel);

			foreach (var serviceLocation in GrpcClientUtils.GetServiceLocationsFromLoadBalancer(
				         lbChannel, ServiceName, maxCount))
			{
				yield return CreateClient(
					serviceLocation.HostName, serviceLocation.Port, UseTls, ClientCertificate);
			}
		}

		protected abstract IQualityVerificationClient CreateClient(
			[NotNull] string hostName,
			int port,
			bool useTls,
			[CanBeNull] string clientCertificate);

		private ChannelBase GetBalancedChannel()
		{
			ChannelCredentials credentials =
				GrpcClientUtils.CreateChannelCredentials(UseTls, ClientCertificate);

			var enoughForLargeGeometries = (int) Math.Pow(1024, 3);

			ChannelBase actualChannel = TryGetChannelFromLoadBalancer(
				Channel, credentials, ServiceName,
				enoughForLargeGeometries);

			if (actualChannel == null)
			{
				if (TryOpenOtherChannel())
				{
					actualChannel = TryGetChannelFromLoadBalancer(
						Channel, credentials, ServiceName,
						enoughForLargeGeometries);
				}
				else
				{
					throw new InvalidOperationException(
						"Load balancer has not provided a valid channel");
				}
			}

			return actualChannel;
		}

		[NotNull]
		private static async Task<LoadReportResponse> GetLoadReport(
			[NotNull] ChannelBase channel,
			[NotNull] string serviceName,
			[CanBeNull] string serviceScope,
			TimeSpan timeout)
		{
			var loadRequest = new LoadReportRequest
			                  {
				                  Scope = serviceScope ?? string.Empty,
				                  ServiceName = serviceName
			                  };

			LoadReportingGrpc.LoadReportingGrpcClient loadClient =
				new LoadReportingGrpc.LoadReportingGrpcClient(channel);

			LoadReportResponse loadReportResponse = await TimeoutAfter(
				                                        GetLoadReport(loadClient, loadRequest),
				                                        timeout);

			return loadReportResponse;
		}

		private static async Task<LoadReportResponse> GetLoadReport(
			[NotNull] LoadReportingGrpc.LoadReportingGrpcClient loadClient,
			[NotNull] LoadReportRequest loadRequest)
		{
			return await loadClient.ReportLoadAsync(loadRequest);
		}

		[NotNull]
		private static async Task<TResult> TimeoutAfter<TResult>([NotNull] Task<TResult> task,
		                                                         TimeSpan timeout)
		{
			using (var timeoutCancellationTokenSource = new CancellationTokenSource())
			{
				var completedTask = await Task.WhenAny(task,
				                                       Task.Delay(
					                                       timeout,
					                                       timeoutCancellationTokenSource.Token));

				if (completedTask == task)
				{
					timeoutCancellationTokenSource.Cancel();
					return await task;
				}

				throw new TimeoutException("The operation has timed out.");
			}
		}
	}
}
