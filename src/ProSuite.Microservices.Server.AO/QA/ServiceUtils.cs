using System;
using Grpc.Core;
using log4net.Core;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Server.AO.QA
{
	internal static class ServiceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		internal static bool KeepServingOnError(bool defaultValue)
		{
			return EnvironmentUtils.GetBooleanEnvironmentVariableValue(
				"PROSUITE_QA_SERVER_KEEP_SERVING_ON_ERROR", defaultValue);
		}

		internal static void SetUnhealthy([CanBeNull] IServiceHealth serviceHealth,
		                                  [NotNull] Type serviceType)
		{
			if (serviceHealth != null)
			{
				_msg.Warn("Setting service health to \"not serving\" due to exception " +
				          "because the process might be compromised.");

				serviceHealth.SetStatus(serviceType, false);
			}
		}

		internal static void SendFatalException(
			[NotNull] Exception exception,
			IServerStreamWriter<VerificationResponse> responseStream)
		{
			void Write(VerificationResponse r) => responseStream.WriteAsync(r);

			SendFatalException(exception, Write);
		}

		internal static void SendFatalException(
			[NotNull] Exception exception,
			IServerStreamWriter<DataVerificationResponse> responseStream)
		{
			void Write(VerificationResponse r) =>
				responseStream.WriteAsync(new DataVerificationResponse { Response = r });

			SendFatalException(exception, Write);
		}

		internal static void SendFatalException(
			[NotNull] Exception exception,
			Action<VerificationResponse> writeAsync)
		{
			var response = new VerificationResponse();

			response.ServiceCallStatus = (int) ServiceCallStatus.Failed;

			if (! string.IsNullOrEmpty(exception.Message))
			{
				response.Progress = new VerificationProgressMsg
				                    {
					                    Message = exception.Message
				                    };
			}

			try
			{
				writeAsync(response);
			}
			catch (InvalidOperationException ex)
			{
				// For example: System.InvalidOperationException: Only one write can be pending at a time
				_msg.Warn("Error sending progress to the client", ex);
			}
		}

		internal static void SendFatalException(
			[NotNull] Exception exception,
			IServerStreamWriter<StandaloneVerificationResponse> responseStream)
		{
			MessagingUtils.SendResponse(responseStream,
			                            new StandaloneVerificationResponse()
			                            {
				                            Message = new LogMsg()
				                                      {
					                                      Message = exception.Message,
					                                      MessageLevel = Level.Error.Value
				                                      },
				                            ServiceCallStatus = (int) ServiceCallStatus.Failed
			                            });
		}
	}
}
