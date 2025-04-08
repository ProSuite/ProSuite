using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Datasource;

public class WorkListGeometryService
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly CancellationTokenSource _cancellationTokenSource = new();

	public void Start(IWorkList workList)
	{
		CancellationToken token = _cancellationTokenSource.Token;

		int count = 3;

		try
		{
			if (workList is not SelectionWorkList selectionWorkList)
			{
				return;
			}

			var threads = new Thread[count];

			for (int index = 0; index < count; index++)
			{
				var name = $"swl geometry service {index}";
				threads[index] = CreateThread(selectionWorkList, name, token);
			}

			foreach (Thread thread in threads)
			{
				thread.Start();
			}
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	private static Thread CreateThread(SelectionWorkList workList, string name,
	                                   CancellationToken token)
	{
		//var thread = new Thread(() => BackgroundAction(workList, token));
		var thread = new Thread(() => { });

		thread.TrySetApartmentState(ApartmentState.STA);
		thread.Name = name;
		thread.IsBackground = true;
		return thread;
	}

	//private static void BackgroundAction(SelectionWorkList workList, CancellationToken token)
	//{
	//	int id = Environment.CurrentManagedThreadId;

	//	try
	//	{
	//		Stopwatch watch = _msg.DebugStartTiming("Start thread {0}", id);

	//		int total = 0;
	//		if (workList.TryGetItems(id, out List<IWorkItem> items))
	//		{
	//			total = items.Count;
	//			for (int index = 0; index < total; index++)
	//			{
	//				IWorkItem item = items[index];

	//				if (token.IsCancellationRequested)
	//				{
	//					_msg.Debug(
	//						$"Thread {id} cancellation after {index} of {total} items");

	//					// without catch block: this kills the process!
	//					token.ThrowIfCancellationRequested();
	//				}

	//				//// without catch block: this kills the process!
	//				//if (index == total - 3)
	//				//{
	//				//	throw new OperationCanceledException("bar");
	//				//}

	//				workList.Repository.RefreshGeometry(item);
	//			}
	//		}

	//		_msg.DebugStopTiming(watch, $"Thread {id}: {total} item geometries refreshed");

	//		// The thread terminates once its work is done.
	//	}
	//	catch (OperationCanceledException oce)
	//	{
	//		_msg.Debug("Cancel service", oce);
	//	}
	//	catch (Exception ex)
	//	{
	//		Gateway.LogError(ex, _msg);
	//	}
	//}
}
