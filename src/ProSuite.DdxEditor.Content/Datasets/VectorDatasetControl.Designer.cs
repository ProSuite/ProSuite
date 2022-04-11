namespace ProSuite.DdxEditor.Content.Datasets
{
    partial class VectorDatasetControl<T>
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
			this._labelMinimumSegmentLength = new System.Windows.Forms.Label();
			this._fileSystemPathControlDefaultSymbology = new global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl();
			this._labelDefaultSymbology = new System.Windows.Forms.Label();
			this._textBoxModelMinimumSegmentLength = new System.Windows.Forms.TextBox();
			this._numericUpDownNullableMinimumSegmentLength = new global::ProSuite.Commons.UI.WinForms.Controls.NumericUpDownNullable();
			this._labelModel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _labelMinimumSegmentLength
			// 
			this._labelMinimumSegmentLength.AutoSize = true;
			this._labelMinimumSegmentLength.Location = new System.Drawing.Point(22, 37);
			this._labelMinimumSegmentLength.Name = "_labelMinimumSegmentLength";
			this._labelMinimumSegmentLength.Size = new System.Drawing.Size(172, 13);
			this._labelMinimumSegmentLength.TabIndex = 1;
			this._labelMinimumSegmentLength.Text = "Dataset Minimum Segment Length:";
			this._labelMinimumSegmentLength.TextAlign = System.Drawing.ContentAlignment.TopRight;
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
			this._fileSystemPathControlDefaultSymbology.Location = new System.Drawing.Point(200, 3);
			this._fileSystemPathControlDefaultSymbology.Name = "_fileSystemPathControlDefaultSymbology";
			this._fileSystemPathControlDefaultSymbology.Size = new System.Drawing.Size(425, 27);
			this._fileSystemPathControlDefaultSymbology.TabIndex = 0;
			// 
			// _labelDefaultSymbology
			// 
			this._labelDefaultSymbology.AutoSize = true;
			this._labelDefaultSymbology.Location = new System.Drawing.Point(96, 9);
			this._labelDefaultSymbology.Name = "_labelDefaultSymbology";
			this._labelDefaultSymbology.Size = new System.Drawing.Size(98, 13);
			this._labelDefaultSymbology.TabIndex = 3;
			this._labelDefaultSymbology.Text = "Default Symbology:";
			this._labelDefaultSymbology.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxModelMinimumSegmentLength
			// 
			this._textBoxModelMinimumSegmentLength.Location = new System.Drawing.Point(458, 34);
			this._textBoxModelMinimumSegmentLength.Name = "_textBoxModelMinimumSegmentLength";
			this._textBoxModelMinimumSegmentLength.ReadOnly = true;
			this._textBoxModelMinimumSegmentLength.Size = new System.Drawing.Size(75, 20);
			this._textBoxModelMinimumSegmentLength.TabIndex = 2;
			// 
			// _numericUpDownNullableMinimumSegmentLength
			// 
			this._numericUpDownNullableMinimumSegmentLength.DecimalPlaces = 2;
			this._numericUpDownNullableMinimumSegmentLength.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
			this._numericUpDownNullableMinimumSegmentLength.Location = new System.Drawing.Point(200, 34);
			this._numericUpDownNullableMinimumSegmentLength.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
			this._numericUpDownNullableMinimumSegmentLength.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
			this._numericUpDownNullableMinimumSegmentLength.Name = "_numericUpDownNullableMinimumSegmentLength";
			this._numericUpDownNullableMinimumSegmentLength.Size = new System.Drawing.Size(152, 20);
			this._numericUpDownNullableMinimumSegmentLength.TabIndex = 1;
			this._numericUpDownNullableMinimumSegmentLength.ThousandsSeparator = false;
			this._numericUpDownNullableMinimumSegmentLength.Value = new decimal(new int[] {
            0,
            0,
            0,
            0});
			// 
			// _labelModel
			// 
			this._labelModel.AutoSize = true;
			this._labelModel.Location = new System.Drawing.Point(376, 37);
			this._labelModel.Name = "_labelModel";
			this._labelModel.Size = new System.Drawing.Size(76, 13);
			this._labelModel.TabIndex = 7;
			this._labelModel.Text = "Model Default:";
			this._labelModel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// VectorDatasetControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._labelModel);
			this.Controls.Add(this._numericUpDownNullableMinimumSegmentLength);
			this.Controls.Add(this._textBoxModelMinimumSegmentLength);
			this.Controls.Add(this._labelDefaultSymbology);
			this.Controls.Add(this._fileSystemPathControlDefaultSymbology);
			this.Controls.Add(this._labelMinimumSegmentLength);
			this.Name = "VectorDatasetControl";
			this.Size = new System.Drawing.Size(647, 65);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelMinimumSegmentLength;
        private global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl _fileSystemPathControlDefaultSymbology;
        private System.Windows.Forms.Label _labelDefaultSymbology;
        private System.Windows.Forms.TextBox _textBoxModelMinimumSegmentLength;
        private global::ProSuite.Commons.UI.WinForms.Controls.NumericUpDownNullable _numericUpDownNullableMinimumSegmentLength;
        private System.Windows.Forms.Label _labelModel;
    }
}
