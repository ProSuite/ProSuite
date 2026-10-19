using System;
using ArcGIS.Desktop.Editing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Gdb;

/// <summary>
/// Utility methods for the error handling of <see cref="EditOperation"/>s, shared by the
/// various edit operation wrappers (such as EditorTransaction or GdbTransaction).
/// </summary>
public static class EditOperationUtils
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	/// <summary>
	/// Wraps the specified edit operation call-back in an exception handler that aborts the
	/// edit operation and reports the exception to the caller. No exception must ever escape
	/// an edit operation call-back, otherwise the process crashes.
	/// </summary>
	/// <param name="procedure">The actual call-back to be executed.</param>
	/// <param name="reportException">Called with the exception in case the procedure has
	/// thrown. Typically used to remember the exception until the edit operation has been
	/// executed (see <see cref="HasError"/>).</param>
	/// <returns>The wrapped call-back to be handed to <see cref="EditOperation.Callback"/>.</returns>
	[NotNull]
	public static Action<EditOperation.IEditContext> WrapCallback(
		[NotNull] Action<EditOperation.IEditContext> procedure,
		[NotNull] Action<Exception> reportException)
	{
		void WrappedAction(EditOperation.IEditContext context)
		{
			try
			{
				procedure(context);
			}
			catch (Exception e)
			{
				// NOTE: No exception should be thrown here otherwise the process will crash
				_msg.Debug("Error in edit operation", e);

				// The Exception is internal, therefore we cannot use typeof
				if (e.GetType().Name == "AbortEditException")
				{
					// Or probably if it's the wrong SR?
					_msg.Debug("The edit operation was aborted. This could happen for " +
					           "example if the coordinates are out of bounds.");
				}

				reportException(e);

				context.Abort(e.Message);
			}
		}

		return WrappedAction;
	}

	/// <summary>
	/// Determines whether the edit operation has failed and, if so, provides the exception
	/// that describes the failure. This avoids failing silently in case the operation was
	/// aborted (in which case <see cref="EditOperation.Execute"/> just returns false).
	/// </summary>
	/// <param name="editOperation">The executed edit operation.</param>
	/// <param name="executeResult">The result of the edit operation's execution.</param>
	/// <param name="callbackException">The exception thrown by the call-back, if any
	/// (see <see cref="WrapCallback"/>).</param>
	/// <param name="exception">The resulting exception, or null if the operation succeeded.</param>
	/// <returns><c>true</c> if the operation has failed, <c>false</c> otherwise.</returns>
	public static bool HasError([NotNull] EditOperation editOperation,
	                            bool executeResult,
	                            [CanBeNull] Exception callbackException,
	                            [CanBeNull] out Exception exception)
	{
		exception = null;

		if (executeResult)
		{
			return false;
		}

		_msg.Debug("The edit operation failed.");

		if (callbackException != null)
		{
			_msg.Debug("The exception from the call-back is: ", callbackException);

			exception = new AggregateException(
				$"Edit operation failed: {callbackException.Message}", callbackException);
		}
		else if (editOperation.ErrorMessage != null)
		{
			_msg.DebugFormat("The message from the operation execution is: {0}",
			                 editOperation.ErrorMessage);

			exception = new AggregateException(
				$"Edit operation failed: {editOperation.ErrorMessage}");
		}
		else
		{
			// Neither the call-back nor the operation itself provided any detail. This can
			// happen for example if the operation was cancelled by the user.
			exception = new AggregateException(
				$"Edit operation failed: {editOperation.Name}");
		}

		return true;
	}
}
