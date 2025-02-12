using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Framework;
using System.Collections.Generic;
using System;
using System.Threading;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource;

public class WorkListService
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly CancellationTokenSource _cancellationTokenSource = new();

	public bool Start(IWorkList workList)
	{
		CancellationToken token = _cancellationTokenSource.Token;

		int count = 3;

		try
		{
			Thread[] threads = new Thread[count];
			for (int index = 0; index < count; index++)
			{
				var threadName = $"{nameof(WorkListService)} {index}";
				threads[index] = CreateThread(workList, threadName, token);
			}

			foreach (Thread thread in threads)
			{
				thread.Start();
			}
		}
		catch (OperationCanceledException)
		{
			_msg.Debug("Service cancelled.");
		}
		catch (AggregateException ae)
		{
			// Handle the expected cancellation exception.
			ae.Handle(e => e is OperationCanceledException);
			_msg.Debug("Service cancelled.");
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}

		return true;
	}

	private Thread CreateThread(IWorkList workList, string threadName, CancellationToken token)
	{
		var thread = new Thread(() => BackgroundAction(workList, token));

		thread.TrySetApartmentState(ApartmentState.STA);
		thread.Name = threadName;
		thread.IsBackground = true;
		return thread;
	}

	private void BackgroundAction(IWorkList workList, CancellationToken token)
	{
		int id = Environment.CurrentManagedThreadId;

		try
		{
			if (workList.TryGetItems(id, out List<IWorkItem> items))
			{
				foreach (IWorkItem item in items)
				{
					if (token.IsCancellationRequested)
					{
						token.ThrowIfCancellationRequested();
						_msg.Debug("Service cancellation requested.");
					}

					workList.Repository.RefreshGeometry(item);
				}
			}

			// The thread terminates once its work is done.
		}
		catch (OperationCanceledException)
		{
			_msg.Debug("Service cancellation requested.");
		}
		catch (AggregateException ae)
		{
			// Handle the expected cancellation exception.
			ae.Handle(e => e is OperationCanceledException);
			_msg.Debug("Service cancellation requested.");
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}
}
