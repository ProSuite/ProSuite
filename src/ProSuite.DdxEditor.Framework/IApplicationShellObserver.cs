using System.Windows.Forms;

namespace ProSuite.DdxEditor.Framework
{
	public interface IApplicationShellObserver
	{
		bool TrySavePendingChanges();

		void DiscardPendingChanges();

		/// <summary>
		/// Handles the form closing.
		/// </summary>
		/// <param name="canCancel">if set to <c>true</c> it is possible to cancel the 
		/// closing of the form.</param>
		/// <returns><c>true</c> if the form should close, <c>false</c> if the closing 
		/// should be cancelled.</returns>
		bool HandleFormClosing(bool canCancel);

		void FormClosed();

		void GoBack();

		void GoForward();

		void ShowOptions();

		void ShowAbout();

		void KeyDownPreview(KeyEventArgs keyEventArgs);
	}
}
