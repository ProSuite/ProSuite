using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	/// <summary>
	/// Treenode that supports tristate checkboxes
	/// </summary>
	public class TriStateTreeNode : TreeNode, IStateTreeNode
	{
		private CheckState _checkState;

		#region Constructors

		public TriStateTreeNode(string text) : base(text) { }

		#endregion

		public new bool Checked
		{
			get { return base.Checked; }
			set
			{
				base.Checked = value;

				SetCheckStateCore(value
					                  ? CheckState.Checked
					                  : CheckState.Unchecked);
			}
		}

		public virtual CheckState CheckState
		{
			get { return _checkState; }
			set
			{
				base.Checked = value != CheckState.Unchecked;

				SetCheckStateCore(value);
			}
		}

		#region IStateTreeNode Members

		void IStateTreeNode.UpdateState(TreeViewCancelEventArgs e)
		{
			switch (CheckState)
			{
				case CheckState.Checked:
					CheckState = CheckState.Unchecked;
					break;

				case CheckState.Indeterminate:
					CheckState = CheckState.Checked;
					break;

				case CheckState.Unchecked:
					CheckState = CheckState.Checked;
					break;
			}
		}

		#endregion

		private void SetCheckStateCore(CheckState checkState)
		{
			_checkState = checkState;

			StateImageIndex = (int) checkState;
		}
	}
}
