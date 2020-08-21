using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Framework
{
	public static class QueuedTaskUtils
	{
		public static Task<T> Run<T>([NotNull] Func<Task<T>> function,
		                             [CanBeNull] CancelableProgressor progressor = null,
		                             TaskCreationOptions creationOptions = TaskCreationOptions.None)
		{
			// NOTE on the standard QueuedTask.Run: if the progressor is null, there's an argument exception.
			Task<T> result = progressor == null
				                 ? QueuedTask.Run(function, creationOptions)
				                 : QueuedTask.Run(function, progressor, creationOptions);

			return result;
		}

		public static Task<T> Run<T>(
			[NotNull] Func<T> function,
			[CanBeNull] Progressor progressor = null,
			TaskCreationOptions creationOptions = TaskCreationOptions.None)
		{
			// NOTE on the standard QueuedTask.Run: if the progressor is null, there's an argument exception.
			Task<T> result = progressor == null
				                 ? QueuedTask.Run(function, creationOptions)
				                 : QueuedTask.Run(function, progressor, creationOptions);

			return result;
		}
	}
}
