using System;
using System.ServiceProcess;
using System.Timers;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Microservices.Server.AO
{
	/// <summary>
	/// Windows service that wraps a Grpc service implementation T.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class GrpcWindowsService<T> : ServiceBase where T : class
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly Func<string[], StartedGrpcServer<T>> _serverStart;

		[CanBeNull] private Grpc.Core.Server _server;

		private IServiceHealth _health;
		private Timer _timer;
		private readonly double _interval = 3 * 1000;

		public GrpcWindowsService(
			[NotNull] string serviceName,
			[NotNull] Func<string[], StartedGrpcServer<T>> serverStart)
		{
			_serverStart = serverStart;
			ServiceName = serviceName;
		}

		public bool HasBeenStarted { get; private set; }

		private void StartHealthChecking(IServiceHealth health)
		{
			_health = health;

			if (_health == null)
			{
				return;
			}

			if (_timer != null)
			{
				_msg.Info("Health-check timer is already running.");
				return;
			}

			_timer = new Timer(_interval) { AutoReset = true };

			_timer.Elapsed += _timer_Elapsed;
			_timer.Start();
		}

		protected override void OnStart(string[] args)
		{
			Try(nameof(OnStart),
			    () =>
			    {
				    HasBeenStarted = true;

				    StartedGrpcServer<T> started = _serverStart(args);

				    _server = started.Server;

				    StartHealthChecking(started.ServiceHealth);
			    });
		}

		protected override void OnStop()
		{
			Try(nameof(OnStop),
			    () =>
			    {
				    if (_server != null)
				    {
					    GrpcServerUtils.GracefullyStop(_server);
				    }
			    });
		}

		private void _timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (_health == null)
			{
				return;
			}

			if (_health.IsAnyServiceUnhealthy())
			{
				_health = null;

				_msg.Warn("Shutting down due to unhealthy service...");

				GrpcServerUtils.GracefullyStop(_server);

				// This allows the auto-restart to kick in:
				Environment.Exit(-1);
			}
		}

		private static void Try([CanBeNull] string methodName,
		                        [NotNull] Action procedure)
		{
			try
			{
				if (! string.IsNullOrEmpty(methodName))
				{
					_msg.Debug(methodName);
				}

				procedure();
			}
			catch (Exception e)
			{
				_msg.Error(e.Message, e);
			}
		}
	}
}
