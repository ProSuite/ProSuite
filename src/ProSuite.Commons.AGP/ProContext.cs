using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP
{
	public static class ProContext
	{
		/// <summary>
		/// Whether the current process is running in headless mode, i.e. outside the ArcGIS Pro application.
		/// </summary>
		public static bool IsRunningHeadless
		{
			get
			{
				bool canLoad = false;
				try
				{
					canLoad = CanLoadFrameworkAssembly();
				}
				catch (FileNotFoundException)
				{
					// Load failure
				}

				return ! canLoad;
			}
		}

		/// <summary>
		/// Execute the given action either as a QueuedTask (if running in ArcGIS Pro)
		/// or as a BackgroundTask if running headless and not already on an STA thread.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public static Task Run([NotNull] Action action,
		                       TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (IsRunningHeadless)
			{
				if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
				{
					action();
					return Task.CompletedTask;
				}

				return BackgroundTask.Run(action, BackgroundProgressor.None);
			}

			return QueuedTaskUtils.Run(action, null, options);
		}

		/// <summary>
		/// Execute the given function either as a QueuedTask (if running in ArcGIS Pro)
		/// or as a BackgroundTask if running headless and not already on an STA thread.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public static Task Run([NotNull] Func<Task> function,
		                       TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (IsRunningHeadless)
			{
				if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
				{
					return function();
				}

				return BackgroundTask.Run(function, BackgroundProgressor.None);
			}

			return QueuedTaskUtils.Run(function, null, options);
		}

		/// <summary>
		/// Test whether ArcGIS.Desktop.Framework can be loaded, i.e. we are running in the
		/// ArcGIS Pro application. The failure will occur at the call site!
		/// </summary>
		/// <returns></returns>
		private static bool CanLoadFrameworkAssembly()
		{
			try
			{
				// ReSharper disable once UnusedVariable
				Type type = typeof(QueuedTask);
			}
			catch (Exception e)
			{
				return false;
			}

			return true;
		}
	}
}
