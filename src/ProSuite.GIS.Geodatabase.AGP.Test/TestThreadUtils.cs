using System;
using System.Threading;

namespace ProSuite.GIS.Geodatabase.AGP.Test;

internal static class TestThreadUtils
{
	public static Exception RunOnOtherThread(Action action)
	{
		Exception result = null;

		var thread = new Thread(() =>
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				result = e;
			}
		});

		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();

		return result;
	}
}
