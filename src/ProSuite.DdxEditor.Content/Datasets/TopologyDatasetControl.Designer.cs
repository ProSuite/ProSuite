namespace ProSuite.DdxEditor.Content.Datasets
{
    partial class TopologyDatasetControl<T>
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
			this._labelDefaultSymbology = new System.Windows.Forms.Label();
			this._fileSystemPathControlDefaultSymbology = new global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl();
			this.SuspendLayout();
			// 
			// _labelDefaultSymbology
			// 
			this._labelDefaultSymbology.AutoSize = true;
			this._labelDefaultSymbology.Location = new System.Drawing.Point(96, 8);
			this._labelDefaultSymbology.Name = "_labelDefaultSymbology";
			this._labelDefaultSymbology.Size = new System.Drawing.Size(98, 13);
			this._labelDefaultSymbology.TabIndex = 7;
			this._labelDefaultSymbology.Text = "Default Symbology:";
			this._labelDefaultSymbology.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _fileSystemPathControlDefaultSymbology
			// 
			this._fileSystemPathControlDefaultSymbology.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this._fileSystemPathControlDefaultSymbology.ControlPathType = global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathType.ChooseFileName;
			this._fileSystemPathControlDefaultSymbology.FileCheckFileExists = true;
			this._fileSystemPathControlDefaultSymbology.FileCheckPathExists = true;
			this._fileSystemPathControlDefaultSymbology.FileDefaultExtension = null;
			this._fileSystemPathControlDefaultSymbology.FileFilter = null;
			this._fileSystemPathControlDefaultSymbology.FolderGroupTitle = "Choose Folder";
			this._fileSystemPathControlDefaultSymbology.FolderShowNewFolderButton = true;
			this._fileSystemPathControlDefaultSymbology.Location = new System.Drawing.Point(200, 2);
			this._fileSystemPathControlDefaultSymbology.Name = "_fileSystemPathControlDefaultSymbology";
			this._fileSystemPathControlDefaultSymbology.Size = new System.Drawing.Size(376, 27);
			this._fileSystemPathControlDefaultSymbology.TabIndex = 6;
			// 
			// TopologyDatasetControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._labelDefaultSymbology);
			this.Controls.Add(this._fileSystemPathControlDefaultSymbology);
			this.Name = "TopologyDatasetControl";
			this.Size = new System.Drawing.Size(600, 31);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelDefaultSymbology;
        private global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl _fileSystemPathControlDefaultSymbology;
    }
}
