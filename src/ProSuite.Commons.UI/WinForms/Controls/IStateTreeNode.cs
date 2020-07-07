using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Interface used by <see cref="CustomStateTreeView"/> to delegate state change.
	/// </summary>
	public interface IStateTreeNode
	{
		void UpdateState(TreeViewCancelEventArgs e);
	}
}
