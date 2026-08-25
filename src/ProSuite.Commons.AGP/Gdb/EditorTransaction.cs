using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Gdb;

public class EditorTransaction
{
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
		return EditOperationUtils.HasError(_editOperation, executeResult, _exception,
		                                   out exception);
	}

	private Action<EditOperation.IEditContext> GetWrappedAction(
		[NotNull] Action<EditOperation.IEditContext> procedure)
	{
		return EditOperationUtils.WrapCallback(procedure, e => _exception = e);
	}
}
