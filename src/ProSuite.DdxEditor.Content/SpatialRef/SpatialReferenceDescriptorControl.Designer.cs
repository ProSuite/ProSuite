namespace ProSuite.DdxEditor.Content.SpatialRef
{
    partial class SpatialReferenceDescriptorControl
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
			this._labelName = new System.Windows.Forms.Label();
			this._textBoxName = new System.Windows.Forms.TextBox();
			this._textBoxDescription = new System.Windows.Forms.TextBox();
			this._labelDescription = new System.Windows.Forms.Label();
			this.toolStrip1 = new global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this._toolStripButtonCopy = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonImportFromFeatureClass = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonImportFromDataset = new System.Windows.Forms.ToolStripButton();
			this._tabControlProperties = new System.Windows.Forms.TabControl();
			this._tabPageProperties = new System.Windows.Forms.TabPage();
			this._propertyGridGeneral = new System.Windows.Forms.PropertyGrid();
			this._tabPageXml = new System.Windows.Forms.TabPage();
			this._webBrowserXml = new System.Windows.Forms.WebBrowser();
			this.toolStrip1.SuspendLayout();
			this._tabControlProperties.SuspendLayout();
			this._tabPageProperties.SuspendLayout();
			this._tabPageXml.SuspendLayout();
			this.SuspendLayout();
			// 
			// _labelName
			// 
			this._labelName.AutoSize = true;
			this._labelName.Location = new System.Drawing.Point(29, 7);
			this._labelName.Name = "_labelName";
			this._labelName.Size = new System.Drawing.Size(38, 13);
			this._labelName.TabIndex = 0;
			this._labelName.Text = "Name:";
			this._labelName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxName
			// 
			this._textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxName.Location = new System.Drawing.Point(73, 4);
			this._textBoxName.Name = "_textBoxName";
			this._textBoxName.Size = new System.Drawing.Size(537, 20);
			this._textBoxName.TabIndex = 0;
			// 
			// _textBoxDescription
			// 
			this._textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxDescription.Location = new System.Drawing.Point(73, 30);
			this._textBoxDescription.Multiline = true;
			this._textBoxDescription.Name = "_textBoxDescription";
			this._textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxDescription.Size = new System.Drawing.Size(537, 76);
			this._textBoxDescription.TabIndex = 1;
			// 
			// _labelDescription
			// 
			this._labelDescription.AutoSize = true;
			this._labelDescription.Location = new System.Drawing.Point(4, 33);
			this._labelDescription.Name = "_labelDescription";
			this._labelDescription.Size = new System.Drawing.Size(63, 13);
			this._labelDescription.TabIndex = 2;
			this._labelDescription.Text = "Description:";
			this._labelDescription.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// toolStrip1
			// 
			this.toolStrip1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.toolStrip1.AutoSize = false;
			this.toolStrip1.ClickThrough = true;
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripButtonCopy,
            this._toolStripButtonImportFromFeatureClass,
            this._toolStripButtonImportFromDataset});
			this.toolStrip1.Location = new System.Drawing.Point(210, 118);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip1.Size = new System.Drawing.Size(417, 25);
			this.toolStrip1.TabIndex = 2;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// _toolStripButtonCopy
			// 
			this._toolStripButtonCopy.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonCopy.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Copy;
			this._toolStripButtonCopy.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this._toolStripButtonCopy.Name = "_toolStripButtonCopy";
			this._toolStripButtonCopy.Size = new System.Drawing.Size(55, 22);
			this._toolStripButtonCopy.Text = "Copy";
			this._toolStripButtonCopy.ToolTipText = "Copy To Clipboard";
			this._toolStripButtonCopy.Click += new System.EventHandler(this._toolStripButtonCopy_Click);
			// 
			// _toolStripButtonImportFromFeatureClass
			// 
			this._toolStripButtonImportFromFeatureClass.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonImportFromFeatureClass.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.ArcCatalog;
			this._toolStripButtonImportFromFeatureClass.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this._toolStripButtonImportFromFeatureClass.Name = "_toolStripButtonImportFromFeatureClass";
			this._toolStripButtonImportFromFeatureClass.Size = new System.Drawing.Size(179, 22);
			this._toolStripButtonImportFromFeatureClass.Text = "Get From Workspace Dataset";
			this._toolStripButtonImportFromFeatureClass.Click += new System.EventHandler(this._toolStripButtonImportFromFeatureClass_Click);
			// 
			// _toolStripButtonImportFromDataset
			// 
			this._toolStripButtonImportFromDataset.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this._toolStripButtonImportFromDataset.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.Find;
			this._toolStripButtonImportFromDataset.Name = "_toolStripButtonImportFromDataset";
			this._toolStripButtonImportFromDataset.Size = new System.Drawing.Size(176, 22);
			this._toolStripButtonImportFromDataset.Text = "Get From Registered Dataset";
			this._toolStripButtonImportFromDataset.Click += new System.EventHandler(this._buttonGetFromDataset_Click);
			// 
			// _tabControlProperties
			// 
			this._tabControlProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tabControlProperties.Controls.Add(this._tabPageProperties);
			this._tabControlProperties.Controls.Add(this._tabPageXml);
			this._tabControlProperties.Location = new System.Drawing.Point(7, 124);
			this._tabControlProperties.Name = "_tabControlProperties";
			this._tabControlProperties.SelectedIndex = 0;
			this._tabControlProperties.Size = new System.Drawing.Size(620, 362);
			this._tabControlProperties.TabIndex = 12;
			this._tabControlProperties.SelectedIndexChanged += new System.EventHandler(this._tabControlProperties_SelectedIndexChanged);
			// 
			// _tabPageProperties
			// 
			this._tabPageProperties.Controls.Add(this._propertyGridGeneral);
			this._tabPageProperties.Location = new System.Drawing.Point(4, 22);
			this._tabPageProperties.Name = "_tabPageProperties";
			this._tabPageProperties.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageProperties.Size = new System.Drawing.Size(612, 336);
			this._tabPageProperties.TabIndex = 0;
			this._tabPageProperties.Text = "Spatial Reference Definition";
			this._tabPageProperties.UseVisualStyleBackColor = true;
			// 
			// _propertyGridGeneral
			// 
			this._propertyGridGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
			this._propertyGridGeneral.Location = new System.Drawing.Point(3, 3);
			this._propertyGridGeneral.Name = "_propertyGridGeneral";
			this._propertyGridGeneral.PropertySort = System.Windows.Forms.PropertySort.NoSort;
			this._propertyGridGeneral.Size = new System.Drawing.Size(606, 330);
			this._propertyGridGeneral.TabIndex = 1;
			this._propertyGridGeneral.ToolbarVisible = false;
			// 
			// _tabPageXml
			// 
			this._tabPageXml.Controls.Add(this._webBrowserXml);
			this._tabPageXml.Location = new System.Drawing.Point(4, 22);
			this._tabPageXml.Name = "_tabPageXml";
			this._tabPageXml.Padding = new System.Windows.Forms.Padding(3);
			this._tabPageXml.Size = new System.Drawing.Size(612, 336);
			this._tabPageXml.TabIndex = 1;
			this._tabPageXml.Text = "XML";
			this._tabPageXml.UseVisualStyleBackColor = true;
			// 
			// _webBrowserXml
			// 
			this._webBrowserXml.Dock = System.Windows.Forms.DockStyle.Fill;
			this._webBrowserXml.Location = new System.Drawing.Point(3, 3);
			this._webBrowserXml.MinimumSize = new System.Drawing.Size(20, 20);
			this._webBrowserXml.Name = "_webBrowserXml";
			this._webBrowserXml.Size = new System.Drawing.Size(606, 330);
			this._webBrowserXml.TabIndex = 1;
			this._webBrowserXml.WebBrowserShortcutsEnabled = false;
			this._webBrowserXml.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(this._webBrowserXml_Navigated);
			this._webBrowserXml.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this._webBrowserXml_Navigating);
			// 
			// SpatialReferenceDescriptorControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._textBoxDescription);
			this.Controls.Add(this._labelDescription);
			this.Controls.Add(this._textBoxName);
			this.Controls.Add(this._labelName);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this._tabControlProperties);
			this.Name = "SpatialReferenceDescriptorControl";
			this.Size = new System.Drawing.Size(630, 489);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this._tabControlProperties.ResumeLayout(false);
			this._tabPageProperties.ResumeLayout(false);
			this._tabPageXml.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.TextBox _textBoxDescription;
		private System.Windows.Forms.Label _labelDescription;
        private System.Windows.Forms.ToolStripButton _toolStripButtonImportFromDataset;
        private System.Windows.Forms.ToolStripButton _toolStripButtonCopy;
        private System.Windows.Forms.ToolStripButton _toolStripButtonImportFromFeatureClass;
        private global::ProSuite.Commons.UI.WinForms.Controls.ToolStripEx toolStrip1;
		private System.Windows.Forms.TabControl _tabControlProperties;
		private System.Windows.Forms.TabPage _tabPageProperties;
		private System.Windows.Forms.TabPage _tabPageXml;
		private System.Windows.Forms.WebBrowser _webBrowserXml;
		private System.Windows.Forms.PropertyGrid _propertyGridGeneral;
    }
}
