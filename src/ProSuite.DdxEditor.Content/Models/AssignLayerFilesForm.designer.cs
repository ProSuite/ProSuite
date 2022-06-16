namespace ProSuite.DdxEditor.Content.Models
{
	partial class AssignLayerFilesForm
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
			this._labelDirectory = new System.Windows.Forms.Label();
			this._buttonOK = new System.Windows.Forms.Button();
			this._buttonCancel = new System.Windows.Forms.Button();
			this._fileSystemPathControl = new global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl();
			this.SuspendLayout();
			// 
			// _labelDirectory
			// 
			this._labelDirectory.AutoSize = true;
			this._labelDirectory.Location = new System.Drawing.Point(13, 18);
			this._labelDirectory.Name = "_labelDirectory";
			this._labelDirectory.Size = new System.Drawing.Size(120, 13);
			this._labelDirectory.TabIndex = 1;
			this._labelDirectory.Text = "Directory with layer files:";
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(456, 51);
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
			this._buttonCancel.Location = new System.Drawing.Point(537, 51);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(75, 23);
			this._buttonCancel.TabIndex = 3;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
			// 
			// _fileSystemPathControl
			// 
			this._fileSystemPathControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._fileSystemPathControl.ControlPathType = global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathType.ChooseFolder;
			this._fileSystemPathControl.FileCheckFileExists = false;
			this._fileSystemPathControl.FileCheckPathExists = true;
			this._fileSystemPathControl.FileDefaultExtension = null;
			this._fileSystemPathControl.FileFilter = null;
			this._fileSystemPathControl.FolderGroupTitle = "Choose Folder";
			this._fileSystemPathControl.FolderShowNewFolderButton = false;
			this._fileSystemPathControl.Location = new System.Drawing.Point(139, 12);
			this._fileSystemPathControl.Name = "_fileSystemPathControl";
			this._fileSystemPathControl.Size = new System.Drawing.Size(473, 26);
			this._fileSystemPathControl.TabIndex = 6;
			this._fileSystemPathControl.ValueChanged += new System.EventHandler(this._fileSystemPathControl_ValueChanged);
			// 
			// AssignLayerFilesForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(624, 86);
			this.Controls.Add(this._fileSystemPathControl);
			this.Controls.Add(this._buttonCancel);
			this.Controls.Add(this._buttonOK);
			this.Controls.Add(this._labelDirectory);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(1400, 120);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(300, 120);
			this.Name = "AssignLayerFilesForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "Assign Missing Layer Files";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label _labelDirectory;
		private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.Button _buttonCancel;
		private global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl _fileSystemPathControl;
	}
}