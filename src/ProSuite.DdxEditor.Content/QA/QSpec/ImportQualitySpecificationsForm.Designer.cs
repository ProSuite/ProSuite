namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	partial class ImportQualitySpecificationsForm
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
			this._groupBoxOptions = new System.Windows.Forms.GroupBox();
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets = new System.Windows.Forms.CheckBox();
			this._checkBoxUpdateTestDescriptorProperties = new System.Windows.Forms.CheckBox();
			this._checkBoxUpdateTestDescriptorNames = new System.Windows.Forms.CheckBox();
			this._groupBoxExportTo = new System.Windows.Forms.GroupBox();
			this._labelXmlFile = new System.Windows.Forms.Label();
			this._fileSystemPathControl = new global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl();
			this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this._buttonOK = new System.Windows.Forms.Button();
			this._buttonCancel = new System.Windows.Forms.Button();
			this._groupBoxOptions.SuspendLayout();
			this._groupBoxExportTo.SuspendLayout();
			this._statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// _groupBoxOptions
			// 
			this._groupBoxOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxOptions.Controls.Add(this._checkBoxIgnoreQualityConditionsForUnknownDatasets);
			this._groupBoxOptions.Controls.Add(this._checkBoxUpdateTestDescriptorProperties);
			this._groupBoxOptions.Controls.Add(this._checkBoxUpdateTestDescriptorNames);
			this._groupBoxOptions.Location = new System.Drawing.Point(12, 66);
			this._groupBoxOptions.Name = "_groupBoxOptions";
			this._groupBoxOptions.Size = new System.Drawing.Size(377, 93);
			this._groupBoxOptions.TabIndex = 1;
			this._groupBoxOptions.TabStop = false;
			this._groupBoxOptions.Text = "Options";
			// 
			// _checkBoxIgnoreQualityConditionsForUnknownDatasets
			// 
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.AutoSize = true;
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.Location = new System.Drawing.Point(16, 19);
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.Name = "_checkBoxIgnoreQualityConditionsForUnknownDatasets";
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.Size = new System.Drawing.Size(245, 17);
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.TabIndex = 0;
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.Text = "Ignore quality conditions for unknown datasets";
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.UseVisualStyleBackColor = true;
			// 
			// _checkBoxUpdateTestDescriptorProperties
			// 
			this._checkBoxUpdateTestDescriptorProperties.AutoSize = true;
			this._checkBoxUpdateTestDescriptorProperties.Location = new System.Drawing.Point(16, 65);
			this._checkBoxUpdateTestDescriptorProperties.Name = "_checkBoxUpdateTestDescriptorProperties";
			this._checkBoxUpdateTestDescriptorProperties.Size = new System.Drawing.Size(261, 17);
			this._checkBoxUpdateTestDescriptorProperties.TabIndex = 2;
			this._checkBoxUpdateTestDescriptorProperties.Text = "Update other properties of existing test descriptors";
			this._checkBoxUpdateTestDescriptorProperties.UseVisualStyleBackColor = true;
			// 
			// _checkBoxUpdateTestDescriptorNames
			// 
			this._checkBoxUpdateTestDescriptorNames.AutoSize = true;
			this._checkBoxUpdateTestDescriptorNames.Location = new System.Drawing.Point(16, 42);
			this._checkBoxUpdateTestDescriptorNames.Name = "_checkBoxUpdateTestDescriptorNames";
			this._checkBoxUpdateTestDescriptorNames.Size = new System.Drawing.Size(219, 17);
			this._checkBoxUpdateTestDescriptorNames.TabIndex = 1;
			this._checkBoxUpdateTestDescriptorNames.Text = "Update names of existing test descriptors";
			this._checkBoxUpdateTestDescriptorNames.UseVisualStyleBackColor = true;
			// 
			// _groupBoxExportTo
			// 
			this._groupBoxExportTo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxExportTo.Controls.Add(this._labelXmlFile);
			this._groupBoxExportTo.Controls.Add(this._fileSystemPathControl);
			this._groupBoxExportTo.Location = new System.Drawing.Point(12, 6);
			this._groupBoxExportTo.Name = "_groupBoxExportTo";
			this._groupBoxExportTo.Size = new System.Drawing.Size(377, 54);
			this._groupBoxExportTo.TabIndex = 0;
			this._groupBoxExportTo.TabStop = false;
			this._groupBoxExportTo.Text = "Import from";
			// 
			// _labelXmlFile
			// 
			this._labelXmlFile.AutoSize = true;
			this._labelXmlFile.Location = new System.Drawing.Point(13, 24);
			this._labelXmlFile.Name = "_labelXmlFile";
			this._labelXmlFile.Size = new System.Drawing.Size(26, 13);
			this._labelXmlFile.TabIndex = 6;
			this._labelXmlFile.Text = "File:";
			// 
			// _fileSystemPathControl
			// 
			this._fileSystemPathControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._fileSystemPathControl.ControlPathType = global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathType.ChooseFileName;
			this._fileSystemPathControl.FileCheckFileExists = false;
			this._fileSystemPathControl.FileCheckPathExists = true;
			this._fileSystemPathControl.FileDefaultExtension = null;
			this._fileSystemPathControl.FileFilter = null;
			this._fileSystemPathControl.FolderGroupTitle = "Choose Folder";
			this._fileSystemPathControl.FolderShowNewFolderButton = true;
			this._fileSystemPathControl.Location = new System.Drawing.Point(45, 18);
			this._fileSystemPathControl.Name = "_fileSystemPathControl";
			this._fileSystemPathControl.Size = new System.Drawing.Size(326, 26);
			this._fileSystemPathControl.TabIndex = 0;
			this._fileSystemPathControl.ValueChanged += new System.EventHandler(this._fileSystemPathControl_ValueChanged);
			// 
			// _statusStrip
			// 
			this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripStatusLabel});
			this._statusStrip.Location = new System.Drawing.Point(0, 198);
			this._statusStrip.Name = "_statusStrip";
			this._statusStrip.Size = new System.Drawing.Size(401, 22);
			this._statusStrip.TabIndex = 10;
			this._statusStrip.Text = "statusStrip1";
			// 
			// _toolStripStatusLabel
			// 
			this._toolStripStatusLabel.Name = "_toolStripStatusLabel";
			this._toolStripStatusLabel.Size = new System.Drawing.Size(54, 17);
			this._toolStripStatusLabel.Text = "<status>";
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(233, 165);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(75, 23);
			this._buttonOK.TabIndex = 2;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(314, 165);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(75, 23);
			this._buttonCancel.TabIndex = 3;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
			// 
			// ImportQualitySpecificationsForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(401, 220);
			this.Controls.Add(this._groupBoxOptions);
			this.Controls.Add(this._groupBoxExportTo);
			this.Controls.Add(this._statusStrip);
			this.Controls.Add(this._buttonOK);
			this.Controls.Add(this._buttonCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximumSize = new System.Drawing.Size(10000, 254);
			this.MinimumSize = new System.Drawing.Size(330, 254);
			this.Name = "ImportQualitySpecificationsForm";
			this.ShowInTaskbar = false;
			this.Text = "Import Quality Specifications";
			this._groupBoxOptions.ResumeLayout(false);
			this._groupBoxOptions.PerformLayout();
			this._groupBoxExportTo.ResumeLayout(false);
			this._groupBoxExportTo.PerformLayout();
			this._statusStrip.ResumeLayout(false);
			this._statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox _groupBoxOptions;
		private System.Windows.Forms.CheckBox _checkBoxUpdateTestDescriptorNames;
		private System.Windows.Forms.GroupBox _groupBoxExportTo;
		private System.Windows.Forms.Label _labelXmlFile;
		private global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl _fileSystemPathControl;
		private System.Windows.Forms.StatusStrip _statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel _toolStripStatusLabel;
		private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.Button _buttonCancel;
		private System.Windows.Forms.CheckBox _checkBoxIgnoreQualityConditionsForUnknownDatasets;
		private System.Windows.Forms.CheckBox _checkBoxUpdateTestDescriptorProperties;
	}
}