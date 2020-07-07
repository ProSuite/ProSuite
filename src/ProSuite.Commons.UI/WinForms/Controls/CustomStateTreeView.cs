using System;
using System.Windows.Forms;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class CustomStateTreeView : TreeView
	{
		protected virtual void CreateStateImages() { }

		protected virtual void OnCustomCheck(TreeNode node, TreeViewAction action)
		{
			var e = new TreeViewCancelEventArgs(node, false, action);
			OnBeforeCheck(e);
			if (e.Cancel)
			{
				return;
			}

			if (node is IStateTreeNode)
			{
				((IStateTreeNode) node).UpdateState(e);
			}

			OnAfterCheck(new TreeViewEventArgs(node, action));
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			StateImageList = new ImageList();
			CreateStateImages();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			TreeViewHitTestInfo info = HitTest(e.X, e.Y);

			if (info.Node != null && info.Location.ToString() == "StateImage")
			{
				OnCustomCheck(info.Node, TreeViewAction.ByMouse);
			}

			base.OnMouseDown(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Space:
					if (StateImageList != null && SelectedNode != null)
					{
						OnCustomCheck(SelectedNode, TreeViewAction.ByKeyboard);
					}

					e.Handled = true;
					break;
			}

			base.OnKeyDown(e);
		}
	}
}
