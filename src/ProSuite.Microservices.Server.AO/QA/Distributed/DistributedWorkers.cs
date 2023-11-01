using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.Microservices.Server.AO.QA.Distributed
{
	/// <summary>
	/// Manages the list of distributed clients and the currently active tasks / sub-verifications
	/// being processed in parallel.
	/// </summary>
	public class DistributedWorkers
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IList<QualityVerificationServiceClient> _configuredClients;
		private readonly int _desiredParallelCount;
		private readonly Predicate<IQualityVerificationClient> _clientPredicate;

		private readonly List<IQualityVerificationClient> _workerClients =
			new List<IQualityVerificationClient>();

		// This should be a concurrent hashset. Work-around: Use concurrent dictionary.
		private readonly IDictionary<IQualityVerificationClient, IQualityVerificationClient>
			_workingClients =
				new ConcurrentDictionary<IQualityVerificationClient, IQualityVerificationClient>();

		private readonly IDictionary<SubVerification, IQualityVerificationClient>
			_subveriClientsDict =
				new ConcurrentDictionary<SubVerification, IQualityVerificationClient>();

		private readonly IDictionary<Task<bool>, SubVerification> _tasks =
			new ConcurrentDictionary<Task<bool>, SubVerification>();

		public DistributedWorkers(
			[NotNull] IList<QualityVerificationServiceClient> configuredClients,
			int desiredParallelCount,
			[CanBeNull] Predicate<IQualityVerificationClient> clientPredicate)
		{
			_configuredClients = configuredClients;
			_desiredParallelCount = desiredParallelCount;
			_clientPredicate = clientPredicate;

			UpdateWorkerClients();
		}

		public int MaxParallelCount => _desiredParallelCount;

		public int ActiveTaskCount => _tasks.Count;

		public IReadOnlyCollection<SubVerification> ActiveSubVerifications =>
			_tasks.Values.ToList();

		/// <summary>
		/// Gets an available working client. If all clients are busy, null is returned.
		/// If all clients are unhealthy, an exception is thrown.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		[CanBeNull]
		public IQualityVerificationClient GetWorkerClient()
		{
			// Re-fill the list of clients if necessary:
			UpdateWorkerClients();

			if (_workerClients.Count == 0)
			{
				throw new InvalidOperationException("All workers are exhausted.");
			}

			if (_workerClients.Count == 1)
			{
				// Single-worker configurations allow worker-side multi-threading. Always return
				// the worker -> all verifications are sent and the throttling happens in the
				// worker.
				return _workerClients[0];
			}

			foreach (IQualityVerificationClient client in _workerClients)
			{
				if (! _workingClients.ContainsKey(client))
				{
					//if (client.CanAcceptCalls(allowFailOver: false))
					return client;
				}
			}

			return null;
		}

		internal Task<bool> IniTask([NotNull] SubVerification subVerification,
		                            [NotNull] IQualityVerificationClient verificationClient,
		                            [NotNull] Func<Task<bool>> verifyFunc)
		{
			// Check if there is a free client (and allow failing over if necessary):
			if (! verificationClient.CanAcceptCalls(true))
			{
				// TODO: Do something else? Use a different worker?
				return Task.FromResult(false);
			}

			_subveriClientsDict.Add(subVerification, verificationClient);
			_workingClients.Add(verificationClient, verificationClient);

			Task<bool> newTask = verifyFunc();

			// Process the messages even though the foreground thread is blocking/busy processing results
			newTask.ConfigureAwait(false);

			if (_tasks.ContainsKey(newTask))
			{
				_msg.WarnFormat("New Task already exists in dictionary!");
				LogTask(_tasks, newTask, subVerification);
			}

			_tasks.Add(newTask, subVerification);

			return newTask;
		}

		internal bool TryTakeCompleted(
			out Task<bool> completedTask,
			out SubVerification subVerification,
			out IQualityVerificationClient finishedClient)
		{
			KeyValuePair<Task<bool>, SubVerification> keyValuePair =
				_tasks.FirstOrDefault(kvp => kvp.Key.IsCompleted);

			// NOTE: 'Default' is an empty keyValuePair struct
			completedTask = keyValuePair.Key;
			subVerification = keyValuePair.Value;

			if (completedTask == null)
			{
				finishedClient = null;
				return false;
			}

			finishedClient = _subveriClientsDict[subVerification];
			_workingClients.Remove(finishedClient);

			return _tasks.Remove(completedTask);
		}

		public IQualityVerificationClient GetWorkerClient(SubVerification subVerification)
		{
			_subveriClientsDict.TryGetValue(subVerification,
			                                out IQualityVerificationClient workerClient);
			return workerClient;
		}

		private void UpdateWorkerClients()
		{
			Stopwatch watch = Stopwatch.StartNew();

			int removedWorkers = RemoveUnhealthyWorkers(_workerClients);

			_msg.DebugStopTiming(watch, "Removed {0} unhealthy workers from worker client list.",
			                     removedWorkers);

			int desiredNewWorkerCount = _desiredParallelCount - _workerClients.Count;

			if (desiredNewWorkerCount <= 0)
			{
				return;
			}

			watch.Restart();

			int extraWorkers = _clientPredicate == null ? 0 : 2;
			desiredNewWorkerCount += extraWorkers;
			foreach (IQualityVerificationClient workerClient in GetNewWorkerClients(
				         desiredNewWorkerCount))
			{
				if (_workerClients.Count >= _desiredParallelCount)
				{
					// it's enough
					break;
				}

				string newAddress = workerClient.GetAddress();
				if (_workerClients.Any(c => c.GetAddress()
				                             .Equals(newAddress,
				                                     StringComparison.CurrentCultureIgnoreCase)))
				{
					// it's already in the list
					continue;
				}

				if (_clientPredicate == null || _clientPredicate(workerClient))
				{
					_msg.DebugFormat("Adding additional worker client {0}",
					                 workerClient.GetAddress());
					_workerClients.Add(workerClient);
				}
			}

			_msg.DebugStopTiming(watch, "Updated worker client list. It now contains {0} clients.",
			                     _workerClients.Count);
		}

		private static int RemoveUnhealthyWorkers(
			ICollection<IQualityVerificationClient> workerClients)
		{
			var unhealthyWorkers = new List<IQualityVerificationClient>();

			foreach (IQualityVerificationClient client in workerClients)
			{
				if (! client.CanAcceptCalls(allowFailOver: false, logOnlyIfUnhealthy: true))
				{
					unhealthyWorkers.Add(client);
				}
			}

			int removeCount = 0;
			foreach (IQualityVerificationClient unhealthyWorker in unhealthyWorkers)
			{
				if (workerClients.Remove(unhealthyWorker))
				{
					removeCount++;
				}
			}

			return removeCount;
		}

		/// <summary>
		/// If the first client channel configuration of the
		/// client-side service configuration file is a load balancer address,
		/// <see cref="desiredNewWorkerCount"/> service addresses will be requested from the load
		/// balancer. Otherwise all the client channels configured in the list of configured
		/// clients will be returned (regardless of maxParallel).
		/// </summary>
		/// <param name="desiredNewWorkerCount"></param>
		/// <returns></returns>
		private IEnumerable<IQualityVerificationClient> GetNewWorkerClients(
			int desiredNewWorkerCount)
		{
			QualityVerificationServiceClient client =
				_configuredClients.FirstOrDefault(c => c.CanAcceptCalls());

			if (client == null)
			{
				yield break;
			}

			if (client.ChannelIsLoadBalancer)
			{
				foreach (QualityVerificationServiceClient workerClient in client.GetWorkerClients(
					         desiredNewWorkerCount))
				{
					yield return workerClient;
				}
			}
			else
			{
				foreach (var workerClient in _configuredClients)
				{
					yield return workerClient;
				}
			}
		}

		private static void LogTask(IDictionary<Task<bool>, SubVerification> tasks,
		                            Task<bool> newTask,
		                            SubVerification newSubVerification)
		{
			LogTask(newTask, newSubVerification, "New");

			if (tasks.TryGetValue(newTask, out SubVerification existing))
			{
				foreach (KeyValuePair<Task<bool>, SubVerification> pair in tasks)
				{
					if (pair.Value == existing)
					{
						LogTask(pair.Key, pair.Value, "Equal");
					}
				}
			}
			else
			{
				foreach (KeyValuePair<Task<bool>, SubVerification> pair in tasks)
				{
					LogTask(pair.Key, pair.Value, "Existing");
				}
			}
		}

		private static void LogTask(Task task, SubVerification subVerification, string prefix)
		{
			_msg.Warn(
				$"{prefix} Task {task}; Task Hashcode: {task.GetHashCode()}; Subverification {subVerification}");
		}
	}
}
