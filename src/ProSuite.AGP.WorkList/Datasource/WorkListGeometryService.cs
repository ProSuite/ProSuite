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

	private readonly BlockingCollection<KeyValuePair<string, QueryFilter>> _queue = new();

	[CanBeNull] private CancellationTokenSource _cancellationTokenSource;
	[CanBeNull] private Thread _thread;

	public bool Start()
	{
		try
		{
			_msg.Debug("Start service.");

			_cancellationTokenSource = new CancellationTokenSource();

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

			_queue.CompleteAdding();

			_thread?.Join();
			_thread = null;
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
				_msg.Debug("Service has not been started.");
				return;
			}

			Assert.True(_thread.IsAlive, "Thread is dead. Maybe an exception occurred.");

			if (_queue.IsAddingCompleted)
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
		try
		{
			foreach (var request in _queue.GetConsumingEnumerable(token))
			{
				string workListName = request.Key;
				QueryFilter filter = request.Value;

				IWorkList workList = WorkListRegistry.Instance.Get(workListName);

				if (workList == null)
				{
					_msg.Debug("Cannot get work list.");
					return;
				}

				workList.UpdateItemGeometries(filter);
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
