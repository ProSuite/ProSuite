namespace ProSuite.UI.Core.QA.Customize
{
	partial class ConditionsLayerViewControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this._contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this._toolStripMenuItemCollapseAll = new System.Windows.Forms.ToolStripMenuItem();
			this._treeViewControlConditions = new global::ProSuite.UI.Core.QA.Controls.TestTreeViewControl();
			this._contextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// _contextMenuStrip
			// 
			this._contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripMenuItemCollapseAll});
			this._contextMenuStrip.Name = "_contextMenuStrip";
			this._contextMenuStrip.Size = new System.Drawing.Size(137, 26);
			// 
			// _toolStripMenuItemCollapseAll
			// 
			this._toolStripMenuItemCollapseAll.Name = "_toolStripMenuItemCollapseAll";
			this._toolStripMenuItemCollapseAll.Size = new System.Drawing.Size(136, 22);
			this._toolStripMenuItemCollapseAll.Text = "Collapse All";
			this._toolStripMenuItemCollapseAll.Click += new System.EventHandler(this._toolStripMenuItemCollapseAll_Click);
			// 
			// _treeViewControlConditions
			// 
			this._treeViewControlConditions.CheckBoxes = true;
			this._treeViewControlConditions.ContextMenuStrip = this._contextMenuStrip;
			this._treeViewControlConditions.Dock = System.Windows.Forms.DockStyle.Fill;
			this._treeViewControlConditions.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
			this._treeViewControlConditions.HideSelection = false;
			this._treeViewControlConditions.ImageIndex = 0;
			this._treeViewControlConditions.Location = new System.Drawing.Point(0, 0);
			this._treeViewControlConditions.Name = "_treeViewControlConditions";
			this._treeViewControlConditions.SelectedImageIndex = 0;
			this._treeViewControlConditions.Size = new System.Drawing.Size(727, 283);
			this._treeViewControlConditions.TabIndex = 1;
			this._treeViewControlConditions.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this._treeViewConditions_AfterCheck);
			this._treeViewControlConditions.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this._treeViewConditions_AfterSelect);
			// 
			// ConditionsLayerViewControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._treeViewControlConditions);
			this.Name = "ConditionsLayerViewControl";
			this.Size = new System.Drawing.Size(727, 283);
			this._contextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private global::ProSuite.UI.Core.QA.Controls.TestTreeViewControl _treeViewControlConditions;
		private System.Windows.Forms.ContextMenuStrip _contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemCollapseAll;
	}
}
