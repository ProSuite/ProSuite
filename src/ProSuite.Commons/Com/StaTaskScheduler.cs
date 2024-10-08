using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Com
{
	//
	// StaTaskScheduler is a copy from ParallelExtensionsExtras (MICROSOFT LIMITED PUBLIC LICENSE version 1.1)
	// https://code.msdn.microsoft.com/ParExtSamples
	// https://blogs.msdn.microsoft.com/pfxteam/2010/04/04/a-tour-of-parallelextensionsextras/
	// This class might be included in future .NET frameworks.
	//

	//--------------------------------------------------------------------------
	// 
	//  Copyright (c) Microsoft Corporation.  All rights reserved. 
	// 
	//  File: StaTaskScheduler.cs
	//
	//--------------------------------------------------------------------------

	/// <summary>Provides a scheduler that uses STA threads.</summary>
	public sealed class StaTaskScheduler : TaskScheduler, IDisposable
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>Stores the queued tasks to be executed by our pool of STA threads.</summary>
		private BlockingCollection<Task> _tasks;

		/// <summary>The STA threads used by the scheduler.</summary>
		private readonly List<Thread> _threads;

		/// <summary>Initializes a new instance of the StaTaskScheduler class with the specified concurrency level.</summary>
		/// <param name="numberOfThreads">The number of threads that should be created and used by this scheduler.</param>
		public StaTaskScheduler(int numberOfThreads)
		{
			// Validate arguments
			if (numberOfThreads < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(numberOfThreads));
			}

			// Initialize the tasks collection
			_tasks = new BlockingCollection<Task>();

			// Create the threads to be used by this scheduler
			_threads = Enumerable.Range(0, numberOfThreads).Select(
				i =>
				{
					var thread =
						new Thread(() =>
						{
							// Continually get the next task and try to execute it.
							// This will continue until the scheduler is disposed and no more tasks remain.
							foreach (
								Task t in _tasks
									.GetConsumingEnumerable())
							{
								TryExecuteTask(t);
							}
						});
					thread.IsBackground = true;
					thread.SetApartmentState(
						ApartmentState.STA);
					return thread;
				}).ToList();

			// Start all of the threads
			_threads.ForEach(t => t.Start());
		}

		/// <summary>Queues a Task to be executed by this scheduler.</summary>
		/// <param name="task">The task to be executed.</param>
		protected override void QueueTask(Task task)
		{
			// Push it into the blocking collection of tasks
			_msg.VerboseDebug(() => $"Task queued: {task} <id> {task.Id}");

			_tasks.Add(task);
		}

		/// <summary>Provides a list of the scheduled tasks for the debugger to consume.</summary>
		/// <returns>An enumerable of all tasks currently scheduled.</returns>
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			// Serialize the contents of the blocking collection of tasks for the debugger
			return _tasks.ToArray();
		}

		/// <summary>Determines whether a Task may be inlined.</summary>
		/// <param name="task">The task to be executed.</param>
		/// <param name="taskWasPreviouslyQueued">Whether the task was previously queued.</param>
		/// <returns>true if the task was successfully inlined; otherwise, false.</returns>
		protected override bool TryExecuteTaskInline(Task task,
		                                             bool taskWasPreviouslyQueued)
		{
			// Try to inline if the current thread is STA
			return
				Thread.CurrentThread.GetApartmentState() == ApartmentState.STA &&
				TryExecuteTask(task);
		}

		/// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
		public override int MaximumConcurrencyLevel => _threads.Count;

		/// <summary>
		/// Cleans up the scheduler by indicating that no more tasks will be queued.
		/// This method blocks until all threads successfully shutdown.
		/// </summary>
		public void Dispose()
		{
			if (_tasks != null)
			{
				// Indicate that no new tasks will be coming in
				_tasks.CompleteAdding();

				// Wait for all threads to finish processing tasks
				foreach (Thread thread in _threads)
				{
					thread.Join();
				}

				// Cleanup
				_tasks.Dispose();
				_tasks = null;
			}
		}
	}
}
