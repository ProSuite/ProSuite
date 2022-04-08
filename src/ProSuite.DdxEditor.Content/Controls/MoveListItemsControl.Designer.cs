using ProSuite.DdxEditor.Content.Properties;

namespace ProSuite.DdxEditor.Content.Controls
{
    partial class MoveListItemsControl
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
			this._toolStrip = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
            this._toolStripButtonMoveToTop = new System.Windows.Forms.ToolStripButton();
            this._toolStripButtonMoveUp = new System.Windows.Forms.ToolStripButton();
            this._toolStripButtonMoveDown = new System.Windows.Forms.ToolStripButton();
            this._toolStripButtonMoveToBottom = new System.Windows.Forms.ToolStripButton();
            this._toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // _toolStrip
            // 
            this._toolStrip.Dock = System.Windows.Forms.DockStyle.None;
            this._toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this._toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                                                                                        this._toolStripButtonMoveToTop,
                                                                                        this._toolStripButtonMoveUp,
                                                                                        this._toolStripButtonMoveDown,
                                                                                        this._toolStripButtonMoveToBottom});
            this._toolStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this._toolStrip.Location = new System.Drawing.Point(0, 0);
            this._toolStrip.Name = "_toolStrip";
            this._toolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this._toolStrip.Size = new System.Drawing.Size(32, 113);
            this._toolStrip.TabIndex = 0;
            this._toolStrip.Text = "Move List Items Tools";
            // 
            // _toolStripButtonMoveToTop
            // 
            this._toolStripButtonMoveToTop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._toolStripButtonMoveToTop.Image = Resources.MoveToTop;
            this._toolStripButtonMoveToTop.Name = "_toolStripButtonMoveToTop";
            this._toolStripButtonMoveToTop.Size = new System.Drawing.Size(30, 20);
            this._toolStripButtonMoveToTop.Text = "Move To Top";
            // 
            // _toolStripButtonMoveUp
            // 
            this._toolStripButtonMoveUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._toolStripButtonMoveUp.Image = Resources.MoveUp;
            this._toolStripButtonMoveUp.Name = "_toolStripButtonMoveUp";
            this._toolStripButtonMoveUp.Size = new System.Drawing.Size(30, 20);
            this._toolStripButtonMoveUp.Text = "Move Up";
            // 
            // _toolStripButtonMoveDown
            // 
            this._toolStripButtonMoveDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._toolStripButtonMoveDown.Image = Resources.MoveDown;
            this._toolStripButtonMoveDown.Name = "_toolStripButtonMoveDown";
            this._toolStripButtonMoveDown.Size = new System.Drawing.Size(30, 20);
            this._toolStripButtonMoveDown.Text = "Move Down";
            // 
            // _toolStripButtonMoveToBottom
            // 
            this._toolStripButtonMoveToBottom.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._toolStripButtonMoveToBottom.Image = Resources.MoveToBottom;
            this._toolStripButtonMoveToBottom.Name = "_toolStripButtonMoveToBottom";
            this._toolStripButtonMoveToBottom.Size = new System.Drawing.Size(30, 20);
            this._toolStripButtonMoveToBottom.Text = "Move To Bottom";
            // 
            // MoveListItemsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this._toolStrip);
            this.Name = "MoveListItemsControl";
            this.Size = new System.Drawing.Size(32, 113);
            this._toolStrip.ResumeLayout(false);
            this._toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip _toolStrip;
        private System.Windows.Forms.ToolStripButton _toolStripButtonMoveToTop;
        private System.Windows.Forms.ToolStripButton _toolStripButtonMoveUp;
        private System.Windows.Forms.ToolStripButton _toolStripButtonMoveDown;
        private System.Windows.Forms.ToolStripButton _toolStripButtonMoveToBottom;
    }
}
