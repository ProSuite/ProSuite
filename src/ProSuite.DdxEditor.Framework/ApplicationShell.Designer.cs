namespace ProSuite.DdxEditor.Framework
{
    partial class ApplicationShell
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApplicationShell));
			this._splitContainerInner = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
            this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._splitContainerOuter = new global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx();
            this._menuStrip = new System.Windows.Forms.MenuStrip();
            this._toolStripMenuItemFile = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItemConfiguration = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItemOptions = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this._toolStripMenuItemExit = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItemSearch = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItemHelp = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripMenuItemAbout = new System.Windows.Forms.ToolStripMenuItem();
			this._toolStrip = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
            this._toolStripButtonSave = new System.Windows.Forms.ToolStripButton();
            this._toolStripButtonDiscardChanges = new System.Windows.Forms.ToolStripButton();
            this._toolStripButtonBack = new System.Windows.Forms.ToolStripButton();
            this._toolStripButtonForward = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            ((System.ComponentModel.ISupportInitialize)(this._splitContainerInner)).BeginInit();
            this._splitContainerInner.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._splitContainerOuter)).BeginInit();
            this._splitContainerOuter.Panel2.SuspendLayout();
            this._splitContainerOuter.SuspendLayout();
            this._menuStrip.SuspendLayout();
            this._toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // _splitContainerInner
            // 
            this._splitContainerInner.BackColor = System.Drawing.SystemColors.ControlDark;
            this._splitContainerInner.Dock = System.Windows.Forms.DockStyle.Fill;
            this._splitContainerInner.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this._splitContainerInner.Location = new System.Drawing.Point(0, 0);
            this._splitContainerInner.Name = "_splitContainerInner";
            this._splitContainerInner.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _splitContainerInner.Panel1
            // 
            this._splitContainerInner.Panel1.BackColor = System.Drawing.SystemColors.Control;
            // 
            // _splitContainerInner.Panel2
            // 
            this._splitContainerInner.Panel2.BackColor = System.Drawing.SystemColors.Control;
			this._splitContainerInner.Size = new System.Drawing.Size(533, 442);
			this._splitContainerInner.SplitterDistance = 311;
            this._splitContainerInner.SplitterWidth = 3;
            this._splitContainerInner.TabIndex = 0;
            // 
            // _statusStrip
            // 
			this._statusStrip.Location = new System.Drawing.Point(0, 491);
            this._statusStrip.Name = "_statusStrip";
			this._statusStrip.Size = new System.Drawing.Size(745, 22);
            this._statusStrip.TabIndex = 1;
            // 
            // _splitContainerOuter
            // 
            this._splitContainerOuter.BackColor = System.Drawing.SystemColors.ControlDark;
            this._splitContainerOuter.Dock = System.Windows.Forms.DockStyle.Fill;
            this._splitContainerOuter.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this._splitContainerOuter.Location = new System.Drawing.Point(0, 49);
            this._splitContainerOuter.Name = "_splitContainerOuter";
            // 
            // _splitContainerOuter.Panel1
            // 
            this._splitContainerOuter.Panel1.BackColor = System.Drawing.SystemColors.Control;
            // 
            // _splitContainerOuter.Panel2
            // 
            this._splitContainerOuter.Panel2.BackColor = System.Drawing.SystemColors.Control;
            this._splitContainerOuter.Panel2.Controls.Add(this._splitContainerInner);
			this._splitContainerOuter.Size = new System.Drawing.Size(745, 442);
            this._splitContainerOuter.SplitterDistance = 208;
            this._splitContainerOuter.TabIndex = 3;
            // 
            // _menuStrip
            // 
            this._menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripMenuItemFile,
            this._toolStripMenuItemSearch,
            this._toolStripMenuItemHelp});
            this._menuStrip.Location = new System.Drawing.Point(0, 0);
            this._menuStrip.Name = "_menuStrip";
            this._menuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._menuStrip.Size = new System.Drawing.Size(745, 24);
            this._menuStrip.TabIndex = 4;
            this._menuStrip.Text = "Menu";
            // 
            // _toolStripMenuItemFile
            // 
            this._toolStripMenuItemFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripMenuItemConfiguration,
            this._toolStripMenuItemOptions,
            this.toolStripSeparator3,
            this._toolStripMenuItemExit});
            this._toolStripMenuItemFile.Name = "_toolStripMenuItemFile";
            this._toolStripMenuItemFile.Size = new System.Drawing.Size(37, 20);
            this._toolStripMenuItemFile.Text = "&File";
			// 
			// _toolStripMenuItemConfiguration
			// 
			this._toolStripMenuItemConfiguration.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.ConfigurationEditor;
			this._toolStripMenuItemConfiguration.Name = "_toolStripMenuItemConfiguration";
            this._toolStripMenuItemConfiguration.Size = new System.Drawing.Size(180, 22);
            this._toolStripMenuItemConfiguration.Text = "&Configuration";
            this._toolStripMenuItemConfiguration.Click += new System.EventHandler(this._toolStripMenuItemConfiguration_Click);
            // 
            // _toolStripMenuItemOptions
            // 
            this._toolStripMenuItemOptions.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.Options;
            this._toolStripMenuItemOptions.Name = "_toolStripMenuItemOptions";
			this._toolStripMenuItemOptions.Size = new System.Drawing.Size(125, 22);
            this._toolStripMenuItemOptions.Text = "&Options...";
            this._toolStripMenuItemOptions.Visible = false;
            this._toolStripMenuItemOptions.Click += new System.EventHandler(this._toolStripMenuItemOptions_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(122, 6);
            // 
            // _toolStripMenuItemExit
            // 
            this._toolStripMenuItemExit.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.Exit;
            this._toolStripMenuItemExit.Name = "_toolStripMenuItemExit";
			this._toolStripMenuItemExit.Size = new System.Drawing.Size(125, 22);
            this._toolStripMenuItemExit.Text = "Exit";
            this._toolStripMenuItemExit.Click += new System.EventHandler(this._toolStripMenuItemExit_Click);
            // 
            // _toolStripMenuItemSearch
            // 
            this._toolStripMenuItemSearch.Name = "_toolStripMenuItemSearch";
            this._toolStripMenuItemSearch.Size = new System.Drawing.Size(54, 20);
            this._toolStripMenuItemSearch.Text = "&Search";
            this._toolStripMenuItemSearch.Visible = false;
            // 
            // _toolStripMenuItemHelp
            // 
            this._toolStripMenuItemHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripMenuItemAbout});
            this._toolStripMenuItemHelp.Name = "_toolStripMenuItemHelp";
            this._toolStripMenuItemHelp.Size = new System.Drawing.Size(44, 20);
            this._toolStripMenuItemHelp.Text = "&Help";
            // 
            // _toolStripMenuItemAbout
            // 
            this._toolStripMenuItemAbout.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.ShowAboutBoxCmd;
            this._toolStripMenuItemAbout.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this._toolStripMenuItemAbout.Name = "_toolStripMenuItemAbout";
            this._toolStripMenuItemAbout.Size = new System.Drawing.Size(225, 22);
            this._toolStripMenuItemAbout.Text = "&About Data Dictionary Editor";
            this._toolStripMenuItemAbout.Click += new System.EventHandler(this._toolStripMenuItemAbout_Click);
            // 
            // _toolStrip
            // 
            this._toolStrip.ClickThrough = true;
            this._toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this._toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripButtonSave,
            this._toolStripButtonDiscardChanges,
            this._toolStripButtonBack,
            this._toolStripButtonForward,
            this.toolStripSeparator1,
            this.toolStripSeparator2});
            this._toolStrip.Location = new System.Drawing.Point(0, 24);
            this._toolStrip.Name = "_toolStrip";
            this._toolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolStrip.Size = new System.Drawing.Size(745, 25);
            this._toolStrip.TabIndex = 2;
            this._toolStrip.Text = "Tools";
            // 
            // _toolStripButtonSave
            // 
            this._toolStripButtonSave.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this._toolStripButtonSave.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.Save;
            this._toolStripButtonSave.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this._toolStripButtonSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._toolStripButtonSave.Name = "_toolStripButtonSave";
            this._toolStripButtonSave.Size = new System.Drawing.Size(51, 22);
            this._toolStripButtonSave.Text = "Save";
            this._toolStripButtonSave.Click += new System.EventHandler(this._toolStripButtonSave_Click);
            // 
            // _toolStripButtonDiscardChanges
            // 
            this._toolStripButtonDiscardChanges.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this._toolStripButtonDiscardChanges.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.DiscardChanges;
            this._toolStripButtonDiscardChanges.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this._toolStripButtonDiscardChanges.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._toolStripButtonDiscardChanges.Name = "_toolStripButtonDiscardChanges";
            this._toolStripButtonDiscardChanges.Size = new System.Drawing.Size(66, 22);
            this._toolStripButtonDiscardChanges.Text = "Discard";
            this._toolStripButtonDiscardChanges.Click += new System.EventHandler(this._toolStripButtonDiscardChanges_Click);
            // 
            // _toolStripButtonBack
            // 
            this._toolStripButtonBack.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.GoToLastVisited;
            this._toolStripButtonBack.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this._toolStripButtonBack.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._toolStripButtonBack.Name = "_toolStripButtonBack";
            this._toolStripButtonBack.Size = new System.Drawing.Size(52, 22);
            this._toolStripButtonBack.Text = "Back";
            this._toolStripButtonBack.Click += new System.EventHandler(this._toolStripButtonBack_Click);
            // 
            // _toolStripButtonForward
            // 
            this._toolStripButtonForward.Image = global::ProSuite.DdxEditor.Framework.Properties.Resources.GoToNextVisited;
            this._toolStripButtonForward.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this._toolStripButtonForward.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._toolStripButtonForward.Name = "_toolStripButtonForward";
			this._toolStripButtonForward.Size = new System.Drawing.Size(51, 22);
            this._toolStripButtonForward.Text = "Next";
            this._toolStripButtonForward.Click += new System.EventHandler(this._toolStripButtonForward_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // ApplicationShell
            // 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(745, 513);
            this.Controls.Add(this._splitContainerOuter);
            this.Controls.Add(this._toolStrip);
            this.Controls.Add(this._statusStrip);
            this.Controls.Add(this._menuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this._menuStrip;
            this.Name = "ApplicationShell";
            this.Text = "Data Dictionary Administration Tool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ApplicationShell_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ApplicationShell_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this._splitContainerInner)).EndInit();
            this._splitContainerInner.ResumeLayout(false);
            this._splitContainerOuter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._splitContainerOuter)).EndInit();
            this._splitContainerOuter.ResumeLayout(false);
            this._menuStrip.ResumeLayout(false);
            this._menuStrip.PerformLayout();
            this._toolStrip.ResumeLayout(false);
            this._toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerInner;
		private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripButton _toolStripButtonSave;
        private System.Windows.Forms.ToolStripButton _toolStripButtonDiscardChanges;
        private global::ProSuite.Commons.UI.WinForms.Controls.SplitContainerEx _splitContainerOuter;
        private System.Windows.Forms.ToolStripButton _toolStripButtonBack;
        private System.Windows.Forms.ToolStripButton _toolStripButtonForward;
        private System.Windows.Forms.MenuStrip _menuStrip;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemFile;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemExit;
        private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemHelp;
		private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemAbout;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx _toolStrip;
		private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemOptions;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemSearch;
		private System.Windows.Forms.ToolStripMenuItem _toolStripMenuItemConfiguration;
	}
}
