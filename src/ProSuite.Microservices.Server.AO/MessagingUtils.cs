using System;
using System.Threading.Tasks;
using Grpc.Core;
using log4net.Appender;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Server.AO
{
	public static class MessagingUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static IDisposable TemporaryRootAppender([CanBeNull] IAppender appender)
		{
			if (appender == null)
			{
				return null;
			}

			Log4NetUtils.AddRootAppender(appender);
			return new DisposableCallback(() => Log4NetUtils.RemoveRootAppender(appender));
		}

		public static bool TrySendResponse<T>([NotNull] IServerStreamWriter<T> responseStream,
		                                      [NotNull] T response)
		{
			try
			{
				responseStream.WriteAsync(response);
			}
			catch (InvalidOperationException ex)
			{
				// For example: System.InvalidOperationException: Only one write can be pending at a time
				_msg.VerboseDebug("Error sending response to the client", ex);

				return false;
			}

			return true;
		}

		public static void SendResponse<T>([NotNull] IServerStreamWriter<T> responseStream,
		                                   [NotNull] T response)
		{
			if (TrySendResponse(responseStream, response))
			{
				return;
			}

			// Re-try (typically for the final message containing some extra information):
			_msg.Debug("Error sending progress to the client. Retrying in 1s...");

			Task.Delay(1000);
			responseStream.WriteAsync(response);
		}
	}
}
