using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client;
using ProSuite.Microservices.Client.GrpcNet;

namespace ProSuite.UI.MicroserverState
{
	public class ServerStateViewModel
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public ServerStateViewModel()
		{
			// For the designer (causes dependency on Microservices.Client.GrpcNet)
			var localClient = new GrpcNetQualityVerificationServiceClient("Localhost");
			var remoteClient = new GrpcNetQualityVerificationServiceClient("CRASSUS", 5152);

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
	}
}
