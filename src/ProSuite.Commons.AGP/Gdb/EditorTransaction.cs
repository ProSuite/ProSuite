using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Gdb
{
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
		}

		public bool Execute([NotNull] Action<EditOperation.IEditContext> action,
		                    [NotNull] string description,
		                    [NotNull] Dataset dataset)
		{
			return Execute(action, description, new[] {dataset});
		}

		public bool Execute([NotNull] Action<EditOperation.IEditContext> action,
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
			return await ExecuteAsync(action, description, new[] {dataset});
		}

		public async Task<bool> ExecuteAsync([NotNull] Action<EditOperation.IEditContext> action,
		                                     [NotNull] string description,
		                                     [NotNull] IEnumerable<Dataset> datasets)
		{
			_editOperation.Callback(GetWrappedAction(action), datasets);

			return await ExecuteAsync(description);
		}

		private bool Execute(string description)
		{
			_editOperation.Name = description;

			bool result = _editOperation.Execute();

			if (! result && _exception != null)
			{
				throw _exception;
			}

			return result;
		}

		private async Task<bool> ExecuteAsync([NotNull] string description)
		{
			_editOperation.Name = description;

			bool result = await _editOperation.ExecuteAsync();

			if (! result && _exception != null)
			{
				throw new AggregateException("Edit operation failed", _exception);
			}

			return result;
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

					_exception = e;

					context.Abort(e.Message);
				}
			}

			return WrappedAction;
		}
	}
}
