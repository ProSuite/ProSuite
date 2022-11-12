using System;
using System.Linq;
using System.Text;
using System.Threading;
using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Client.AGP.GeometryProcessing
{
	public static class RpcCallUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static T Try<T>(
			[NotNull] Func<CallOptions, T> func,
			CancellationToken cancellationToken,
			int deadlineMilliseconds = 30000,
			bool noWarn = false)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Operation cancelled");
				return default;
			}

			CallOptions callOptions =
				new CallOptions(null, DateTime.UtcNow.AddMilliseconds(deadlineMilliseconds),
				                cancellationToken);

			T result;
			try
			{
				result = func(callOptions);
			}
			catch (RpcException rpcException)
			{
				_msg.Debug("Exception received from server", rpcException);

				const string exceptionBinKey = "exception-bin";

				if (rpcException.Trailers.Any(t => t.Key.Equals(exceptionBinKey)))
				{
					byte[] bytes = rpcException.Trailers.GetValueBytes(exceptionBinKey);

					if (bytes != null)
					{
						string serverException = Encoding.UTF8.GetString(bytes);
						_msg.DebugFormat("Server call stack: {0}", serverException);
					}
				}

				string message = rpcException.Status.Detail;

				if (rpcException.StatusCode == StatusCode.Cancelled)
				{
					Log("Operation cancelled", noWarn);
					return default;
				}

				if (rpcException.StatusCode == StatusCode.DeadlineExceeded)
				{
					Log("Operation timed out", noWarn);
					return default;
				}

				throw new Exception(message, rpcException);
			}

			return result;
		}

		private static void Log(string message, bool noWarn)
		{
			if (noWarn)
			{
				_msg.Debug(message);
			}
			else
			{
				_msg.Warn(message);
			}
		}
	}
}
