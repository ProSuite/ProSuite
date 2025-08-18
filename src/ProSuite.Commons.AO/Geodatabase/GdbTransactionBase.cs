using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.Callbacks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase
{
	public abstract class GdbTransactionBase : IGdbTransaction
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private bool _aborted;
		[CanBeNull] private IWorkspaceEdit _workspaceEdit;

		protected GdbTransactionBase(bool reconcileRedefinedVersion)
		{
			ReconcileRedefinedVersion = reconcileRedefinedVersion;
		}

		#region IGdbTransaction Members

		public event EventHandler Aborted;

		public bool CanWrite(IWorkspace workspace)
		{
			return workspace is IWorkspaceEdit workspaceEdit &&
			       workspaceEdit.IsBeingEdited() && CanWriteInContext(workspace);
		}

		/// <summary>
		/// Executes a procedure in an edit operation
		/// </summary>
		/// <param name="workspace">The workspace in which the operation is executed</param>
		/// <param name="procedure">The operation.</param>
		/// <param name="description">The description for the edit operation.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> if it was aborted.</returns>
		public bool Execute(IWorkspace workspace, Action procedure, string description)
		{
			return Execute(workspace, delegate { procedure(); },
			               description, null);
		}

		/// <summary>
		/// Executes a procedure in an edit operation
		/// </summary>
		/// <param name="workspace">The workspace in which the operation is executed</param>
		/// <param name="procedure">The operation.</param>
		/// <param name="description">The description for the edit operation.</param>
		/// <param name="trackCancel">The cancel tracker, allowing the procedure to abort 
		/// the transaction (optional).</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> if it was aborted.</returns>
		public bool Execute(IWorkspace workspace,
		                    Action<ITrackCancel> procedure,
		                    string description,
		                    ITrackCancel trackCancel)
		{
			return Execute(workspace, procedure,
			               EditStateInfo.MustNotBeInOperation, description, trackCancel);
		}

		/// <summary>
		/// Executes a procedure in an edit operation
		/// </summary>
		/// <param name="workspace">The workspace in which the operation is executed</param>
		/// <param name="procedure">The operation.</param>
		/// <param name="state">The state.</param>
		/// <param name="description">The description for the edit operation.</param>
		/// <returns><c>true</c> if the operation succeeded, <c>false</c> if it was aborted.</returns>
		public bool Execute(IWorkspace workspace, Action procedure,
		                    EditStateInfo state, string description)
		{
			return Execute(workspace,
			               delegate { procedure(); },
			               state, description, null);
		}

		/// <summary>
		/// Executes a procedure in an edit operation
		/// </summary>
		/// <param name="workspace">The workspace in which the operation is executed</param>
		/// <param name="procedure">The operation.</param>
		/// <param name="state">The state.</param>
		/// <param name="description">The description for the edit operation.</param>
		/// <param name="trackCancel">The cancel tracker (optional).</param>
		/// <returns>
		/// 	<c>true</c> if the operation succeeded, <c>false</c> if it was aborted.
		/// </returns>
		public bool Execute(IWorkspace workspace, Action<ITrackCancel> procedure,
		                    EditStateInfo state, string description,
		                    ITrackCancel trackCancel)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNull(procedure, nameof(procedure));
			Assert.ArgumentNotNullOrEmpty(description, nameof(description));

			if (trackCancel != null)
			{
				Assert.True(trackCancel.Continue(), "Cancel tracker already cancelled");
			}

			using (EditWorkspace(workspace))
			{
				var editWs = (IWorkspaceEdit) workspace;
				var editWs2 = workspace as IWorkspaceEdit2; // not implemented for shapefiles

				bool isBeingEdited = editWs.IsBeingEdited();
				bool isInEditOperation = editWs2 != null && editWs2.IsInEditOperation;

				if ((state & EditStateInfo.MustNotBeEditing) != 0)
				{
					Assert.False(isBeingEdited, "Edit Session already open");
				}

				if ((state & EditStateInfo.MustBeEditing) != 0)
				{
					Assert.True(isBeingEdited, "Edit Session not open");
				}

				if ((state & EditStateInfo.MustNotBeInOperation) != 0)
				{
					if (isInEditOperation)
					{
						AbortEditOperation();
						_msg.Warn("Rolled back unexpected existing edit operation");
					}

					isInEditOperation = false;
				}

				if ((state & EditStateInfo.MustBeInOperation) != 0)
				{
					Assert.True(isInEditOperation, "Edit Session not in Operation");
				}

				_aborted = false;

				IWorkspaceEditEvents_Event workspaceEditEvents = null;

				try
				{
					#region check reset current state

					if (isInEditOperation)
					{
						if ((state & EditStateInfo.AbortExistingOperation) != 0)
						{
							Assert.True(
								(state & EditStateInfo.StopExistingOperation) == 0,
								"Cannot specify both " +
								EditStateInfo.AbortExistingOperation + " and " +
								EditStateInfo.StopExistingOperation);
							AbortEditOperation();
							isInEditOperation = false;
						}
						else if ((state & EditStateInfo.StopExistingOperation) != 0)
						{
							StopEditOperation(description);
							isInEditOperation = false;
						}
					}

					if (isBeingEdited)
					{
						if ((state & EditStateInfo.AbortExistingEditing) != 0)
						{
							Assert.True((state & EditStateInfo.StopExistingEditing) == 0,
							            "Cannot specify both " +
							            EditStateInfo.AbortExistingEditing + " and " +
							            EditStateInfo.StopExistingEditing);

							StopEditing(false);
							isBeingEdited = false;
						}
						else if ((state & EditStateInfo.StopExistingEditing) != 0)
						{
							StopEditing(true);
							isBeingEdited = false;
						}
					}

					#endregion

					if (! isBeingEdited)
					{
						StartEditing();
					}

					if (! isInEditOperation && (state & EditStateInfo.DontStartOperation) == 0)
					{
						StartEditOperation();
					}

					// this may fail (COM object that has been separated from its underlying RCW cannot be used)
					workspaceEditEvents = (IWorkspaceEditEvents_Event) workspace;
					workspaceEditEvents.OnAbortEditOperation += editEvents_OnAbortEditOperation;

					procedure(trackCancel);

					if (trackCancel != null && ! trackCancel.Continue())
					{
						_msg.WarnFormat("Operation cancelled: {0}", description);

						// cancel was called in procedure
						if (! _aborted)
						{
							// The operation is not yet aborted. Abort it now.
							AbortEditOperation();
						}
					}

					if (! _aborted && ! isInEditOperation &&
					    (state & EditStateInfo.KeepOperation) == 0 ||
					    (state & EditStateInfo.StopOperation) != 0)
					{
						// if the edit operation violates rule engine rules, the edit operation won't succeed. However
						// no exception is reported when calling StopEditOperation --> the OnAbort handler is mandatory
						// to detect this situation.
						StopEditOperation(description);
					}

					if (! isBeingEdited && (state & EditStateInfo.KeepEditing) == 0 ||
					    (state & EditStateInfo.StopEditing) != 0)
					{
						StopEditing(true);
					}

					return ! _aborted;
				}
				catch (Exception ex)
				{
					try // Clean up
					{
						string message =
							ex is COMException comEx
								? string.Format(
									"Error executing operation: {0} ({1}; Error Code: {2})",
									description, comEx.Message, comEx.ErrorCode)
								: string.Format("Error executing operation: {0} ({1})",
								                description, ex.Message);

						_msg.Debug(message);

						if (! isInEditOperation)
						{
							// if the error occurred in StopEditOperation(), then
							// that edit operation might already be aborted -> check
							if (editWs2 != null && editWs2.IsInEditOperation)
							{
								AbortEditOperation();
							}
						}

						if (! isBeingEdited)
						{
							StopEditing(false);
						}
					}
					catch (Exception e2)
					{
						// exception intentionally suppressed.
						_msg.Error("Error cleaning up after failed gdb function", e2);
					}

					throw;
				}
				finally
				{
					try
					{
						if (workspaceEditEvents != null)
						{
							workspaceEditEvents.OnAbortEditOperation -=
								editEvents_OnAbortEditOperation;
						}
					}
					catch (AccessViolationException e)
					{
						// exception intentionally suppressed.
						_msg.Warn("Error unregistering event handler", e);
					}
				}
			}
		}

		#endregion

		protected abstract bool CanWriteInContext([NotNull] IWorkspace workspace);

		/// <summary>
		/// Determines whether or not the edit session should be reconciled with its 
		/// SDE version that was edited at the same time in another process. In case
		/// there are no conflicts the edit session can be saved after the reconcile.
		/// </summary>
		[PublicAPI]
		protected bool ReconcileRedefinedVersion { get; set; }

		protected virtual void SetWorkspaceCore(IWorkspace workspace) { }

		protected virtual void StartEditOperation()
		{
			Assert.NotNull(_workspaceEdit);

			_workspaceEdit.StartEditOperation();
		}

		protected virtual void StopEditOperation(string description)
		{
			Assert.NotNull(_workspaceEdit);

			_workspaceEdit.StopEditOperation();
		}

		protected virtual void AbortEditOperation()
		{
			Assert.NotNull(_workspaceEdit);

			_workspaceEdit.AbortEditOperation();
		}

		protected virtual void StartEditing()
		{
			Assert.NotNull(_workspaceEdit);

			_workspaceEdit.StartEditing(false);
		}

		protected virtual void StopEditing(bool saveEdits)
		{
			Assert.NotNull(_workspaceEdit);

			try
			{
				_workspaceEdit.StopEditing(saveEdits);
			}
			catch (COMException ex)
			{
				_msg.Debug("Exception in stop editing.", ex);

				if (ReconcileRedefinedVersion &&
				    ex.ErrorCode == (int) fdoError.FDO_E_VERSION_REDEFINED)
				{
					if (_workspaceEdit is IVersion version)
					{
						if (! ((IVersionEdit) version).Reconcile(version.VersionName))
						{
							_msg.DebugFormat(
								"Reconciled edit session successfully without conflicts. Saving again.");
							_workspaceEdit.StopEditing(saveEdits);

							return;
						}

						_msg.DebugFormat(
							"Reconciling the edit session with the edit version resulted in conflicts.");
					}
					else
					{
						// you never know:
						_msg.DebugFormat("Unexpected exception for non-versioned workspace");
					}
				}

				// NOTE: Another relatively rare errors can occur here, but there is no remedy:
				// COMException (0x800415BA): 'Invalid state ID' (when another process has called RefreshVersion in between)

				throw;
			}
		}

		private void SetWorkspace(IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			IWorkspaceEdit oldWorkspaceEdit = _workspaceEdit;
			try
			{
				_workspaceEdit = (IWorkspaceEdit) workspace;

				SetWorkspaceCore(workspace);
			}
			catch
			{
				_workspaceEdit = oldWorkspaceEdit;
				throw;
			}
		}

		private void ClearWorkspace()
		{
			_workspaceEdit = null;
		}

		private void editEvents_OnAbortEditOperation()
		{
			_msg.Debug("GdbTransationBase.editEvents_OnAbortEditOperation() called");

			_aborted = true;

			OnAborted();
		}

		private void OnAborted()
		{
			Aborted?.Invoke(this, EventArgs.Empty);
		}

		private DisposableCallback EditWorkspace(IWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			SetWorkspace(workspace);

			return new DisposableCallback(ClearWorkspace);
		}
	}
}
