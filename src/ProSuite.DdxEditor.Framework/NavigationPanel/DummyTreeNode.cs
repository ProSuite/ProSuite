using System;
using System.Windows.Forms;

namespace ProSuite.DdxEditor.Framework.NavigationPanel
{
	internal class DummyTreeNode : TreeNode, IDisposable
	{
		public DummyTreeNode() : base("dummy") { }

		#region IDisposable Members

		public void Dispose() { }

		#endregion
	}
}
