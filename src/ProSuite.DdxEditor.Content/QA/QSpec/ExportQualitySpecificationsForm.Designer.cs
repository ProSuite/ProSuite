using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	partial class ExportQualitySpecificationsForm
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
			this._buttonCancel = new System.Windows.Forms.Button();
			this._buttonOK = new System.Windows.Forms.Button();
			this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this._groupBoxQualitySpecifications = new System.Windows.Forms.GroupBox();
			this._dataGridView = new ProSuite.Commons.UI.WinForms.Controls.DoubleBufferedDataGridView();
			this._columnSelected = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this._columnImage = new System.Windows.Forms.DataGridViewImageColumn();
			this._columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnCreated = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnCreatedBy = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnLastChanged = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._columnLastChangedBy = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this._dataGridViewFindToolStrip = new ProSuite.Commons.UI.WinForms.Controls.DataGridViewFindToolStrip();
			this.toolStrip1 = new ProSuite.Commons.UI.WinForms.Controls.ToolStripEx();
			this._toolStripButtonSelectAll = new System.Windows.Forms.ToolStripButton();
			this._toolStripButtonSelectNone = new System.Windows.Forms.ToolStripButton();
			this._groupBoxExportTo = new System.Windows.Forms.GroupBox();
			this._labelDirectory = new System.Windows.Forms.Label();
			this._labelFile = new System.Windows.Forms.Label();
			this._radioButtonDirectory = new System.Windows.Forms.RadioButton();
			this._radioButtonSingleFile = new System.Windows.Forms.RadioButton();
			this._fileSystemPathControlDirectory = new ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl();
			this._fileSystemPathControlSingleFile = new ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl();
			this._groupBoxOptions = new System.Windows.Forms.GroupBox();
			this._groupBoxCategories = new System.Windows.Forms.GroupBox();
			this._radioButtonExportReferencedCategories = new System.Windows.Forms.RadioButton();
			this._radioButtonExportAllCategories = new System.Windows.Forms.RadioButton();
			this._groupBoxDescriptors = new System.Windows.Forms.GroupBox();
			this._radioButtonExportReferencedDescriptors = new System.Windows.Forms.RadioButton();
			this._radioButtonExportAllDescriptors = new System.Windows.Forms.RadioButton();
			this._checkBoxExportNotes = new System.Windows.Forms.CheckBox();
			this._checkBoxExportConnectionFilePaths = new System.Windows.Forms.CheckBox();
			this._checkBoxExportWorkspaceConnections = new System.Windows.Forms.CheckBox();
			this._checkBoxExportMetadata = new System.Windows.Forms.CheckBox();
			this._errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
			this._statusStrip.SuspendLayout();
			this._groupBoxQualitySpecifications.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
			this.toolStrip1.SuspendLayout();
			this._groupBoxExportTo.SuspendLayout();
			this._groupBoxOptions.SuspendLayout();
			this._groupBoxCategories.SuspendLayout();
			this._groupBoxDescriptors.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).BeginInit();
			this.SuspendLayout();
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(603, 741);
			this._buttonCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(88, 27);
			this._buttonCancel.TabIndex = 4;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(509, 741);
			this._buttonOK.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(88, 27);
			this._buttonOK.TabIndex = 3;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// _statusStrip
			// 
			this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripStatusLabel});
			this._statusStrip.Location = new System.Drawing.Point(0, 780);
			this._statusStrip.Name = "_statusStrip";
			this._statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
			this._statusStrip.Size = new System.Drawing.Size(705, 22);
			this._statusStrip.TabIndex = 2;
			this._statusStrip.Text = "statusStrip1";
			// 
			// _toolStripStatusLabel
			// 
			this._toolStripStatusLabel.Name = "_toolStripStatusLabel";
			this._toolStripStatusLabel.Size = new System.Drawing.Size(54, 17);
			this._toolStripStatusLabel.Text = "<status>";
			// 
			// _groupBoxQualitySpecifications
			// 
			this._groupBoxQualitySpecifications.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxQualitySpecifications.Controls.Add(this._dataGridView);
			this._groupBoxQualitySpecifications.Controls.Add(this._dataGridViewFindToolStrip);
			this._groupBoxQualitySpecifications.Controls.Add(this.toolStrip1);
			this._groupBoxQualitySpecifications.Location = new System.Drawing.Point(14, 181);
			this._groupBoxQualitySpecifications.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxQualitySpecifications.Name = "_groupBoxQualitySpecifications";
			this._groupBoxQualitySpecifications.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxQualitySpecifications.Size = new System.Drawing.Size(677, 242);
			this._groupBoxQualitySpecifications.TabIndex = 1;
			this._groupBoxQualitySpecifications.TabStop = false;
			this._groupBoxQualitySpecifications.Text = "Quality specifications";
			// 
			// _dataGridView
			// 
			this._dataGridView.AllowUserToAddRows = false;
			this._dataGridView.AllowUserToDeleteRows = false;
			this._dataGridView.AllowUserToResizeRows = false;
			this._dataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this._dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._columnSelected,
            this._columnImage,
            this._columnName,
            this._columnCategory,
            this._columnDescription,
            this._columnCreated,
            this._columnCreatedBy,
            this._columnLastChanged,
            this._columnLastChangedBy});
			this._dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._dataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this._dataGridView.Location = new System.Drawing.Point(4, 75);
			this._dataGridView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._dataGridView.Name = "_dataGridView";
			this._dataGridView.RowHeadersVisible = false;
			this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this._dataGridView.Size = new System.Drawing.Size(669, 164);
			this._dataGridView.TabIndex = 2;
			this._dataGridView.FilteredRowsChanged += new System.EventHandler(this._dataGridView_FilteredRowsChanged);
			this._dataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this._dataGridView_CellValueChanged);
			this._dataGridView.CurrentCellDirtyStateChanged += new System.EventHandler(this._dataGridView_CurrentCellDirtyStateChanged);
			// 
			// _columnSelected
			// 
			this._columnSelected.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
			this._columnSelected.DataPropertyName = "Selected";
			this._columnSelected.HeaderText = "";
			this._columnSelected.MinimumWidth = 20;
			this._columnSelected.Name = "_columnSelected";
			this._columnSelected.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnSelected.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnSelected.Width = 20;
			// 
			// _columnImage
			// 
			this._columnImage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnImage.DataPropertyName = "Image";
			this._columnImage.HeaderText = "";
			this._columnImage.MinimumWidth = 20;
			this._columnImage.Name = "_columnImage";
			this._columnImage.ReadOnly = true;
			this._columnImage.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this._columnImage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this._columnImage.Width = 20;
			// 
			// _columnName
			// 
			this._columnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnName.DataPropertyName = "Name";
			this._columnName.HeaderText = "Name";
			this._columnName.MinimumWidth = 40;
			this._columnName.Name = "_columnName";
			this._columnName.ReadOnly = true;
			this._columnName.Width = 64;
			// 
			// _columnCategory
			// 
			this._columnCategory.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnCategory.DataPropertyName = "Category";
			this._columnCategory.HeaderText = "Category";
			this._columnCategory.MinimumWidth = 80;
			this._columnCategory.Name = "_columnCategory";
			this._columnCategory.ReadOnly = true;
			this._columnCategory.Width = 80;
			// 
			// _columnDescription
			// 
			this._columnDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this._columnDescription.DataPropertyName = "Description";
			this._columnDescription.HeaderText = "Description";
			this._columnDescription.MinimumWidth = 100;
			this._columnDescription.Name = "_columnDescription";
			this._columnDescription.ReadOnly = true;
			// 
			// _columnCreated
			// 
			this._columnCreated.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnCreated.DataPropertyName = "CreatedDate";
			this._columnCreated.HeaderText = "Created";
			this._columnCreated.MinimumWidth = 30;
			this._columnCreated.Name = "_columnCreated";
			this._columnCreated.Width = 73;
			// 
			// _columnCreatedBy
			// 
			this._columnCreatedBy.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnCreatedBy.DataPropertyName = "CreatedByUser";
			this._columnCreatedBy.HeaderText = "By";
			this._columnCreatedBy.MinimumWidth = 30;
			this._columnCreatedBy.Name = "_columnCreatedBy";
			this._columnCreatedBy.Width = 45;
			// 
			// _columnLastChanged
			// 
			this._columnLastChanged.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnLastChanged.DataPropertyName = "LastChangedDate";
			this._columnLastChanged.HeaderText = "Changed";
			this._columnLastChanged.MinimumWidth = 30;
			this._columnLastChanged.Name = "_columnLastChanged";
			this._columnLastChanged.Width = 80;
			// 
			// _columnLastChangedBy
			// 
			this._columnLastChangedBy.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this._columnLastChangedBy.DataPropertyName = "LastChangedByUser";
			this._columnLastChangedBy.HeaderText = "By";
			this._columnLastChangedBy.MinimumWidth = 30;
			this._columnLastChangedBy.Name = "_columnLastChangedBy";
			this._columnLastChangedBy.Width = 45;
			// 
			// _dataGridViewFindToolStrip
			// 
			this._dataGridViewFindToolStrip.AutoSize = false;
			this._dataGridViewFindToolStrip.ClickThrough = true;
			this._dataGridViewFindToolStrip.FilterRows = false;
			this._dataGridViewFindToolStrip.FindText = "";
			this._dataGridViewFindToolStrip.FindTextBoxWidth = 174;
			this._dataGridViewFindToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._dataGridViewFindToolStrip.Location = new System.Drawing.Point(4, 44);
			this._dataGridViewFindToolStrip.MatchCase = false;
			this._dataGridViewFindToolStrip.Name = "_dataGridViewFindToolStrip";
			this._dataGridViewFindToolStrip.Observer = null;
			this._dataGridViewFindToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._dataGridViewFindToolStrip.Size = new System.Drawing.Size(669, 31);
			this._dataGridViewFindToolStrip.TabIndex = 1;
			this._dataGridViewFindToolStrip.Text = "dataGridViewFindToolStrip1";
			// 
			// toolStrip1
			// 
			this.toolStrip1.ClickThrough = true;
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripButtonSelectAll,
            this._toolStripButtonSelectNone});
			this.toolStrip1.Location = new System.Drawing.Point(4, 19);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip1.Size = new System.Drawing.Size(669, 25);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// _toolStripButtonSelectAll
			// 
			this._toolStripButtonSelectAll.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.SelectAll;
			this._toolStripButtonSelectAll.Name = "_toolStripButtonSelectAll";
			this._toolStripButtonSelectAll.Size = new System.Drawing.Size(73, 22);
			this._toolStripButtonSelectAll.Text = "Select all";
			this._toolStripButtonSelectAll.Click += new System.EventHandler(this._toolStripButtonSelectAll_Click);
			// 
			// _toolStripButtonSelectNone
			// 
			this._toolStripButtonSelectNone.Image = global::ProSuite.DdxEditor.Content.Properties.Resources.SelectNone;
			this._toolStripButtonSelectNone.Name = "_toolStripButtonSelectNone";
			this._toolStripButtonSelectNone.Size = new System.Drawing.Size(88, 22);
			this._toolStripButtonSelectNone.Text = "Select none";
			this._toolStripButtonSelectNone.Click += new System.EventHandler(this._toolStripButtonSelectNone_Click);
			// 
			// _groupBoxExportTo
			// 
			this._groupBoxExportTo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxExportTo.Controls.Add(this._labelDirectory);
			this._groupBoxExportTo.Controls.Add(this._labelFile);
			this._groupBoxExportTo.Controls.Add(this._radioButtonDirectory);
			this._groupBoxExportTo.Controls.Add(this._radioButtonSingleFile);
			this._groupBoxExportTo.Controls.Add(this._fileSystemPathControlDirectory);
			this._groupBoxExportTo.Controls.Add(this._fileSystemPathControlSingleFile);
			this._groupBoxExportTo.Location = new System.Drawing.Point(14, 14);
			this._groupBoxExportTo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxExportTo.Name = "_groupBoxExportTo";
			this._groupBoxExportTo.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxExportTo.Size = new System.Drawing.Size(677, 160);
			this._groupBoxExportTo.TabIndex = 0;
			this._groupBoxExportTo.TabStop = false;
			this._groupBoxExportTo.Text = "Export to";
			// 
			// _labelDirectory
			// 
			this._labelDirectory.AutoSize = true;
			this._labelDirectory.Location = new System.Drawing.Point(38, 120);
			this._labelDirectory.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelDirectory.Name = "_labelDirectory";
			this._labelDirectory.Size = new System.Drawing.Size(58, 15);
			this._labelDirectory.TabIndex = 8;
			this._labelDirectory.Text = "Directory:";
			// 
			// _labelFile
			// 
			this._labelFile.AutoSize = true;
			this._labelFile.Location = new System.Drawing.Point(69, 57);
			this._labelFile.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelFile.Name = "_labelFile";
			this._labelFile.Size = new System.Drawing.Size(28, 15);
			this._labelFile.TabIndex = 8;
			this._labelFile.Text = "File:";
			// 
			// _radioButtonDirectory
			// 
			this._radioButtonDirectory.AutoSize = true;
			this._radioButtonDirectory.Location = new System.Drawing.Point(19, 91);
			this._radioButtonDirectory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._radioButtonDirectory.Name = "_radioButtonDirectory";
			this._radioButtonDirectory.Size = new System.Drawing.Size(319, 19);
			this._radioButtonDirectory.TabIndex = 2;
			this._radioButtonDirectory.TabStop = true;
			this._radioButtonDirectory.Text = "directory with one file per exported quality specification";
			this._radioButtonDirectory.UseVisualStyleBackColor = true;
			// 
			// _radioButtonSingleFile
			// 
			this._radioButtonSingleFile.AutoSize = true;
			this._radioButtonSingleFile.Checked = true;
			this._radioButtonSingleFile.Location = new System.Drawing.Point(19, 28);
			this._radioButtonSingleFile.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._radioButtonSingleFile.Name = "_radioButtonSingleFile";
			this._radioButtonSingleFile.Size = new System.Drawing.Size(314, 19);
			this._radioButtonSingleFile.TabIndex = 0;
			this._radioButtonSingleFile.TabStop = true;
			this._radioButtonSingleFile.Text = "single file containing all exported quality specifications";
			this._radioButtonSingleFile.UseVisualStyleBackColor = true;
			this._radioButtonSingleFile.CheckedChanged += new System.EventHandler(this._radioButtonSingleFile_CheckedChanged);
			// 
			// _fileSystemPathControlDirectory
			// 
			this._fileSystemPathControlDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._fileSystemPathControlDirectory.ControlPathType = ProSuite.Commons.UI.WinForms.Controls.FileSystemPathType.ChooseFolder;
			this._fileSystemPathControlDirectory.FileCheckFileExists = false;
			this._fileSystemPathControlDirectory.FileCheckPathExists = true;
			this._fileSystemPathControlDirectory.FileDefaultExtension = null;
			this._fileSystemPathControlDirectory.FileFilter = null;
			this._fileSystemPathControlDirectory.FolderGroupTitle = "Choose Folder";
			this._fileSystemPathControlDirectory.FolderShowNewFolderButton = true;
			this._fileSystemPathControlDirectory.Location = new System.Drawing.Point(106, 114);
			this._fileSystemPathControlDirectory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._fileSystemPathControlDirectory.Name = "_fileSystemPathControlDirectory";
			this._fileSystemPathControlDirectory.Size = new System.Drawing.Size(544, 30);
			this._fileSystemPathControlDirectory.TabIndex = 3;
			this._fileSystemPathControlDirectory.ValueChanged += new System.EventHandler(this._fileSystemPathControlDirectory_ValueChanged);
			this._fileSystemPathControlDirectory.LeaveTextBox += new System.EventHandler(this._fileSystemPathControlDirectory_LeaveTextBox);
			// 
			// _fileSystemPathControlSingleFile
			// 
			this._fileSystemPathControlSingleFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._fileSystemPathControlSingleFile.ControlPathType = ProSuite.Commons.UI.WinForms.Controls.FileSystemPathType.ChooseFileName;
			this._fileSystemPathControlSingleFile.FileCheckFileExists = false;
			this._fileSystemPathControlSingleFile.FileCheckPathExists = true;
			this._fileSystemPathControlSingleFile.FileDefaultExtension = null;
			this._fileSystemPathControlSingleFile.FileFilter = null;
			this._fileSystemPathControlSingleFile.FolderGroupTitle = "Choose Folder";
			this._fileSystemPathControlSingleFile.FolderShowNewFolderButton = true;
			this._fileSystemPathControlSingleFile.Location = new System.Drawing.Point(106, 51);
			this._fileSystemPathControlSingleFile.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._fileSystemPathControlSingleFile.Name = "_fileSystemPathControlSingleFile";
			this._fileSystemPathControlSingleFile.Size = new System.Drawing.Size(544, 30);
			this._fileSystemPathControlSingleFile.TabIndex = 1;
			this._fileSystemPathControlSingleFile.ValueChanged += new System.EventHandler(this._fileSystemPathControlFile_ValueChanged);
			this._fileSystemPathControlSingleFile.LeaveTextBox += new System.EventHandler(this._fileSystemPathControlFile_LeaveTextBox);
			// 
			// _groupBoxOptions
			// 
			this._groupBoxOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxOptions.Controls.Add(this._groupBoxCategories);
			this._groupBoxOptions.Controls.Add(this._groupBoxDescriptors);
			this._groupBoxOptions.Controls.Add(this._checkBoxExportNotes);
			this._groupBoxOptions.Controls.Add(this._checkBoxExportConnectionFilePaths);
			this._groupBoxOptions.Controls.Add(this._checkBoxExportWorkspaceConnections);
			this._groupBoxOptions.Controls.Add(this._checkBoxExportMetadata);
			this._groupBoxOptions.Location = new System.Drawing.Point(15, 430);
			this._groupBoxOptions.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxOptions.Name = "_groupBoxOptions";
			this._groupBoxOptions.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxOptions.Size = new System.Drawing.Size(676, 303);
			this._groupBoxOptions.TabIndex = 2;
			this._groupBoxOptions.TabStop = false;
			this._groupBoxOptions.Text = "Options";
			// 
			// _groupBoxCategories
			// 
			this._groupBoxCategories.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxCategories.Controls.Add(this._radioButtonExportReferencedCategories);
			this._groupBoxCategories.Controls.Add(this._radioButtonExportAllCategories);
			this._groupBoxCategories.Location = new System.Drawing.Point(7, 216);
			this._groupBoxCategories.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxCategories.Name = "_groupBoxCategories";
			this._groupBoxCategories.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxCategories.Size = new System.Drawing.Size(662, 80);
			this._groupBoxCategories.TabIndex = 5;
			this._groupBoxCategories.TabStop = false;
			this._groupBoxCategories.Text = "Data quality categories";
			// 
			// _radioButtonExportReferencedCategories
			// 
			this._radioButtonExportReferencedCategories.AutoSize = true;
			this._radioButtonExportReferencedCategories.Checked = true;
			this._radioButtonExportReferencedCategories.Location = new System.Drawing.Point(10, 22);
			this._radioButtonExportReferencedCategories.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._radioButtonExportReferencedCategories.Name = "_radioButtonExportReferencedCategories";
			this._radioButtonExportReferencedCategories.Size = new System.Drawing.Size(221, 19);
			this._radioButtonExportReferencedCategories.TabIndex = 0;
			this._radioButtonExportReferencedCategories.TabStop = true;
			this._radioButtonExportReferencedCategories.Text = "Export only the referenced categories";
			this._radioButtonExportReferencedCategories.UseVisualStyleBackColor = true;
			// 
			// _radioButtonExportAllCategories
			// 
			this._radioButtonExportAllCategories.AutoSize = true;
			this._radioButtonExportAllCategories.Location = new System.Drawing.Point(10, 48);
			this._radioButtonExportAllCategories.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._radioButtonExportAllCategories.Name = "_radioButtonExportAllCategories";
			this._radioButtonExportAllCategories.Size = new System.Drawing.Size(131, 19);
			this._radioButtonExportAllCategories.TabIndex = 1;
			this._radioButtonExportAllCategories.Text = "Export all categories";
			this._radioButtonExportAllCategories.UseVisualStyleBackColor = true;
			// 
			// _groupBoxDescriptors
			// 
			this._groupBoxDescriptors.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxDescriptors.Controls.Add(this._radioButtonExportReferencedDescriptors);
			this._groupBoxDescriptors.Controls.Add(this._radioButtonExportAllDescriptors);
			this._groupBoxDescriptors.Location = new System.Drawing.Point(7, 129);
			this._groupBoxDescriptors.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxDescriptors.Name = "_groupBoxDescriptors";
			this._groupBoxDescriptors.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxDescriptors.Size = new System.Drawing.Size(662, 80);
			this._groupBoxDescriptors.TabIndex = 4;
			this._groupBoxDescriptors.TabStop = false;
			this._groupBoxDescriptors.Text = "Algorithm descriptors";
			// 
			// _radioButtonExportReferencedDescriptors
			// 
			this._radioButtonExportReferencedDescriptors.AutoSize = true;
			this._radioButtonExportReferencedDescriptors.Checked = true;
			this._radioButtonExportReferencedDescriptors.Location = new System.Drawing.Point(10, 22);
			this._radioButtonExportReferencedDescriptors.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._radioButtonExportReferencedDescriptors.Name = "_radioButtonExportReferencedDescriptors";
			this._radioButtonExportReferencedDescriptors.Size = new System.Drawing.Size(225, 19);
			this._radioButtonExportReferencedDescriptors.TabIndex = 0;
			this._radioButtonExportReferencedDescriptors.TabStop = true;
			this._radioButtonExportReferencedDescriptors.Text = "Export only the referenced descriptors";
			this._radioButtonExportReferencedDescriptors.UseVisualStyleBackColor = true;
			// 
			// _radioButtonExportAllDescriptors
			// 
			this._radioButtonExportAllDescriptors.AutoSize = true;
			this._radioButtonExportAllDescriptors.Location = new System.Drawing.Point(10, 48);
			this._radioButtonExportAllDescriptors.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._radioButtonExportAllDescriptors.Name = "_radioButtonExportAllDescriptors";
			this._radioButtonExportAllDescriptors.Size = new System.Drawing.Size(135, 19);
			this._radioButtonExportAllDescriptors.TabIndex = 1;
			this._radioButtonExportAllDescriptors.Text = "Export all descriptors";
			this._radioButtonExportAllDescriptors.UseVisualStyleBackColor = true;
			// 
			// _checkBoxExportNotes
			// 
			this._checkBoxExportNotes.AutoSize = true;
			this._checkBoxExportNotes.Location = new System.Drawing.Point(18, 103);
			this._checkBoxExportNotes.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxExportNotes.Name = "_checkBoxExportNotes";
			this._checkBoxExportNotes.Size = new System.Drawing.Size(92, 19);
			this._checkBoxExportNotes.TabIndex = 3;
			this._checkBoxExportNotes.Text = "Export notes";
			this._checkBoxExportNotes.UseVisualStyleBackColor = true;
			// 
			// _checkBoxExportConnectionFilePaths
			// 
			this._checkBoxExportConnectionFilePaths.AutoSize = true;
			this._checkBoxExportConnectionFilePaths.Location = new System.Drawing.Point(41, 76);
			this._checkBoxExportConnectionFilePaths.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxExportConnectionFilePaths.Name = "_checkBoxExportConnectionFilePaths";
			this._checkBoxExportConnectionFilePaths.Size = new System.Drawing.Size(357, 19);
			this._checkBoxExportConnectionFilePaths.TabIndex = 2;
			this._checkBoxExportConnectionFilePaths.Text = "Use file paths to connection files (ArcSDE: *.sde; OLE DB: *.odc)";
			this._checkBoxExportConnectionFilePaths.UseVisualStyleBackColor = true;
			// 
			// _checkBoxExportWorkspaceConnections
			// 
			this._checkBoxExportWorkspaceConnections.AutoSize = true;
			this._checkBoxExportWorkspaceConnections.Location = new System.Drawing.Point(18, 50);
			this._checkBoxExportWorkspaceConnections.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxExportWorkspaceConnections.Name = "_checkBoxExportWorkspaceConnections";
			this._checkBoxExportWorkspaceConnections.Size = new System.Drawing.Size(248, 19);
			this._checkBoxExportWorkspaceConnections.TabIndex = 1;
			this._checkBoxExportWorkspaceConnections.Text = "Export workspace connection information";
			this._checkBoxExportWorkspaceConnections.UseVisualStyleBackColor = true;
			this._checkBoxExportWorkspaceConnections.CheckedChanged += new System.EventHandler(this._checkBoxExportWorkspaceConnections_CheckedChanged);
			// 
			// _checkBoxExportMetadata
			// 
			this._checkBoxExportMetadata.AutoSize = true;
			this._checkBoxExportMetadata.Location = new System.Drawing.Point(18, 23);
			this._checkBoxExportMetadata.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxExportMetadata.Name = "_checkBoxExportMetadata";
			this._checkBoxExportMetadata.Size = new System.Drawing.Size(329, 19);
			this._checkBoxExportMetadata.TabIndex = 0;
			this._checkBoxExportMetadata.Text = "Export metadata (User and Date of Creation/Last Change)";
			this._checkBoxExportMetadata.UseVisualStyleBackColor = true;
			// 
			// _errorProvider
			// 
			this._errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
			this._errorProvider.ContainerControl = this;
			// 
			// ExportQualitySpecificationsForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(705, 802);
			this.Controls.Add(this._groupBoxOptions);
			this.Controls.Add(this._groupBoxExportTo);
			this.Controls.Add(this._groupBoxQualitySpecifications);
			this.Controls.Add(this._statusStrip);
			this.Controls.Add(this._buttonOK);
			this.Controls.Add(this._buttonCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(569, 744);
			this.Name = "ExportQualitySpecificationsForm";
			this.ShowInTaskbar = false;
			this.Text = "Export Quality Specifications";
			this.Load += new System.EventHandler(this.ExportQualitySpecificationsForm_Load);
			this._statusStrip.ResumeLayout(false);
			this._statusStrip.PerformLayout();
			this._groupBoxQualitySpecifications.ResumeLayout(false);
			this._groupBoxQualitySpecifications.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this._groupBoxExportTo.ResumeLayout(false);
			this._groupBoxExportTo.PerformLayout();
			this._groupBoxOptions.ResumeLayout(false);
			this._groupBoxOptions.PerformLayout();
			this._groupBoxCategories.ResumeLayout(false);
			this._groupBoxCategories.PerformLayout();
			this._groupBoxDescriptors.ResumeLayout(false);
			this._groupBoxDescriptors.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._errorProvider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button _buttonCancel;
		private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.StatusStrip _statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel _toolStripStatusLabel;
		private System.Windows.Forms.GroupBox _groupBoxQualitySpecifications;
		private System.Windows.Forms.ToolStripButton _toolStripButtonSelectAll;
		private System.Windows.Forms.ToolStripButton _toolStripButtonSelectNone;
		private global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl _fileSystemPathControlSingleFile;
		private System.Windows.Forms.GroupBox _groupBoxExportTo;
		private System.Windows.Forms.GroupBox _groupBoxOptions;
		private System.Windows.Forms.CheckBox _checkBoxExportMetadata;
		private System.Windows.Forms.CheckBox _checkBoxExportWorkspaceConnections;
		private ToolStripEx toolStrip1;
		private System.Windows.Forms.RadioButton _radioButtonExportReferencedDescriptors;
		private System.Windows.Forms.RadioButton _radioButtonExportAllDescriptors;
		private System.Windows.Forms.ErrorProvider _errorProvider;
		private System.Windows.Forms.Label _labelDirectory;
		private System.Windows.Forms.Label _labelFile;
		private System.Windows.Forms.RadioButton _radioButtonDirectory;
		private System.Windows.Forms.RadioButton _radioButtonSingleFile;
		private FileSystemPathControl _fileSystemPathControlDirectory;
		private DoubleBufferedDataGridView _dataGridView;
		private DataGridViewFindToolStrip _dataGridViewFindToolStrip;
		private System.Windows.Forms.GroupBox _groupBoxDescriptors;
		private System.Windows.Forms.GroupBox _groupBoxCategories;
		private System.Windows.Forms.RadioButton _radioButtonExportReferencedCategories;
		private System.Windows.Forms.RadioButton _radioButtonExportAllCategories;
		private System.Windows.Forms.CheckBox _checkBoxExportNotes;
		private System.Windows.Forms.DataGridViewCheckBoxColumn _columnSelected;
		private System.Windows.Forms.DataGridViewImageColumn _columnImage;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnName;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnCategory;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnDescription;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnCreated;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnCreatedBy;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnLastChanged;
		private System.Windows.Forms.DataGridViewTextBoxColumn _columnLastChangedBy;
		private System.Windows.Forms.CheckBox _checkBoxExportConnectionFilePaths;
	}
}
