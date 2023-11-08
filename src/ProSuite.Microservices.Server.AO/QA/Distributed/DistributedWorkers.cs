using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ConcurrentCollections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client.QA;

namespace ProSuite.Microservices.Server.AO.QA.Distributed
{
	/// <summary>
	/// Manages the list of distributed clients used by all (!) distributed test runners.
	/// </summary>
	public class DistributedWorkers
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IList<QualityVerificationServiceClient> _configuredClients;
		private readonly int _desiredParallelCount;
		private readonly Predicate<IQualityVerificationClient> _clientPredicate;

		private readonly ConcurrentHashSet<IQualityVerificationClient> _workerClients =
			new ConcurrentHashSet<IQualityVerificationClient>();

		private readonly ConcurrentHashSet<IQualityVerificationClient>
			_workingClients =
				new ConcurrentHashSet<IQualityVerificationClient>();

		private readonly IDictionary<SubVerification, IQualityVerificationClient>
			_subveriClientsDict =
				new ConcurrentDictionary<SubVerification, IQualityVerificationClient>();

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
				return _workerClients.FirstOrDefault();
			}

			foreach (IQualityVerificationClient client in _workerClients)
			{
				if (! _workingClients.Contains(client))
				{
					return client;
				}
			}

			return null;
		}

		public bool HasFreeWorkers()
		{
			return _workerClients.Any(client => ! _workingClients.Contains(client));
		}

		/// <summary>
		/// Starts the specified <see cref="verifyFunc"/> on the next available client and returns
		/// the task. It returns null if no worker client is available.
		/// </summary>
		/// <param name="subVerifications"></param>
		/// <param name="verifyFunc"></param>
		/// <param name="started"></param>
		/// <returns></returns>
		[CanBeNull]
		internal Task StartNext(
			[NotNull] Stack<SubVerification> subVerifications,
			[NotNull] Func<SubVerification, IQualityVerificationClient, Task> verifyFunc,
			[CanBeNull] out SubVerification started)
		{
			IQualityVerificationClient client = GetWorkerClient();

			started = null;
			if (client == null)
			{
				return null;
			}

			// Check if there is a free client (and allow failing over if necessary):
			if (! client.CanAcceptCalls(true))
			{
				return null;
			}

			if (! _workingClients.Add(client))
			{
				// Race condition! Another call has already used (and added it to the _workingClients):
				return null;
			}

			started = subVerifications.Pop();

			_msg.Debug($"Popped sub-verification {subVerifications.Count} to be started " +
			           $"on {client.GetAddress()}");

			_subveriClientsDict.Add(started, client);

			Task newTask = verifyFunc(started, client);

			// The worker's messages can be processed in any thread.
			// This is not needed in console apps but included for clarity (and re-usability in
			// environments that contain a SyncronizationContext):
			newTask.ConfigureAwait(false);

			_msg.Info($"Popped sub-verification {subVerifications.Count} and started " +
			          $"on {client.GetAddress()}: {started}");

			return newTask;
		}

		internal bool TryTakeCompleted(
			IDictionary<Task, SubVerification> tasks,
			out Task completedTask,
			out SubVerification subVerification,
			out IQualityVerificationClient finishedClient)
		{
			KeyValuePair<Task, SubVerification> keyValuePair =
				tasks.FirstOrDefault(kvp => kvp.Key.IsCompleted);

			// NOTE: 'Default' is an empty keyValuePair struct
			completedTask = keyValuePair.Key;
			subVerification = keyValuePair.Value;

			if (completedTask == null)
			{
				finishedClient = null;
				return false;
			}

			finishedClient = _subveriClientsDict[subVerification];
			_workingClients.TryRemove(finishedClient);

			return tasks.Remove(completedTask);
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

		private static void LogTask(IDictionary<Task, SubVerification> tasks,
		                            Task newTask,
		                            SubVerification newSubVerification)
		{
			LogTask(newTask, newSubVerification, "New");

			if (tasks.TryGetValue(newTask, out SubVerification existing))
			{
				foreach (KeyValuePair<Task, SubVerification> pair in tasks)
				{
					if (pair.Value == existing)
					{
						LogTask(pair.Key, pair.Value, "Equal");
					}
				}
			}
			else
			{
				foreach (KeyValuePair<Task, SubVerification> pair in tasks)
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
