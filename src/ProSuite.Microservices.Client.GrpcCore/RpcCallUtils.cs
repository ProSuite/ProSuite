using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Client.GrpcCore
{
	public static class RpcCallUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Deadline to be used by geometry calls. It is important to use a dead-line otherwise
		/// blocking server calls can make Pro hang forever.
		/// </summary>
		public static int GeometryDefaultDeadline { get; set; } = GetToolDefaultDeadline();

		public static async Task<T> TryAsync<T>(Func<CallOptions, Task<T>> func,
		                                        CancellationToken cancellationToken,
		                                        int deadlineMilliseconds = 30000,
		                                        bool noWarn = false)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Operation cancelled");
				return default;
			}

			CallOptions callOptions = GetCallOptions<T>(cancellationToken, deadlineMilliseconds);

			T result;
			try
			{
				result = await func(callOptions);
			}
			catch (RpcException rpcException)
			{
				return HandleRpcException<T>(rpcException, noWarn);
			}

			return result;
		}

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

			CallOptions callOptions = GetCallOptions<T>(cancellationToken, deadlineMilliseconds);

			T result;
			try
			{
				result = func(callOptions);
			}
			catch (RpcException rpcException)
			{
				return HandleRpcException<T>(rpcException, noWarn);
			}

			return result;
		}

		private static CallOptions GetCallOptions<T>(CancellationToken cancellationToken,
		                                             int deadlineMilliseconds)
		{
			CallOptions callOptions =
				new CallOptions(null, DateTime.UtcNow.AddMilliseconds(deadlineMilliseconds),
				                cancellationToken);
			return callOptions;
		}

		private static T HandleRpcException<T>(RpcException rpcException, bool noWarn)
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

		private static int GetToolDefaultDeadline()
		{
			string envVarValue =
				Environment.GetEnvironmentVariable("PROSUITE_TOOLS_RPC_DEADLINE_MS");

			if (! string.IsNullOrEmpty(envVarValue) &&
			    int.TryParse(envVarValue, out int deadlineMilliseconds))
			{
				return deadlineMilliseconds;
			}

			// Default;
			return 5000;
		}
	}
}
