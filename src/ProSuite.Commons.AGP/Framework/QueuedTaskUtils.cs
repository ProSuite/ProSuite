using System;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Framework
{
	/// <summary>
	/// These utils help doing "the right thing" regardless of
	/// the type (or absence) of progressor being passed.
	/// There is nothing wrong calling QueuedTask.Run() directly!
	/// </summary>
	/// <remarks>As of Pro 3.x, there are 12 QTR overloads:
	/// 4 callback types (action, func, task, task with value) times
	/// 3 progressor types: none, Progressor, CancelableProgressor</remarks>
	public static class QueuedTaskUtils
	{
		public static Task Run(
			[NotNull] Action action, Progressor progressor = null,
			TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (progressor is null)
				return QueuedTask.Run(action, options);
			if (progressor is CancelableProgressor cancelableProgressor)
				return QueuedTask.Run(action, cancelableProgressor, options);
			return QueuedTask.Run(action, progressor, options);
		}

		public static Task Run(
			[NotNull] Func<Task> action, Progressor progressor = null,
			TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (progressor is null)
				return QueuedTask.Run(action, options);
			if (progressor is CancelableProgressor cancelableProgressor)
				return QueuedTask.Run(action, cancelableProgressor, options);
			return QueuedTask.Run(action, progressor, options);
		}

		public static Task<T> Run<T>(
			[NotNull] Func<T> function, Progressor progressor = null,
			TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (progressor is null)
				return QueuedTask.Run(function, options);
			if (progressor is CancelableProgressor cancelableProgressor)
				return QueuedTask.Run(function, cancelableProgressor, options);
			return QueuedTask.Run(function, progressor, options);
		}

		public static Task<T> Run<T>(
			[NotNull] Func<Task<T>> function, Progressor progressor = null,
			TaskCreationOptions options = TaskCreationOptions.None)
		{
			if (progressor is null)
				return QueuedTask.Run(function, options);
			if (progressor is CancelableProgressor cancelableProgressor)
				return QueuedTask.Run(function, cancelableProgressor, options);
			return QueuedTask.Run(function, progressor, options);
		}
	}
}
