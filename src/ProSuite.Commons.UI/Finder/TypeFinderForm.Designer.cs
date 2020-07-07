using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.Finder
{
    partial class TypeFinderForm
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
            this.components = new System.ComponentModel.Container();
            this._statusStrip = new System.Windows.Forms.StatusStrip();
            this._toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this._labelAssembly = new System.Windows.Forms.Label();
            this._groupBoxTypes = new System.Windows.Forms.GroupBox();
            this._dataGridView = new DoubleBufferedDataGridView();
            this._columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._columnNamespace = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._buttonOK = new System.Windows.Forms.Button();
            this._buttonCancel = new System.Windows.Forms.Button();
            this._errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this._buttonSelectAll = new System.Windows.Forms.Button();
            this._buttonSelectNone = new System.Windows.Forms.Button();
            this._fileSystemPathAssembly = new FileSystemPathControl();
            this._dataGridViewFindToolStrip = new DataGridViewFindToolStrip();
            this._statusStrip.SuspendLayout();
            this._groupBoxTypes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // _statusStrip
            // 
            this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripStatusLabel});
            this._statusStrip.Location = new System.Drawing.Point(0, 424);
            this._statusStrip.Name = "_statusStrip";
            this._statusStrip.Size = new System.Drawing.Size(511, 22);
            this._statusStrip.TabIndex = 0;
            // 
            // _toolStripStatusLabel
            // 
            this._toolStripStatusLabel.Name = "_toolStripStatusLabel";
            this._toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // _labelAssembly
            // 
            this._labelAssembly.AutoSize = true;
            this._labelAssembly.Location = new System.Drawing.Point(12, 19);
            this._labelAssembly.Name = "_labelAssembly";
            this._labelAssembly.Size = new System.Drawing.Size(51, 13);
            this._labelAssembly.TabIndex = 0;
            this._labelAssembly.Text = "Assembly";
            // 
            // _groupBoxTypes
            // 
            this._groupBoxTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._groupBoxTypes.Controls.Add(this._dataGridView);
            this._groupBoxTypes.Controls.Add(this._dataGridViewFindToolStrip);
            this._groupBoxTypes.Location = new System.Drawing.Point(13, 45);
            this._groupBoxTypes.Name = "_groupBoxTypes";
            this._groupBoxTypes.Size = new System.Drawing.Size(486, 338);
            this._groupBoxTypes.TabIndex = 1;
            this._groupBoxTypes.TabStop = false;
            this._groupBoxTypes.Text = "Types";
            // 
            // _dataGridView
            // 
            this._dataGridView.AllowUserToAddRows = false;
            this._dataGridView.AllowUserToDeleteRows = false;
            this._dataGridView.AllowUserToResizeRows = false;
            this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnName,
            this._columnNamespace});
            this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dataGridView.Location = new System.Drawing.Point(3, 41);
            this._dataGridView.Name = "_dataGridView";
            this._dataGridView.ReadOnly = true;
            this._dataGridView.RowHeadersVisible = false;
            this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dataGridView.Size = new System.Drawing.Size(480, 294);
            this._dataGridView.TabIndex = 0;
            this._dataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridView_CellDoubleClick);
            this._dataGridView.SelectionChanged += new System.EventHandler(this._dataGridView_SelectionChanged);
            // 
            // _columnName
            // 
            this._columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this._columnName.DataPropertyName = "Name";
            this._columnName.HeaderText = "Name";
            this._columnName.Name = "_columnName";
            this._columnName.ReadOnly = true;
            this._columnName.Width = 60;
            // 
            // _columnNamespace
            // 
            this._columnNamespace.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this._columnNamespace.DataPropertyName = "Namespace";
            this._columnNamespace.HeaderText = "Namespace";
            this._columnNamespace.Name = "_columnNamespace";
            this._columnNamespace.ReadOnly = true;
            // 
            // _buttonOK
            // 
            this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonOK.Location = new System.Drawing.Point(343, 389);
            this._buttonOK.Name = "_buttonOK";
            this._buttonOK.Size = new System.Drawing.Size(75, 23);
            this._buttonOK.TabIndex = 4;
            this._buttonOK.Text = "OK";
            this._buttonOK.UseVisualStyleBackColor = true;
            this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
            // 
            // _buttonCancel
            // 
            this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._buttonCancel.Location = new System.Drawing.Point(424, 389);
            this._buttonCancel.Name = "_buttonCancel";
            this._buttonCancel.Size = new System.Drawing.Size(75, 23);
            this._buttonCancel.TabIndex = 5;
            this._buttonCancel.Text = "Cancel";
            this._buttonCancel.UseVisualStyleBackColor = true;
            // 
            // _errorProvider
            // 
            this._errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this._errorProvider.ContainerControl = this;
            // 
            // _buttonSelectAll
            // 
            this._buttonSelectAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._buttonSelectAll.Location = new System.Drawing.Point(13, 388);
            this._buttonSelectAll.Name = "_buttonSelectAll";
            this._buttonSelectAll.Size = new System.Drawing.Size(75, 23);
            this._buttonSelectAll.TabIndex = 2;
            this._buttonSelectAll.Text = "Select All";
            this._buttonSelectAll.UseVisualStyleBackColor = true;
            this._buttonSelectAll.Click += new System.EventHandler(this._buttonSelectAll_Click);
            // 
            // _buttonSelectNone
            // 
            this._buttonSelectNone.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._buttonSelectNone.Location = new System.Drawing.Point(94, 388);
            this._buttonSelectNone.Name = "_buttonSelectNone";
            this._buttonSelectNone.Size = new System.Drawing.Size(75, 23);
            this._buttonSelectNone.TabIndex = 3;
            this._buttonSelectNone.Text = "Select None";
            this._buttonSelectNone.UseVisualStyleBackColor = true;
            this._buttonSelectNone.Click += new System.EventHandler(this._buttonSelectNone_Click);
            // 
            // _fileSystemPathAssembly
            // 
            this._fileSystemPathAssembly.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._fileSystemPathAssembly.ControlPathType = FileSystemPathType.ChooseFileName;
            this._fileSystemPathAssembly.FileCheckFileExists = true;
            this._fileSystemPathAssembly.FileCheckPathExists = true;
            this._fileSystemPathAssembly.FileDefaultExtension = "dll";
            this._fileSystemPathAssembly.FileFilter = "Dlls (*.dll)|*.dll|Executables (*.exe) |*.exe";
            this._fileSystemPathAssembly.FolderGroupTitle = "Choose Folder";
            this._fileSystemPathAssembly.FolderShowNewFolderButton = true;
            this._fileSystemPathAssembly.Location = new System.Drawing.Point(69, 12);
            this._fileSystemPathAssembly.Name = "_fileSystemPathAssembly";
            this._fileSystemPathAssembly.Size = new System.Drawing.Size(424, 27);
            this._fileSystemPathAssembly.TabIndex = 0;
            this._fileSystemPathAssembly.ValueChanged += new System.EventHandler(this._fileSystemPathAssembly_ValueChanged);
            // 
            // _dataGridViewFindToolStrip
            // 
            this._dataGridViewFindToolStrip.ClickThrough = true;
            this._dataGridViewFindToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(3, 16);
            this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
            this._dataGridViewFindToolStrip.Observer = null;
            this._dataGridViewFindToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(480, 25);
            this._dataGridViewFindToolStrip.TabIndex = 1;
            this._dataGridViewFindToolStrip.Text = "dataGridViewFindToolStrip1";
            // 
            // TypeFinderForm
            // 
            this.AcceptButton = this._buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._buttonCancel;
            this.ClientSize = new System.Drawing.Size(511, 446);
            this.Controls.Add(this._buttonSelectNone);
            this.Controls.Add(this._buttonSelectAll);
            this.Controls.Add(this._buttonCancel);
            this.Controls.Add(this._buttonOK);
            this.Controls.Add(this._groupBoxTypes);
            this.Controls.Add(this._labelAssembly);
            this.Controls.Add(this._fileSystemPathAssembly);
            this.Controls.Add(this._statusStrip);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(380, 280);
            this.Name = "TypeFinderForm";
            this.ShowInTaskbar = false;
            this.Text = "Type Finder";
            this.Load += new System.EventHandler(this.TypeFinderForm_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TypeFinderForm_FormClosed);
            this._statusStrip.ResumeLayout(false);
            this._statusStrip.PerformLayout();
            this._groupBoxTypes.ResumeLayout(false);
            this._groupBoxTypes.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _toolStripStatusLabel;
        private FileSystemPathControl _fileSystemPathAssembly;
        private System.Windows.Forms.Label _labelAssembly;
        private System.Windows.Forms.GroupBox _groupBoxTypes;
        private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.Button _buttonCancel;
        private System.Windows.Forms.ErrorProvider _errorProvider;
        private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
        private System.Windows.Forms.DataGridViewTextBoxColumn _columnNamespace;
        private System.Windows.Forms.Button _buttonSelectNone;
        private System.Windows.Forms.Button _buttonSelectAll;
		private DoubleBufferedDataGridView _dataGridView;
        private DataGridViewFindToolStrip _dataGridViewFindToolStrip;
    }
}