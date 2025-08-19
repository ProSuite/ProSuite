using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.UI.MicroserverState
{
	public class ServerStateViewModel
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public ServerStateViewModel()
		{
			// For the designer
#if DEBUG
			var localClient = new MockClient("Localhost");
			var remoteClient = new MockClient("CRASSUS", 5152);

			ServerStates.Add(new ServerState(localClient)
			                 {
				                 Text = "Healthy",
				                 PingLatency = 23,
				                 ServiceState = ServiceState.Serving
			                 });

			ServerStates.Add(new ServerState(remoteClient)
			                 {
				                 Text = "Unavailable",
				                 ServiceState = ServiceState.Starting
			                 });
#endif
		}

		public ServerStateViewModel([NotNull] IMicroserviceClient serviceClient)
		{
			var serverState = new ServerState(serviceClient);
			CurrentServerState = serverState;

			ServerStates.Add(serverState);
		}

		public ServerStateViewModel([NotNull] IEnumerable<IMicroserviceClient> serviceClients)
		{
			_msg.Debug("Adding new service states...");

			ServerStates.AddRange(serviceClients.Select(c => new ServerState(c)));
		}

		public void StartAutoEvaluation()
		{
			foreach (ServerState serverState in ServerStates)
			{
				serverState.StartAutoEvaluation();
			}
		}

		public void StopAutoEvaluation()
		{
			foreach (ServerState serverState in ServerStates)
			{
				serverState.StopAutoEvaluation();
			}
		}

		private bool _evaluating;

		public async Task<bool?> EvaluateServices()
		{
			if (_evaluating)
			{
				return null;
			}

			try
			{
				_evaluating = true;

				bool result = true;
				foreach (ServerState serverState in ServerStates)
				{
					bool? serviceRunning = await serverState.Evaluate();

					result &= serviceRunning == true;
				}

				LastEvaluation = DateTime.Now;

				return result;
			}
			catch (Exception e)
			{
				_msg.Debug("Error evaluating service fitness", e);
				return false;
			}
			finally
			{
				_evaluating = false;
			}
		}

		public DateTime LastEvaluation { get; private set; } = DateTime.MinValue;

		public List<ServerState> ServerStates { get; set; } = new List<ServerState>();

		public ServerState CurrentServerState { get; set; }

		public bool HasAvailableService => ServerStates.Any(s => s.IsServing);

		public bool AllServicesAvailable => ServerStates.All(s => s.IsServing);

		public int AvailableServiceCount => ServerStates.Count(s => s.IsServing);

		/// <summary>
		/// The action that closes the host window.
		/// </summary>
		public Action CloseAction { get; set; }

		private class MockClient : IMicroserviceClient
		{
			public MockClient(string hostName, int port = 5151)
			{
				HostName = hostName;
				Port = port;
			}

			#region Implementation of IMicroserviceClient

			public string HostName { get; set; }
			public int Port { get; set; }
			public bool UseTls { get; set; }

			public string ServiceName => nameof(QualityVerificationGrpc);
			public string ServiceDisplayName => "Quality Verification Service";

			public bool ChannelIsLoadBalancer { get; set; }
			public bool CanFailOver { get; set; }
			public bool ProcessStarted { get; set; }

			public void Disconnect()
			{
				throw new NotImplementedException();
			}

			public bool CanAcceptCalls(bool allowFailOver = false, bool logOnlyIfUnhealthy = false)
			{
				return true;
			}

			public Task<bool> CanAcceptCallsAsync(bool allowFailOver = false)
			{
				return Task.FromResult(true);
			}

			public Task<bool> AllowStartingLocalServerAsync(
				string executable, string extraArguments = null, string command = null)
			{
				return Task.FromResult(false);
			}

			public bool AllowStartingLocalServer(string executable, string extraArguments = null, string command = null)
			{
				return false;
			}

			public bool TryRestart()
			{
				throw new NotImplementedException();
			}

			public string GetAddress()
			{
				string scheme = UseTls ? "https" : "http";

				return $"{scheme}://{HostName}:{Port}";
			}

			public Task<int> GetWorkerServiceCountAsync()
			{
				return Task.FromResult(12);
			}

			#endregion
		}
	}
}
