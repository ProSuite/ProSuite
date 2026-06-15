using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Gdb;

public class EditorTransaction
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly EditOperation _editOperation;

	private Exception _exception;

	public EditorTransaction([NotNull] EditOperation editOperation)
	{
		_editOperation = editOperation;

		// Avoid the message box (only appears when calling the async variant)
		_editOperation.ShowModalMessageAfterFailure = false;

		// Default to false
		_editOperation.ShowProgressor = false;
	}

	/// <summary>
	/// Whether the progressor window should be shown. Default: False.
	/// </summary>
	public bool ShowProgressorWindow
	{
		get => _editOperation.ShowProgressor;
		set => _editOperation.ShowProgressor = value;
	}

	public bool Execute([NotNull] Action<EditOperation.IEditContext> action,
	                    [NotNull] string description,
	                    [NotNull] Dataset dataset)
	{
		return Execute(action, description, new[] { dataset });
	}

	public bool Execute([NotNull, InstantHandle] Action<EditOperation.IEditContext> action,
	                    [NotNull] string description,
	                    [NotNull] IEnumerable<Dataset> datasets)
	{
		_editOperation.Callback(GetWrappedAction(action), datasets);

		return Execute(description);
	}

	public async Task<bool> ExecuteAsync([NotNull] Action<EditOperation.IEditContext> action,
	                                     [NotNull] string description,
	                                     [NotNull] Dataset dataset)
	{
		return await ExecuteAsync(action, description, new[] { dataset });
	}

	public async Task<bool> ExecuteAsync([NotNull] Action<EditOperation.IEditContext> action,
	                                     [NotNull] string description,
	                                     [NotNull] IEnumerable<Dataset> datasets)
	{
		_editOperation.Callback(GetWrappedAction(action), datasets);

		return await ExecuteAsync(description);
	}

	public bool Execute(string description)
	{
		_editOperation.Name = description;

		bool result = _editOperation.Execute();

		if (HasError(result, out Exception exception))
		{
			throw exception;
		}

		return result;
	}

	private async Task<bool> ExecuteAsync([NotNull] string description)
	{
		_editOperation.Name = description;

		bool result = await _editOperation.ExecuteAsync();

		if (HasError(result, out Exception exception))
		{
			throw exception;
		}

		return result;
	}

	private bool HasError(bool executeResult, out Exception exception)
	{
		exception = null;

		if (executeResult)
		{
			return false;
		}

		_msg.Debug("The edit operation failed.");

		if (_exception != null)
		{
			_msg.Debug("The exception from the call-back is: ", _exception);

			exception = new AggregateException(
				$"Edit operation failed: {_exception.Message}", _exception);
		}
		else if (_editOperation.ErrorMessage != null)
		{
			_msg.DebugFormat("The message from the operation execution is: {0}",
			                 _editOperation.ErrorMessage);
			exception = new AggregateException(
				$"Edit operation failed: {_editOperation.ErrorMessage}");
		}

		return true;
	}

	private Action<EditOperation.IEditContext> GetWrappedAction(
		[NotNull] Action<EditOperation.IEditContext> procedure)
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

				_exception = e;

				context.Abort(e.Message);
			}
		}

		return WrappedAction;
	}
}
