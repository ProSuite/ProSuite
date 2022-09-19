namespace ProSuite.DdxEditor.Content.Connections
{
    partial class FilePathConnectionProviderControl<T>
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
			this._labelPath = new System.Windows.Forms.Label();
			this._fileSystemPathControlPath = new global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl();
			this.SuspendLayout();
			// 
			// _labelPath
			// 
			this._labelPath.AutoSize = true;
			this._labelPath.Location = new System.Drawing.Point(62, 10);
			this._labelPath.Name = "_labelPath";
			this._labelPath.Size = new System.Drawing.Size(32, 13);
			this._labelPath.TabIndex = 2;
			this._labelPath.Text = "Path:";
			this._labelPath.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _fileSystemPathControlPath
			// 
			this._fileSystemPathControlPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._fileSystemPathControlPath.ControlPathType = global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathType.ChooseFileName;
			this._fileSystemPathControlPath.FileCheckFileExists = true;
			this._fileSystemPathControlPath.FileCheckPathExists = true;
			this._fileSystemPathControlPath.FileDefaultExtension = null;
			this._fileSystemPathControlPath.FileFilter = null;
			this._fileSystemPathControlPath.FolderGroupTitle = "Choose Folder";
			this._fileSystemPathControlPath.FolderShowNewFolderButton = true;
			this._fileSystemPathControlPath.Location = new System.Drawing.Point(100, 4);
			this._fileSystemPathControlPath.Name = "_fileSystemPathControlPath";
			this._fileSystemPathControlPath.Size = new System.Drawing.Size(493, 27);
			this._fileSystemPathControlPath.TabIndex = 3;
			// 
			// FilePathConnectionProviderControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._fileSystemPathControlPath);
			this.Controls.Add(this._labelPath);
			this.Name = "FilePathConnectionProviderControl";
			this.Size = new System.Drawing.Size(600, 33);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelPath;
        private global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl _fileSystemPathControlPath;

    }
}
