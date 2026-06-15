using ProSuite.UI.Core.QA.Controls;

namespace ProSuite.UI.Core.QA.VerificationResult
{
	partial class VerifiedConditionsHierarchyControl
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
			this._splitContainerConditions = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
			this._treeViewConditions = new TestTreeViewControl();
			this._panelExecuteInfo = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this._splitContainerConditions)).BeginInit();
			this._splitContainerConditions.Panel1.SuspendLayout();
			this._splitContainerConditions.Panel2.SuspendLayout();
			this._splitContainerConditions.SuspendLayout();
			this.SuspendLayout();
			// 
			// _splitContainerConditions
			// 
			this._splitContainerConditions.Dock = System.Windows.Forms.DockStyle.Fill;
			this._splitContainerConditions.Location = new System.Drawing.Point(0, 0);
			this._splitContainerConditions.Name = "_splitContainerConditions";
			// 
			// _splitContainerConditions.Panel1
			// 
			this._splitContainerConditions.Panel1.Controls.Add(this._treeViewConditions);
			// 
			// _splitContainerConditions.Panel2
			// 
			this._splitContainerConditions.Panel2.Controls.Add(this._panelExecuteInfo);
			this._splitContainerConditions.Size = new System.Drawing.Size(511, 393);
			this._splitContainerConditions.SplitterDistance = 233;
			this._splitContainerConditions.SplitterWidth = 1;
			this._splitContainerConditions.TabIndex = 1;
			// 
			// _treeViewConditions
			// 
			this._treeViewConditions.Dock = System.Windows.Forms.DockStyle.Fill;
			this._treeViewConditions.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
			this._treeViewConditions.HideSelection = false;
			this._treeViewConditions.ImageIndex = 0;
			this._treeViewConditions.Location = new System.Drawing.Point(0, 0);
			this._treeViewConditions.Name = "_treeViewConditions";
			this._treeViewConditions.SelectedImageIndex = 0;
			this._treeViewConditions.Size = new System.Drawing.Size(233, 393);
			this._treeViewConditions.TabIndex = 0;
			this._treeViewConditions.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this._treeViewConditions_DrawNode);
			this._treeViewConditions.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this._treeViewConditions_AfterSelect);
			// 
			// _panelExecuteInfo
			// 
			this._panelExecuteInfo.BackColor = System.Drawing.SystemColors.Window;
			this._panelExecuteInfo.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panelExecuteInfo.Location = new System.Drawing.Point(0, 0);
			this._panelExecuteInfo.Name = "_panelExecuteInfo";
			this._panelExecuteInfo.Size = new System.Drawing.Size(277, 393);
			this._panelExecuteInfo.TabIndex = 8;
			this._panelExecuteInfo.Paint += new System.Windows.Forms.PaintEventHandler(this._panelExecuteInfo_Paint);
			// 
			// VerifiedConditionsHierarchyControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._splitContainerConditions);
			this.Name = "VerifiedConditionsHierarchyControl";
			this.Size = new System.Drawing.Size(511, 393);
			this.Load += new System.EventHandler(this.VerifiedConditionsHierarchyControl_Load);
			this._splitContainerConditions.Panel1.ResumeLayout(false);
			this._splitContainerConditions.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this._splitContainerConditions)).EndInit();
			this._splitContainerConditions.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerConditions;
		private TestTreeViewControl _treeViewConditions;
		private System.Windows.Forms.Panel _panelExecuteInfo;
	}
}
