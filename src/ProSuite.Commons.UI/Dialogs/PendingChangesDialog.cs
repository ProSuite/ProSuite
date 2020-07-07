using System;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Dialogs
{
	public class PendingChangesDialog
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly string _messageTitle;
		private readonly IWin32Window _owner;
		private bool _aborting;
		private bool _canSave;
		private bool _hasPendingChanges;
		private bool _saving;

		public PendingChangesDialog([NotNull] IWin32Window owner,
		                            [NotNull] string messageTitle,
		                            [NotNull] Action writeChanges,
		                            [NotNull] Action discardChanges)
		{
			Assert.ArgumentNotNull(owner, nameof(owner));
			Assert.ArgumentNotNull(messageTitle, nameof(messageTitle));
			Assert.ArgumentNotNull(writeChanges, nameof(writeChanges));
			Assert.ArgumentNotNull(discardChanges, nameof(discardChanges));

			_owner = owner;
			_messageTitle = messageTitle;
			WriteChanges = writeChanges;
			DiscardChanges = discardChanges;
		}

		public bool IsSaving => _saving && ! _aborting;

		public bool AutoSave { get; set; }

		[NotNull]
		private Action WriteChanges { get; }

		[NotNull]
		private Action DiscardChanges { get; }

		public void BeginWrite()
		{
			_saving = true;
		}

		public void EndWrite()
		{
			_saving = false;
		}

		public void Abort()
		{
			_aborting = true;
		}

		public bool CanHandlePendingChanges([NotNull] Func<bool> hasPendingChanges,
		                                    PendingChangesOption pendingChangeOptions)
		{
			try
			{
				_hasPendingChanges = hasPendingChanges();

				if (! _hasPendingChanges)
				{
					return true;
				}

				_canSave = false;

				if (! AutoSave)
				{
					return CanHandle(pendingChangeOptions, out _canSave);
				}

				_canSave = true;
				return true;
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e.Message, e, _msg, _owner);
				return false;
			}
		}

		public bool TryWritePendingChanges()
		{
			try
			{
				if (! _hasPendingChanges)
				{
					_msg.VerboseDebug("no pending changes");
					return false;
				}

				if (AutoSave)
				{
					_canSave = true;
				}

				if (! CanHandlePendingChanges())
				{
					_msg.VerboseDebugFormat(
						"Cannot handle pending changes. Reason: saving {0}, aborting {1}, can save {2}",
						_saving,
						_aborting, _canSave);
					return false;
				}

				BeginWrite();

				WriteChanges();
				return true;
			}

			catch (Exception e)
			{
				ErrorHandler.HandleError(e.Message, e, _msg, _owner);
				return false;
			}
			finally
			{
				_canSave = false;
				EndWrite();
			}
		}

		private bool CanHandlePendingChanges()
		{
			return _canSave && ! _saving && ! _aborting;
		}

		private bool CanHandle(PendingChangesOption pendingChanges, out bool save)
		{
			save = false;
			bool canHandle;

			switch (pendingChanges)
			{
				case PendingChangesOption.SaveDiscardCancel:
					canHandle = ConfirmSaveDiscardCancelPendingChanges(out save);
					break;

				case PendingChangesOption.SaveCancel:
					canHandle = ConfirmSaveCancelPendingChanges(out save);
					break;

				case PendingChangesOption.SaveDiscard:
					canHandle = ConfirmSaveDiscardPendingChanges(out save);
					break;

				case PendingChangesOption.Save:
					save = true;
					canHandle = true;
					break;

				case PendingChangesOption.AssertNoPendingChanges:
					throw new InvalidOperationException(
						string.Format("Unexpected pending changes option: {0}", pendingChanges));

				default:
					throw new InvalidOperationException(
						string.Format("Unknown pending changes option: {0}", pendingChanges));
			}

			return canHandle;
		}

		private bool ConfirmSaveCancelPendingChanges(out bool save)
		{
			Assert.True(_hasPendingChanges, "No pending changes");

			bool canHandle;
			if (Dialog.OkCancel(_owner, _messageTitle,
			                    "Would you like to save the pending changes?"))
			{
				save = true;
				canHandle = true;
			}
			else
			{
				save = false;
				canHandle = false; // cancelled
			}

			return canHandle;
		}

		private bool ConfirmSaveDiscardPendingChanges(out bool save)
		{
			Assert.True(_hasPendingChanges, "No pending changes");

			const bool canHandle = true;

			if (Dialog.YesNo(_owner, _messageTitle,
			                 "Would you like to save the pending changes?"))
			{
				save = true;
			}
			else
			{
				DiscardChanges();
				save = false;
			}

			return canHandle;
		}

		private bool ConfirmSaveDiscardCancelPendingChanges(out bool save)
		{
			Assert.True(_hasPendingChanges, "No pending changes");

			save = false;
			bool canHandle;

			switch (Dialog.YesNoCancel(_owner, _messageTitle,
			                           "Would you like to save the pending changes?"))
			{
				case YesNoCancelDialogResult.Yes:
					save = true;
					canHandle = true;
					break;

				case YesNoCancelDialogResult.No:
					DiscardChanges();
					canHandle = true;
					break;

				case YesNoCancelDialogResult.Cancel:
					canHandle = false;
					break;

				default:
					canHandle = false;
					break;
			}

			return canHandle;
		}
	}
}
