namespace ProSuite.Commons.UI.WinForms.Controls
{
    partial class ExpanderControl
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
			this._panelContent = new System.Windows.Forms.Panel();
			this._toolStripTitle = new ToolStripEx();
			this._toolStripLabelTitle = new System.Windows.Forms.ToolStripLabel();
			this._toolStripButtonTogglePanel = new System.Windows.Forms.ToolStripButton();
			this._toolStripTitle.SuspendLayout();
			this.SuspendLayout();
			// 
			// _panelContent
			// 
			this._panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panelContent.Location = new System.Drawing.Point(0, 25);
			this._panelContent.Name = "_panelContent";
			this._panelContent.Size = new System.Drawing.Size(231, 125);
			this._panelContent.TabIndex = 6;
			// 
			// _toolStripTitle
			// 
			this._toolStripTitle.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
			this._toolStripTitle.ClickThrough = true;
			this._toolStripTitle.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolStripTitle.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripLabelTitle,
            this._toolStripButtonTogglePanel});
			this._toolStripTitle.Location = new System.Drawing.Point(0, 0);
			this._toolStripTitle.Name = "_toolStripTitle";
			this._toolStripTitle.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStripTitle.Size = new System.Drawing.Size(231, 25);
			this._toolStripTitle.TabIndex = 7;
			this._toolStripTitle.Text = "toolStripTitle";
			this._toolStripTitle.DoubleClick += new System.EventHandler(this._toolStripTitle_DoubleClick);
			// 
			// _toolStripLabelTitle
			// 
			this._toolStripLabelTitle.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
			this._toolStripLabelTitle.Name = "_toolStripLabelTitle";
			this._toolStripLabelTitle.Size = new System.Drawing.Size(0, 22);
			// 
			// _toolStripButtonTogglePanel
			// 
			this._toolStripButtonTogglePanel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonTogglePanel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._toolStripButtonTogglePanel.Image = global::ProSuite.Commons.UI.Properties.Resources.Collapse;
			this._toolStripButtonTogglePanel.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this._toolStripButtonTogglePanel.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._toolStripButtonTogglePanel.Name = "_toolStripButtonTogglePanel";
			this._toolStripButtonTogglePanel.Size = new System.Drawing.Size(23, 22);
			this._toolStripButtonTogglePanel.Text = "Expand / Collapse";
			this._toolStripButtonTogglePanel.Click += new System.EventHandler(this._toolStripButtonTogglePanel_Click);
			// 
			// ExpanderControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._panelContent);
			this.Controls.Add(this._toolStripTitle);
			this.Name = "ExpanderControl";
			this.Size = new System.Drawing.Size(231, 150);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.ExpanderControl_Paint);
			this._toolStripTitle.ResumeLayout(false);
			this._toolStripTitle.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.Panel _panelContent;
        private System.Windows.Forms.ToolStripLabel _toolStripLabelTitle;
        private System.Windows.Forms.ToolStripButton _toolStripButtonTogglePanel;
		private ToolStripEx _toolStripTitle;
    }
}