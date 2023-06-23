using System;
using System.Threading;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.WinForms
{
	public class IdleCallback
	{
		[NotNull] private readonly Action _procedure;
		[CanBeNull] private SynchronizationContext _synchronizationContext;
		private int _expectedThreadId;
		private bool _pending;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public IdleCallback([NotNull] Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			_procedure = procedure;
		}

		public void Callback()
		{
			if (_pending)
			{
				_msg.VerboseDebug(() => "Callback already pending - ignored");
				return;
			}

			_expectedThreadId = Thread.CurrentThread.ManagedThreadId;

			if (_synchronizationContext == null)
			{
				// get the winforms synchronization context
				// NOTE: relying on the current synchronization context of the current thread is not sufficient, 
				//       as this gets changed depending on previous winforms actions:
				//       - after showing a modal dialog, the synchronization context DOES NOT dispatch back to the gui thread
				//       - after opening/activating a modeless form, it DOES dispatch back to the gui thread
				// -> better to set the expected synchronization context explicitly
				// NOTE: this must be done on the gui thread, otherwise even this synchronization context will not dispatch back to it
				_synchronizationContext = new WindowsFormsSynchronizationContext();
			}

			_msg.VerboseDebug(
				() =>
					$"Scheduling delayed processing (thread: {Thread.CurrentThread.ManagedThreadId})");

			try
			{
				_pending = true;

				_synchronizationContext.Post(delegate { TryExecuteProcedure(); }, null);
			}
			catch (Exception ex)
			{
				_pending = false;
				_msg.Error($"Error executing procedure via synchronization context: {ex.Message}",
				           ex);
			}
		}

		#region Non-public members

		private void TryExecuteProcedure()
		{
			Assert.NotNull(_procedure, "_procedure");
			Assert.AreEqual(_expectedThreadId, Thread.CurrentThread.ManagedThreadId,
			                "Unexpected thread for callback");
			Assert.True(_pending, "No pending callback");

			try
			{
				_procedure();
			}
			catch (Exception ex)
			{
				_msg.Error($"Error executing procedure: {ex.Message}", ex);
			}
			finally
			{
				_pending = false;
			}
		}

		#endregion
	}
}
