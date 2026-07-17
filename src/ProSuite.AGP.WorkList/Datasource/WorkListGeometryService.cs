using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource;

public class WorkListGeometryService
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[CanBeNull] private BlockingCollection<KeyValuePair<string, QueryFilter>> _queue;

	[CanBeNull] private CancellationTokenSource _cancellationTokenSource;
	[CanBeNull] private volatile Thread _thread;

	public bool Start()
	{
		try
		{
			if (_thread != null && _thread.IsAlive)
			{
				_msg.Debug("Service is already running.");
				return true;
			}

			_msg.Debug("Start service.");

			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = new CancellationTokenSource();

			_queue = new BlockingCollection<KeyValuePair<string, QueryFilter>>();

			CancellationToken token = _cancellationTokenSource.Token;

			_thread = new Thread(() => BackgroundAction(token));

			_thread.TrySetApartmentState(ApartmentState.STA);
			_thread.Name = $"{_thread.ManagedThreadId} {nameof(WorkListGeometryService)}";
			_thread.IsBackground = true;
			_thread.Start();

			return _thread != null;
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}

		return false;
	}

	public void Stop()
	{
		try
		{
			if (_thread == null)
			{
				_msg.Debug("Service has not been started.");
				return;
			}

			_msg.Debug("Stop service.");

			_cancellationTokenSource?.Cancel();

			_queue?.CompleteAdding();

			_thread?.Join();
			_thread = null;

			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = null;
		}
		catch (AggregateException ae)
		{
			// Handle the expected cancellation exception.
			ae.Handle(e => e is OperationCanceledException);
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
	}

	public void UpdateItemGeometries(string workListName, QueryFilter filter)
	{
		try
		{
			if (_thread == null)
			{
				_msg.Debug("Service has not been started. Starting now...");
				if (! Start())
				{
					_msg.Debug("Could not start service. No geometries will be cached.");
					return;
				}
			}

			Thread thread = _thread;
			if (thread == null || ! thread.IsAlive)
			{
				_msg.Debug("Thread is dead. Maybe an exception occurred.");
				return;
			}

			if (_queue == null || _queue.IsAddingCompleted)
			{
				return;
			}

			_queue.Add(KeyValuePair.Create(workListName, filter));
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
	}

	private void BackgroundAction(CancellationToken token)
	{
		BlockingCollection<KeyValuePair<string, QueryFilter>> queue = Assert.NotNull(_queue);
		try
		{
			foreach (var request in queue.GetConsumingEnumerable(token))
			{
				string workListName = request.Key;
				QueryFilter filter = request.Value;

				IWorkList workList = WorkListRegistry.Instance.Get(workListName);

				if (workList == null)
				{
					_msg.Debug("Cannot get work list.");
					continue;
				}

				workList.UpdateExistingItemGeometries(filter);
			}
		}
		catch (OperationCanceledException ex)
		{
			_msg.Debug("Cancel service", ex);
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
	}
}
